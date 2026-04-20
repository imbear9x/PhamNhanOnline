using GameServer.DTO;
using GameServer.Exceptions;
using GameServer.Runtime;
using GameServer.World;
using GameShared.Messages;

namespace GameServer.Services;

public sealed class EquipmentActionService
{
    private readonly GameDb _db;
    private readonly EquipmentService _equipmentService;
    private readonly SkillService _skillService;
    private readonly CharacterFinalStatService _characterFinalStatService;
    private readonly ItemService _itemService;

    public EquipmentActionService(
        GameDb db,
        EquipmentService equipmentService,
        SkillService skillService,
        CharacterFinalStatService characterFinalStatService,
        ItemService itemService)
    {
        _db = db;
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

        return await ExecuteEquipInternalAsync(player, playerItemId, slotIndex, cancellationToken);
    }

    public async Task<EquipmentActionExecutionResult> EquipFirstAvailableAsync(
        PlayerSession player,
        long playerItemId,
        CancellationToken cancellationToken = default)
    {
        await using var tx = await _db.BeginTransactionAsync(cancellationToken);

        var slotIndex = await _equipmentService.GetFirstAvailableSlotIndexAsync(
            player.CharacterData.CharacterId,
            cancellationToken);
        if (!slotIndex.HasValue)
            throw new GameException(MessageCode.EquipmentSlotInvalid);

        var result = await ExecuteEquipInternalAsync(player, playerItemId, slotIndex.Value, cancellationToken, ownsTransaction: true);
        await tx.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<EquipmentActionExecutionResult> UnequipAsync(
        PlayerSession player,
        int slotIndex,
        CancellationToken cancellationToken = default)
    {
        if (slotIndex <= 0)
            throw new GameException(MessageCode.EquipmentSlotInvalid);

        await using var tx = await _db.BeginTransactionAsync(cancellationToken);

        var changed = await _equipmentService.UnequipItemAsync(
            player.CharacterData.CharacterId,
            slotIndex,
            cancellationToken);
        if (!changed)
            throw new GameException(MessageCode.EquipmentSlotEmpty);

        var skillSync = await _skillService.SyncEquipmentGrantedSkillsAsync(player.CharacterData.CharacterId, cancellationToken);
        var runtimeSnapshot = await _characterFinalStatService.ApplyAuthoritativeFinalStatsAsync(player, cancellationToken);
        var items = await _itemService.GetInventoryAsync(player.CharacterData.CharacterId, cancellationToken);

        await tx.CommitAsync(cancellationToken);

        return new EquipmentActionExecutionResult(
            items,
            runtimeSnapshot,
            skillSync.Changed ? skillSync.Snapshot : null);
    }

    private async Task<EquipmentActionExecutionResult> ExecuteEquipInternalAsync(
        PlayerSession player,
        long playerItemId,
        int slotIndex,
        CancellationToken cancellationToken,
        bool ownsTransaction = false)
    {
        if (!ownsTransaction)
        {
            await using var tx = await _db.BeginTransactionAsync(cancellationToken);
            await _equipmentService.EquipItemAsync(
                player.CharacterData.CharacterId,
                playerItemId,
                slotIndex,
                cancellationToken);

            var skillSync = await _skillService.SyncEquipmentGrantedSkillsAsync(player.CharacterData.CharacterId, cancellationToken);
            var runtimeSnapshot = await _characterFinalStatService.ApplyAuthoritativeFinalStatsAsync(player, cancellationToken);
            var items = await _itemService.GetInventoryAsync(player.CharacterData.CharacterId, cancellationToken);

            await tx.CommitAsync(cancellationToken);

            return new EquipmentActionExecutionResult(
                items,
                runtimeSnapshot,
                skillSync.Changed ? skillSync.Snapshot : null);
        }

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
