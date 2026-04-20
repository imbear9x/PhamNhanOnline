using GameServer.DTO;
using GameServer.Entities;
using GameServer.Exceptions;
using GameServer.Network;
using GameServer.Network.Interface;
using GameServer.Randomness;
using GameServer.Repositories;
using GameServer.Runtime;
using GameServer.World;
using GameShared.Logging;
using GameShared.Messages;
using GameShared.Models;
using GameShared.Packets;
using Microsoft.Extensions.DependencyInjection;

namespace GameServer.Services;

public sealed class AlchemyPracticeService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PracticeService _practiceService;
    private readonly WorldManager _worldManager;
    private readonly INetworkSender _network;

    public AlchemyPracticeService(
        IServiceScopeFactory scopeFactory,
        PracticeService practiceService,
        WorldManager worldManager,
        INetworkSender network)
    {
        _scopeFactory = scopeFactory;
        _practiceService = practiceService;
        _worldManager = worldManager;
        _network = network;
    }

    public async Task<AlchemyPracticeStatusModel?> GetStatusAsync(
        Guid playerId,
        CancellationToken cancellationToken = default)
    {
        await EnsureDueSessionCompletedAsync(playerId, cancellationToken);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var pillRecipeService = scope.ServiceProvider.GetRequiredService<PillRecipeService>();
        var modelBuilder = scope.ServiceProvider.GetRequiredService<AlchemyModelBuilder>();
        var latestSession = await _practiceService.GetLatestSessionByTypeAsync(playerId, PracticeType.Alchemy, cancellationToken);
        if (latestSession is null)
            return null;
        if (latestSession.PracticeState == (int)PracticeSessionState.Completed ||
            latestSession.PracticeState == (int)PracticeSessionState.Cancelled)
        {
            return null;
        }

        PillRecipeDetailModel? recipe = null;
        try
        {
            var detail = await pillRecipeService.GetRecipeDetailAsync(playerId, latestSession.DefinitionId, cancellationToken);
            recipe = modelBuilder.BuildRecipeDetailModel(detail.Definition, detail.Progress);
        }
        catch (GameException)
        {
        }

        return modelBuilder.BuildAlchemyPracticeStatusModel(latestSession, recipe);
    }

    public async Task EnsureDueSessionsCompletedAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<PlayerPracticeSessionRepository>();
        var activeSessions = await repository.ListActiveByTypeAsync(PracticeType.Alchemy, cancellationToken);
        foreach (var session in activeSessions)
            await CompleteSessionIfDueAsync(session.Id, cancellationToken);
    }

    public async Task EnsureDueSessionCompletedAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        var latest = await _practiceService.GetLatestSessionByTypeAsync(playerId, PracticeType.Alchemy, cancellationToken);
        if (latest is null || latest.PracticeState != (int)PracticeSessionState.Active)
            return;

        await CompleteSessionIfDueAsync(latest.Id, cancellationToken);
    }

    public async Task PushPendingResultAsync(ConnectionSession session, CancellationToken cancellationToken = default)
    {
        if (session.Player is null)
            return;

        await EnsureDueSessionCompletedAsync(session.Player.CharacterData.CharacterId, cancellationToken);
        var latest = await _practiceService.GetLatestSessionByTypeAsync(session.Player.CharacterData.CharacterId, PracticeType.Alchemy, cancellationToken);
        if (latest is null || latest.PracticeState != (int)PracticeSessionState.ResultPendingAcknowledgement)
            return;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var modelBuilder = scope.ServiceProvider.GetRequiredService<AlchemyModelBuilder>();
        var result = modelBuilder.BuildPracticeCompletionResultModel(latest);
        if (!result.HasValue)
            return;

        _network.Send(session.ConnectionId, new PracticeCompletedPacket
        {
            Session = _practiceService.BuildSessionModel(latest, DateTime.UtcNow),
            Result = result.Value
        });
    }

    private async Task CompleteSessionIfDueAsync(long practiceSessionId, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GameDb>();
        var repository = scope.ServiceProvider.GetRequiredService<PlayerPracticeSessionRepository>();
        var session = await repository.GetByIdAsync(practiceSessionId, cancellationToken);
        if (session is null || session.PracticeType != (int)PracticeType.Alchemy || session.PracticeState != (int)PracticeSessionState.Active)
            return;

        var utcNow = DateTime.UtcNow;
        if (_practiceService.CalculateRemainingDurationSeconds(session, utcNow) > 0L)
            return;

        var requestPayload = _practiceService.DeserializeRequestPayload(session);
        if (requestPayload is null)
            return;

        var pillRecipeService = scope.ServiceProvider.GetRequiredService<PillRecipeService>();
        var alchemyService = scope.ServiceProvider.GetRequiredService<AlchemyService>();
        var itemService = scope.ServiceProvider.GetRequiredService<ItemService>();
        var playerRecipes = scope.ServiceProvider.GetRequiredService<PlayerPillRecipeRepository>();
        var notificationRepository = scope.ServiceProvider.GetRequiredService<PlayerNotificationRepository>();
        var notificationBuilder = scope.ServiceProvider.GetRequiredService<PlayerNotificationModelBuilder>();
        var random = scope.ServiceProvider.GetRequiredService<IGameRandomService>();
        var modelBuilder = scope.ServiceProvider.GetRequiredService<AlchemyModelBuilder>();

        try
        {
            var detail = await pillRecipeService.GetRecipeDetailAsync(session.PlayerId, session.DefinitionId, cancellationToken);
            var successRates = await alchemyService.BuildSuccessRollRatesAsync(
                session.PlayerId,
                session.DefinitionId,
                requestPayload.RequestedCraftCount,
                requestPayload.SelectedOptionalInputs,
                cancellationToken);
            var successCount = 0;
            for (var index = 0; index < successRates.Count; index++)
            {
                if (random.CheckChance(ToPartsPerMillion(successRates[index])).Success)
                    successCount++;
            }

            var requestedCraftCount = Math.Max(1, requestPayload.RequestedCraftCount);
            var failedCount = Math.Max(0, requestedCraftCount - successCount);
            var success = successCount > 0;

            await using var tx = await db.BeginTransactionAsync(cancellationToken);
            var rewards = new List<PracticeRewardEntry>();
            if (successCount > 0)
            {
                await itemService.AddItemAsync(
                    session.PlayerId,
                    detail.Definition.ResultPillItemTemplateId,
                    successCount,
                    false,
                    null,
                    cancellationToken);

                var learned = await playerRecipes.GetByPlayerAndRecipeAsync(session.PlayerId, session.DefinitionId, cancellationToken);
                if (learned is not null)
                {
                    learned.TotalCraftCount += successCount;
                    learned.CurrentSuccessRateBonus = alchemyService.ResolveMasteryBonusForCurrentProgress(
                        detail.Definition,
                        learned.TotalCraftCount,
                        learned.CurrentSuccessRateBonus);
                    learned.UpdatedAt = utcNow;
                    await playerRecipes.UpdateAsync(learned, cancellationToken);
                }

                rewards.Add(new PracticeRewardEntry(detail.Definition.ResultPillItemTemplateId, successCount));
            }

            var completionPayload = new PracticeCompletionPayload(
                success,
                requestedCraftCount,
                successCount,
                failedCount,
                success ? "Luyen che thanh cong" : "Luyen che that bai",
                successCount > 0
                    ? $"Nhan duoc {successCount} {detail.Definition.Name}."
                    : $"Khong nhan duoc {detail.Definition.Name}.",
                detail.Definition.ResultPillItemTemplateId,
                rewards);

            session.AccumulatedActiveSeconds = Math.Max(session.TotalDurationSeconds, _practiceService.CalculateAccumulatedActiveSeconds(session, utcNow));
            session.PracticeState = (int)PracticeSessionState.Completed;
            session.LastResumedAtUtc = null;
            session.PausedAtUtc = null;
            session.CompletedAtUtc = utcNow;
            session.ResultAcknowledgedAtUtc = utcNow;
            session.UpdatedAtUtc = utcNow;
            session.ResultPayloadJson = _practiceService.SerializePayload(completionPayload);
            await repository.UpdateAsync(session, cancellationToken);

            var notification = new PlayerNotificationEntity
            {
                PlayerId = session.PlayerId,
                NotificationType = (int)PlayerNotificationType.PracticeResult,
                SourceType = (int)PlayerNotificationSourceType.PracticeSession,
                SourceId = session.Id,
                Title = completionPayload.Title,
                Message = completionPayload.Message,
                DisplayItemTemplateId = completionPayload.DisplayItemTemplateId,
                PayloadJson = _practiceService.SerializePayload(completionPayload),
                CreatedAtUtc = utcNow
            };
            notification.Id = await notificationRepository.CreateAsync(notification, cancellationToken);
            await tx.CommitAsync(cancellationToken);

            if (_worldManager.TryGetPlayerByCharacterId(session.PlayerId, out var player))
            {
                try
                {
                    _practiceService.SyncOnlinePlayerState(player, null);

                    var completionPacket = new PracticeCompletedPacket
                    {
                        Session = _practiceService.BuildSessionModel(session, utcNow),
                        Result = completionPayload is not null ? modelBuilder.BuildPracticeCompletionResultModel(session) : null
                    };
                    _network.Send(player.ConnectionId, completionPacket);

                    var inventoryItems = await itemService.GetInventoryAsync(session.PlayerId, cancellationToken);
                    var inventoryPacket = new GetInventoryResultPacket
                    {
                        Success = true,
                        Code = MessageCode.None,
                        Items = inventoryItems.Select(static item => item.ToModel()).ToList()
                    };
                    _network.Send(player.ConnectionId, inventoryPacket);

                    var notificationModel = notificationBuilder.Build(notification);
                    var notificationPacket = new PlayerNotificationReceivedPacket
                    {
                        Notification = notificationModel
                    };
                    _network.Send(player.ConnectionId, notificationPacket);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Failed to push alchemy completion packets for session {session.Id}.");
                }
            }
        }
        catch (GameException ex)
        {
            Logger.Error($"Alchemy practice completion aborted with game exception code={ex.Code}.");
        }
    }

    private static int ToPartsPerMillion(double rawRate)
    {
        var normalized = rawRate <= 0d
            ? 0d
            : (rawRate > 1d ? rawRate / 100d : rawRate);
        return (int)Math.Round(Math.Clamp(normalized, 0d, 1d) * 1_000_000d, MidpointRounding.AwayFromZero);
    }
}

public readonly record struct AlchemyPracticeStartResult(
    bool Success,
    MessageCode Code,
    string? FailureReason,
    PracticeSessionModel? Session,
    IReadOnlyList<AlchemyConsumedItemModel> ConsumedItems,
    IReadOnlyList<InventoryItemModel> InventoryItems,
    PillRecipeDetailModel? Recipe)
{
    public static AlchemyPracticeStartResult Failed(MessageCode code, string? failureReason = null) =>
        new(false, code, failureReason, null, Array.Empty<AlchemyConsumedItemModel>(), Array.Empty<InventoryItemModel>(), null);

    public static AlchemyPracticeStartResult Succeeded(
        PracticeSessionModel session,
        IReadOnlyList<AlchemyConsumedItemModel> consumedItems,
        IReadOnlyList<InventoryItemModel> inventoryItems,
        PillRecipeDetailModel recipe) =>
        new(true, MessageCode.None, null, session, consumedItems, inventoryItems, recipe);
}
