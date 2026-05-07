using GameServer.DTO;
using GameServer.Entities;
using GameServer.Exceptions;
using GameServer.Network;
using GameServer.Repositories;
using GameServer.Runtime;
using GameServer.World;
using GameShared.Messages;
using GameShared.Models;

namespace GameServer.Services;

public sealed class AlchemyCraftActionService
{
    private readonly PracticeService _practiceService;
    private readonly PillRecipeService _pillRecipeService;
    private readonly AlchemyService _alchemyService;
    private readonly ItemService _itemService;
    private readonly PlayerItemRepository _playerItems;
    private readonly PlayerPracticeSessionRepository _practiceRepository;
    private readonly AlchemyModelBuilder _modelBuilder;
    private readonly PlayerInventoryTransactionService _inventoryTransactions;

    public AlchemyCraftActionService(
        PracticeService practiceService,
        PillRecipeService pillRecipeService,
        AlchemyService alchemyService,
        ItemService itemService,
        PlayerItemRepository playerItems,
        PlayerPracticeSessionRepository practiceRepository,
        AlchemyModelBuilder modelBuilder,
        PlayerInventoryTransactionService inventoryTransactions)
    {
        _practiceService = practiceService;
        _pillRecipeService = pillRecipeService;
        _alchemyService = alchemyService;
        _itemService = itemService;
        _playerItems = playerItems;
        _practiceRepository = practiceRepository;
        _modelBuilder = modelBuilder;
        _inventoryTransactions = inventoryTransactions;
    }

    public async Task<AlchemyPracticeStartResult> StartCraftAsync(
        ConnectionSession session,
        int recipeId,
        int requestedCraftCount,
        IReadOnlyCollection<long>? selectedPlayerItemIds,
        IReadOnlyCollection<AlchemyOptionalInputSelectionModel>? selectedOptionalInputs,
        CancellationToken cancellationToken = default)
    {
        if (session.Player is null)
            return AlchemyPracticeStartResult.Failed(MessageCode.CharacterMustEnterWorld);

        var player = session.Player;
        var runtimeSnapshot = player.RuntimeState.CaptureSnapshot();
        if (!_practiceService.TryValidatePrivateHome(player, out var failureCode))
            return AlchemyPracticeStartResult.Failed(failureCode);
        if (runtimeSnapshot.CurrentState.CurrentState == CharacterRuntimeStateCodes.Cultivating ||
            _practiceService.IsPracticing(player))
        {
            return AlchemyPracticeStartResult.Failed(MessageCode.PracticeAlreadyActive);
        }

        if (player.IsStunned(DateTime.UtcNow))
            return AlchemyPracticeStartResult.Failed(MessageCode.CharacterCannotActWhileStunned);
        if (runtimeSnapshot.CurrentState.CurrentState == CharacterRuntimeStateCodes.Casting || player.IsCastingSkill)
            return AlchemyPracticeStartResult.Failed(MessageCode.CharacterCannotActWhileCasting);

        var blocking = await _practiceService.GetBlockingSessionAsync(player.CharacterData.CharacterId, cancellationToken);
        if (blocking is not null)
            return AlchemyPracticeStartResult.Failed(MessageCode.PracticeAlreadyActive);

        try
        {
            var startedEntity = default(PlayerPracticeSessionEntity);
            var result = await _inventoryTransactions.ExecuteAsync(
                player.CharacterData.CharacterId,
                ct => StartCraftCoreAsync(
                    player,
                    recipeId,
                    requestedCraftCount,
                    selectedPlayerItemIds,
                    selectedOptionalInputs,
                    entity => startedEntity = entity,
                    ct),
                cancellationToken);

            if (result.Success && startedEntity is not null)
                _practiceService.SyncOnlinePlayerState(player, startedEntity);

            return result;
        }
        catch (GameException ex)
        {
            return AlchemyPracticeStartResult.Failed(ex.Code);
        }
    }

    private async Task<AlchemyPracticeStartResult> StartCraftCoreAsync(
        PlayerSession player,
        int recipeId,
        int requestedCraftCount,
        IReadOnlyCollection<long>? selectedPlayerItemIds,
        IReadOnlyCollection<AlchemyOptionalInputSelectionModel>? selectedOptionalInputs,
        Action<PlayerPracticeSessionEntity> markStarted,
        CancellationToken cancellationToken)
    {
        var playerId = player.CharacterData.CharacterId;
        var blocking = await _practiceRepository.GetBlockingSessionAsync(playerId, cancellationToken);
        if (blocking is not null)
            return AlchemyPracticeStartResult.Failed(MessageCode.PracticeAlreadyActive);

        var detail = await _pillRecipeService.GetRecipeDetailAsync(playerId, recipeId, cancellationToken);
        var validation = await _alchemyService.ValidateCraftPillAsync(
            playerId,
            recipeId,
            requestedCraftCount,
            selectedPlayerItemIds,
            selectedOptionalInputs,
            cancellationToken);
        if (!validation.Success)
        {
            return AlchemyPracticeStartResult.Failed(
                MessageCode.AlchemyInputInvalid,
                validation.FailureReason);
        }

        var inventoryBefore = await _itemService.GetInventoryAsync(playerId, cancellationToken);
        var inventoryBeforeByPlayerItemId = inventoryBefore.ToDictionary(static item => item.PlayerItemId);
        var utcNow = DateTime.UtcNow;
        var requestPayload = new PracticeSessionPayload(
            recipeId,
            Math.Max(1, validation.RequestedCraftCount),
            validation.AppliedOptionalInputs
                .Select(selection => new PracticeOptionalInputEntry(selection.Input.Id, Math.Max(0, selection.AppliedCount)))
                .OrderBy(static entry => entry.InputId)
                .ToArray(),
            validation.SuccessRateSegments
                .Select(segment => new PracticeRateSegmentEntry(segment.SuccessRate, Math.Max(0, segment.Count)))
                .ToArray(),
            BuildConsumedEntries(validation, inventoryBeforeByPlayerItemId));

        await ConsumeValidatedInputsAsync(
            playerId,
            validation,
            cancellationToken);

        var entity = new PlayerPracticeSessionEntity
        {
            PlayerId = playerId,
            PracticeType = (int)PracticeType.Alchemy,
            PracticeState = (int)PracticeSessionState.Active,
            DefinitionId = recipeId,
            CurrentMapId = player.MapId,
            Title = detail.Definition.Name,
            TotalDurationSeconds = Math.Max(1L, detail.Definition.CraftDurationSeconds) * Math.Max(1, validation.RequestedCraftCount),
            AccumulatedActiveSeconds = 0L,
            CancelLockedProgress = 1d,
            RequestPayloadJson = _practiceService.SerializePayload(requestPayload),
            ResultPayloadJson = null,
            StartedAtUtc = utcNow,
            LastResumedAtUtc = utcNow,
            PausedAtUtc = null,
            CompletedAtUtc = null,
            ResultAcknowledgedAtUtc = null,
            UpdatedAtUtc = utcNow,
            CreatedAtUtc = utcNow
        };
        entity.Id = await _practiceRepository.CreateAsync(entity, cancellationToken);
        markStarted(entity);

        var inventoryAfter = await _itemService.GetInventoryAsync(playerId, cancellationToken);
        return AlchemyPracticeStartResult.Succeeded(
            _practiceService.BuildSessionModel(entity, DateTime.UtcNow),
            _modelBuilder.BuildConsumedItems(validation, inventoryBeforeByPlayerItemId),
            inventoryAfter.Select(static item => item.ToModel()).ToArray(),
            _modelBuilder.BuildRecipeDetailModel(detail.Definition, detail.Progress));
    }

    private async Task ConsumeValidatedInputsAsync(
        Guid playerId,
        AlchemyValidationResult validation,
        CancellationToken cancellationToken)
    {
        foreach (var playerItemId in validation.ConsumedPlayerItemIds)
            await _itemService.RemovePlayerItemAsync(playerId, playerItemId, cancellationToken);

        foreach (var stackReduction in validation.ConsumedStackQuantities)
        {
            var playerItem = await _playerItems.GetByIdAsync(stackReduction.Key, cancellationToken)
                             ?? throw new InvalidOperationException($"Player item {stackReduction.Key} was not found during alchemy practice start.");
            if (playerItem.PlayerId != playerId)
                throw new InvalidOperationException($"Player item {stackReduction.Key} does not belong to player {playerId}.");

            playerItem.Quantity -= stackReduction.Value;
            playerItem.UpdatedAt = DateTime.UtcNow;
            if (playerItem.Quantity <= 0)
            {
                await _itemService.RemovePlayerItemAsync(playerId, playerItem.Id, cancellationToken);
            }
            else
            {
                await _playerItems.UpdateAsync(playerItem, cancellationToken);
            }
        }
    }

    private static PracticeConsumedEntry[] BuildConsumedEntries(
        AlchemyValidationResult validation,
        IReadOnlyDictionary<long, InventoryItemView> inventoryByPlayerItemId)
    {
        var entries = new List<PracticeConsumedEntry>();
        foreach (var playerItemId in validation.ConsumedPlayerItemIds)
        {
            if (!inventoryByPlayerItemId.TryGetValue(playerItemId, out var item))
                continue;

            entries.Add(new PracticeConsumedEntry(
                item.PlayerItemId,
                item.Definition.Id,
                1,
                item.IsBound,
                item.ExpireAt.HasValue
                    ? new DateTimeOffset(item.ExpireAt.Value).ToUnixTimeMilliseconds()
                    : null));
        }

        foreach (var pair in validation.ConsumedStackQuantities.OrderBy(static x => x.Key))
        {
            if (!inventoryByPlayerItemId.TryGetValue(pair.Key, out var item))
                continue;

            entries.Add(new PracticeConsumedEntry(
                item.PlayerItemId,
                item.Definition.Id,
                Math.Max(0, pair.Value),
                item.IsBound,
                item.ExpireAt.HasValue
                    ? new DateTimeOffset(item.ExpireAt.Value).ToUnixTimeMilliseconds()
                    : null));
        }

        return entries.ToArray();
    }
}
