using GameServer.Entities;
using GameServer.Exceptions;
using GameServer.Repositories;
using GameServer.Runtime;

namespace GameServer.Services;

public sealed class EquipmentService
{
    private readonly PlayerItemRepository _playerItems;
    private readonly PlayerEquipmentRepository _playerEquipments;
    private readonly ItemDefinitionCatalog _definitions;

    public EquipmentService(
        PlayerItemRepository playerItems,
        PlayerEquipmentRepository playerEquipments,
        ItemDefinitionCatalog definitions)
    {
        _playerItems = playerItems;
        _playerEquipments = playerEquipments;
        _definitions = definitions;
    }

    public async Task<EquipmentValidationResult> ValidateEquipAsync(
        Guid playerId,
        long playerItemId,
        EquipmentSlot requestedSlot,
        CancellationToken cancellationToken = default)
    {
        var playerItem = await _playerItems.GetByIdAsync(playerItemId, cancellationToken);
        if (playerItem is null || playerItem.PlayerId != playerId)
            return EquipmentValidationResult.Failed(GameShared.Messages.MessageCode.InventoryItemInvalid, "Item khong ton tai trong tui do cua player.");
        if (!_definitions.TryGetItem(playerItem.ItemTemplateId, out var itemDefinition) || itemDefinition.Equipment is null)
            return EquipmentValidationResult.Failed(GameShared.Messages.MessageCode.InventoryItemInvalid, "Item nay khong phai trang bi.");
        if (itemDefinition.Equipment.SlotType != requestedSlot)
            return EquipmentValidationResult.Failed(GameShared.Messages.MessageCode.EquipmentSlotMismatch, "Item khong thuoc slot dang yeu cau.");

        var inventory = await _playerItems.ListByPlayerIdAsync(playerId, cancellationToken);
        var equipmentRows = await _playerEquipments.ListByPlayerItemIdsAsync(inventory.Select(x => x.Id).ToArray(), cancellationToken);
        var equipmentByItemId = equipmentRows.ToDictionary(x => x.PlayerItemId);
        if (!equipmentByItemId.TryGetValue(playerItemId, out var equipmentEntity))
            return EquipmentValidationResult.Failed(GameShared.Messages.MessageCode.InventoryItemInvalid, "Trang bi chua duoc khoi tao instance equipment.");

        var occupied = equipmentRows.FirstOrDefault(x => x.PlayerItemId != playerItemId && x.EquippedSlot == (int)requestedSlot);

        return EquipmentValidationResult.Succeeded(playerItem, itemDefinition, equipmentEntity, occupied);
    }

    public async Task<EquippedItemView> EquipItemAsync(
        Guid playerId,
        long playerItemId,
        EquipmentSlot requestedSlot,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateEquipAsync(playerId, playerItemId, requestedSlot, cancellationToken);
        if (!validation.Success || validation.PlayerItem is null || validation.ItemDefinition?.Equipment is null || validation.PlayerEquipment is null)
            throw new GameException(validation.FailureCode ?? GameShared.Messages.MessageCode.UnknownError, validation.FailureReason ?? "Khong the trang bi item.");

        if (validation.OccupiedEquipment is not null)
        {
            validation.OccupiedEquipment.EquippedSlot = null;
            validation.OccupiedEquipment.UpdatedAt = DateTime.UtcNow;
            await _playerEquipments.UpdateAsync(validation.OccupiedEquipment, cancellationToken);
        }

        var equipmentEntity = validation.PlayerEquipment;
        equipmentEntity.EquippedSlot = (int)requestedSlot;
        equipmentEntity.UpdatedAt = DateTime.UtcNow;
        await _playerEquipments.UpdateAsync(equipmentEntity, cancellationToken);

        return new EquippedItemView(
            validation.PlayerItem.Id,
            validation.ItemDefinition,
            validation.ItemDefinition.Equipment,
            requestedSlot,
            equipmentEntity.EnhanceLevel,
            equipmentEntity.Durability);
    }

    public async Task<bool> UnequipItemAsync(
        Guid playerId,
        EquipmentSlot slot,
        CancellationToken cancellationToken = default)
    {
        var inventory = await _playerItems.ListByPlayerIdAsync(playerId, cancellationToken);
        var equipmentRows = await _playerEquipments.ListByPlayerItemIdsAsync(inventory.Select(x => x.Id).ToArray(), cancellationToken);
        var target = equipmentRows.FirstOrDefault(x => x.EquippedSlot == (int)slot);
        if (target is null)
            return false;

        target.EquippedSlot = null;
        target.UpdatedAt = DateTime.UtcNow;
        await _playerEquipments.UpdateAsync(target, cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<EquippedItemView>> GetEquippedItemsAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        var inventory = await _playerItems.ListByPlayerIdAsync(playerId, cancellationToken);
        var itemsById = inventory.ToDictionary(x => x.Id);
        var equipmentRows = await _playerEquipments.ListByPlayerItemIdsAsync(itemsById.Keys.ToArray(), cancellationToken);

        return equipmentRows
            .Where(x => x.EquippedSlot.HasValue && itemsById.ContainsKey(x.PlayerItemId))
            .Select(x =>
            {
                var item = itemsById[x.PlayerItemId];
                if (!_definitions.TryGetItem(item.ItemTemplateId, out var definition) || definition.Equipment is null)
                    throw new InvalidOperationException($"Player item {item.Id} duoc mark equip nhung khong co equipment definition.");

                return new EquippedItemView(
                    item.Id,
                    definition,
                    definition.Equipment,
                    (EquipmentSlot)x.EquippedSlot!.Value,
                    x.EnhanceLevel,
                    x.Durability);
            })
            .OrderBy(x => x.EquippedSlot)
            .ToArray();
    }
}

public sealed record EquipmentValidationResult(
    bool Success,
    GameShared.Messages.MessageCode? FailureCode,
    string? FailureReason,
    PlayerItemEntity? PlayerItem,
    ItemDefinition? ItemDefinition,
    PlayerEquipmentEntity? PlayerEquipment,
    PlayerEquipmentEntity? OccupiedEquipment)
{
    public static EquipmentValidationResult Failed(GameShared.Messages.MessageCode code, string reason) =>
        new(false, code, reason, null, null, null, null);

    public static EquipmentValidationResult Succeeded(
        PlayerItemEntity playerItem,
        ItemDefinition itemDefinition,
        PlayerEquipmentEntity playerEquipment,
        PlayerEquipmentEntity? occupiedEquipment) =>
        new(true, null, null, playerItem, itemDefinition, playerEquipment, occupiedEquipment);
}
