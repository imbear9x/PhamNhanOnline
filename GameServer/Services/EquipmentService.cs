using GameServer.Entities;
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
        CancellationToken cancellationToken = default)
    {
        var playerItem = await _playerItems.GetByIdAsync(playerItemId, cancellationToken);
        if (playerItem is null || playerItem.PlayerId != playerId)
            return EquipmentValidationResult.Failed("Item khong ton tai trong tui do cua player.");
        if (!_definitions.TryGetItem(playerItem.ItemTemplateId, out var itemDefinition) || itemDefinition.Equipment is null)
            return EquipmentValidationResult.Failed("Item nay khong phai trang bi.");

        var inventory = await _playerItems.ListByPlayerIdAsync(playerId, cancellationToken);
        var equipmentRows = await _playerEquipments.ListByPlayerItemIdsAsync(inventory.Select(x => x.Id).ToArray(), cancellationToken);
        var equipmentByItemId = equipmentRows.ToDictionary(x => x.PlayerItemId);
        if (!equipmentByItemId.TryGetValue(playerItemId, out var equipmentEntity))
            return EquipmentValidationResult.Failed("Trang bi chua duoc khoi tao instance equipment.");

        var occupied = equipmentRows
            .Where(x => x.PlayerItemId != playerItemId && x.EquippedSlot == (int)itemDefinition.Equipment.SlotType)
            .Join(inventory, equipment => equipment.PlayerItemId, item => item.Id, (equipment, item) => item)
            .Any(item => item.PlayerId == playerId);

        if (occupied)
            return EquipmentValidationResult.Failed($"Slot {itemDefinition.Equipment.SlotType} dang co trang bi khac.");

        return EquipmentValidationResult.Succeeded(playerItem, itemDefinition, equipmentEntity);
    }

    public async Task<EquippedItemView> EquipItemAsync(
        Guid playerId,
        long playerItemId,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateEquipAsync(playerId, playerItemId, cancellationToken);
        if (!validation.Success || validation.PlayerItem is null || validation.ItemDefinition?.Equipment is null || validation.PlayerEquipment is null)
            throw new InvalidOperationException(validation.FailureReason ?? "Khong the trang bi item.");

        var equipmentEntity = validation.PlayerEquipment;
        equipmentEntity.EquippedSlot = (int)validation.ItemDefinition.Equipment.SlotType;
        equipmentEntity.UpdatedAt = DateTime.UtcNow;
        await _playerEquipments.UpdateAsync(equipmentEntity, cancellationToken);

        return new EquippedItemView(
            validation.PlayerItem.Id,
            validation.ItemDefinition,
            validation.ItemDefinition.Equipment,
            validation.ItemDefinition.Equipment.SlotType,
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
    string? FailureReason,
    PlayerItemEntity? PlayerItem,
    ItemDefinition? ItemDefinition,
    PlayerEquipmentEntity? PlayerEquipment)
{
    public static EquipmentValidationResult Failed(string reason) => new(false, reason, null, null, null);

    public static EquipmentValidationResult Succeeded(
        PlayerItemEntity playerItem,
        ItemDefinition itemDefinition,
        PlayerEquipmentEntity playerEquipment) =>
        new(true, null, playerItem, itemDefinition, playerEquipment);
}
