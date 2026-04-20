using GameServer.DTO;
using GameServer.Entities;
using GameServer.Exceptions;
using GameServer.Network;
using GameServer.Repositories;
using GameServer.Runtime;
using GameShared.Messages;
using GameShared.Models;

namespace GameServer.Services;

public sealed class AlchemyCraftActionService
{
    private readonly GameDb _db;
    private readonly PracticeService _practiceService;
    private readonly PillRecipeService _pillRecipeService;
    private readonly AlchemyService _alchemyService;
    private readonly ItemService _itemService;
    private readonly PlayerItemRepository _playerItems;
    private readonly PlayerPracticeSessionRepository _practiceRepository;
    private readonly AlchemyModelBuilder _modelBuilder;

    public AlchemyCraftActionService(
        GameDb db,
        PracticeService practiceService,
        PillRecipeService pillRecipeService,
        AlchemyService alchemyService,
        ItemService itemService,
        PlayerItemRepository playerItems,
        PlayerPracticeSessionRepository practiceRepository,
        AlchemyModelBuilder modelBuilder)
    {
        _db = db;
        _practiceService = practiceService;
        _pillRecipeService = pillRecipeService;
        _alchemyService = alchemyService;
        _itemService = itemService;
        _playerItems = playerItems;
        _practiceRepository = practiceRepository;
        _modelBuilder = modelBuilder;
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
            var detail = await _pillRecipeService.GetRecipeDetailAsync(player.CharacterData.CharacterId, recipeId, cancellationToken);
            var validation = await _alchemyService.ValidateCraftPillAsync(
                player.CharacterData.CharacterId,
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

            var inventoryBefore = await _itemService.GetInventoryAsync(player.CharacterData.CharacterId, cancellationToken);
            var inventoryBeforeByPlayerItemId = inventoryBefore.ToDictionary(static item => item.PlayerItemId);
            var utcNow = DateTime.UtcNow;
            var requestPayload = new PracticeSessionPayload(
                recipeId,
                Math.Max(1, validation.RequestedCraftCount),
                validation.AppliedOptionalInputs
                    .Select(selection => new PracticeOptionalInputEntry(selection.Input.Id, Math.Max(0, selection.AppliedCount)))
                    .OrderBy(static entry => entry.InputId)
                    .ToArray(),
                BuildConsumedEntries(validation, inventoryBeforeByPlayerItemId));

            await using var tx = await _db.BeginTransactionAsync(cancellationToken);
            await ConsumeValidatedInputsAsync(
                player.CharacterData.CharacterId,
                validation,
                cancellationToken);

            var entity = new PlayerPracticeSessionEntity
            {
                PlayerId = player.CharacterData.CharacterId,
                PracticeType = (int)PracticeType.Alchemy,
                PracticeState = (int)PracticeSessionState.Active,
                DefinitionId = recipeId,
                CurrentMapId = player.MapId,
                Title = detail.Definition.Name,
                TotalDurationSeconds = Math.Max(1L, detail.Definition.CraftDurationSeconds) * Math.Max(1, validation.RequestedCraftCount),
                AccumulatedActiveSeconds = 0L,
                CancelLockedProgress = 0.8d,
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
            await tx.CommitAsync(cancellationToken);

            _practiceService.SyncOnlinePlayerState(player, entity);

            var inventoryAfter = await _itemService.GetInventoryAsync(player.CharacterData.CharacterId, cancellationToken);
            return AlchemyPracticeStartResult.Succeeded(
                _practiceService.BuildSessionModel(entity, DateTime.UtcNow),
                _modelBuilder.BuildConsumedItems(validation, inventoryBeforeByPlayerItemId),
                inventoryAfter.Select(static item => item.ToModel()).ToArray(),
                _modelBuilder.BuildRecipeDetailModel(detail.Definition, detail.Progress));
        }
        catch (GameException ex)
        {
            return AlchemyPracticeStartResult.Failed(ex.Code);
        }
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

            entries.Add(new PracticeConsumedEntry(item.PlayerItemId, item.Definition.Id, 1));
        }

        foreach (var pair in validation.ConsumedStackQuantities.OrderBy(static x => x.Key))
        {
            if (!inventoryByPlayerItemId.TryGetValue(pair.Key, out var item))
                continue;

            entries.Add(new PracticeConsumedEntry(item.PlayerItemId, item.Definition.Id, Math.Max(0, pair.Value)));
        }

        return entries.ToArray();
    }
}
