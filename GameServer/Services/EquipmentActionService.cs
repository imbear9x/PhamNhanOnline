using GameServer.DTO;
using GameServer.Exceptions;
using GameServer.Runtime;
using GameServer.World;
using GameShared.Messages;

namespace GameServer.Services;

public sealed class EquipmentActionService
{
    private readonly PlayerInventoryTransactionService _inventoryTransactions;
    private readonly EquipmentService _equipmentService;
    private readonly SkillService _skillService;
    private readonly CharacterFinalStatService _characterFinalStatService;
    private readonly ItemService _itemService;

    public EquipmentActionService(
        PlayerInventoryTransactionService inventoryTransactions,
        EquipmentService equipmentService,
        SkillService skillService,
        CharacterFinalStatService characterFinalStatService,
        ItemService itemService)
    {
        _inventoryTransactions = inventoryTransactions;
        _equipmentService = equipmentService;
        _skillService = skillService;
        _characterFinalStatService = characterFinalStatService;
        _itemService = itemService;
    }

    public async Task<EquipmentActionExecutionResult> EquipAsync(
        PlayerSession player,
        long playerItemId,
        int slotIndex,
        CancellationToken cancellationToken = default)
    {
        if (slotIndex <= 0)
            throw new GameException(MessageCode.EquipmentSlotInvalid);

        return await _inventoryTransactions.ExecuteAsync(
            player.CharacterData.CharacterId,
            ct => ExecuteEquipInternalAsync(player, playerItemId, slotIndex, ct),
            cancellationToken);
    }

    public async Task<EquipmentActionExecutionResult> EquipFirstAvailableAsync(
        PlayerSession player,
        long playerItemId,
        CancellationToken cancellationToken = default)
    {
        return await _inventoryTransactions.ExecuteAsync(
            player.CharacterData.CharacterId,
            async ct =>
            {
                var slotIndex = await _equipmentService.GetFirstAvailableSlotIndexAsync(
                    player.CharacterData.CharacterId,
                    ct);
                if (!slotIndex.HasValue)
                    throw new GameException(MessageCode.EquipmentSlotInvalid);

                return await ExecuteEquipInternalAsync(player, playerItemId, slotIndex.Value, ct);
            },
            cancellationToken);
    }

    public async Task<EquipmentActionExecutionResult> UnequipAsync(
        PlayerSession player,
        int slotIndex,
        CancellationToken cancellationToken = default)
    {
        if (slotIndex <= 0)
            throw new GameException(MessageCode.EquipmentSlotInvalid);

        return await _inventoryTransactions.ExecuteAsync(
            player.CharacterData.CharacterId,
            async ct =>
            {
                var changed = await _equipmentService.UnequipItemAsync(
                    player.CharacterData.CharacterId,
                    slotIndex,
                    ct);
                if (!changed)
                    throw new GameException(MessageCode.EquipmentSlotEmpty);

                var skillSync = await _skillService.SyncEquipmentGrantedSkillsAsync(player.CharacterData.CharacterId, ct);
                var runtimeSnapshot = await _characterFinalStatService.ApplyAuthoritativeFinalStatsAsync(player, ct);
                var items = await _itemService.GetInventoryAsync(player.CharacterData.CharacterId, ct);

                return new EquipmentActionExecutionResult(
                    items,
                    runtimeSnapshot,
                    skillSync.Changed ? skillSync.Snapshot : null);
            },
            cancellationToken);
    }

    private async Task<EquipmentActionExecutionResult> ExecuteEquipInternalAsync(
        PlayerSession player,
        long playerItemId,
        int slotIndex,
        CancellationToken cancellationToken)
    {
        await _equipmentService.EquipItemAsync(
            player.CharacterData.CharacterId,
            playerItemId,
            slotIndex,
            cancellationToken);

        var ownedSkills = await _skillService.SyncEquipmentGrantedSkillsAsync(player.CharacterData.CharacterId, cancellationToken);
        var finalSnapshot = await _characterFinalStatService.ApplyAuthoritativeFinalStatsAsync(player, cancellationToken);
        var inventoryItems = await _itemService.GetInventoryAsync(player.CharacterData.CharacterId, cancellationToken);

        return new EquipmentActionExecutionResult(
            inventoryItems,
            finalSnapshot,
            ownedSkills.Changed ? ownedSkills.Snapshot : null);
    }
}

public readonly record struct EquipmentActionExecutionResult(
    IReadOnlyList<InventoryItemView> Items,
    CharacterRuntimeSnapshot RuntimeSnapshot,
    OwnedSkillsSnapshotDto? ChangedSkillSnapshot);
