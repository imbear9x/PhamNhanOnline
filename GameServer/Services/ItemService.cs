using GameServer.Entities;
using GameServer.Repositories;
using GameServer.Runtime;

namespace GameServer.Services;

public sealed class ItemService
{
    private readonly ItemDefinitionCatalog _definitions;
    private readonly PlayerItemRepository _playerItems;
    private readonly PlayerEquipmentRepository _playerEquipments;
    private readonly PlayerEquipmentStatBonusRepository _playerEquipmentBonuses;
    private readonly PlayerSoilRepository _playerSoils;

    public ItemService(
        ItemDefinitionCatalog definitions,
        PlayerItemRepository playerItems,
        PlayerEquipmentRepository playerEquipments,
        PlayerEquipmentStatBonusRepository playerEquipmentBonuses,
        PlayerSoilRepository playerSoils)
    {
        _definitions = definitions;
        _playerItems = playerItems;
        _playerEquipments = playerEquipments;
        _playerEquipmentBonuses = playerEquipmentBonuses;
        _playerSoils = playerSoils;
    }

    public async Task<IReadOnlyList<PlayerItemEntity>> AddItemAsync(
        Guid playerId,
        int itemTemplateId,
        int quantity,
        bool isBound = false,
        DateTime? expireAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        var definition = GetDefinition(itemTemplateId);
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        var touched = new List<PlayerItemEntity>();
        if (!definition.IsStackable)
        {
            for (var i = 0; i < quantity; i++)
            {
                var entity = new PlayerItemEntity
                {
                    PlayerId = playerId,
                    ItemTemplateId = itemTemplateId,
                    Quantity = 1,
                    IsBound = isBound,
                    AcquiredAt = DateTime.UtcNow,
                    ExpireAt = expireAtUtc,
                    UpdatedAt = DateTime.UtcNow
                };
                entity.Id = await _playerItems.CreateAsync(entity, cancellationToken);
                await EnsureEquipmentRecordIfNeededAsync(definition, entity.Id, cancellationToken);
                await EnsureSoilRecordIfNeededAsync(definition, entity.Id, cancellationToken);
                touched.Add(entity);
            }

            return touched;
        }

        var remaining = quantity;
        var existingStacks = await _playerItems.ListByTemplateIdAsync(playerId, itemTemplateId, cancellationToken);
        foreach (var stack in existingStacks.Where(x => !IsExpired(x.ExpireAt) && x.IsBound == isBound && x.ExpireAt == expireAtUtc && x.Quantity < definition.MaxStack))
        {
            var availableCapacity = definition.MaxStack - stack.Quantity;
            if (availableCapacity <= 0)
                continue;

            var added = Math.Min(remaining, availableCapacity);
            stack.Quantity += added;
            stack.UpdatedAt = DateTime.UtcNow;
            await _playerItems.UpdateAsync(stack, cancellationToken);
            touched.Add(stack);
            remaining -= added;
            if (remaining <= 0)
                break;
        }

        while (remaining > 0)
        {
            var stackQuantity = Math.Min(remaining, definition.MaxStack);
            var entity = new PlayerItemEntity
            {
                PlayerId = playerId,
                ItemTemplateId = itemTemplateId,
                Quantity = stackQuantity,
                IsBound = isBound,
                AcquiredAt = DateTime.UtcNow,
                ExpireAt = expireAtUtc,
                UpdatedAt = DateTime.UtcNow
            };
            entity.Id = await _playerItems.CreateAsync(entity, cancellationToken);
            touched.Add(entity);
            remaining -= stackQuantity;
        }

        return touched;
    }

    public async Task RemoveItemAsync(
        Guid playerId,
        int itemTemplateId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        var definition = GetDefinition(itemTemplateId);
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        var playerItems = await _playerItems.ListByTemplateIdAsync(playerId, itemTemplateId, cancellationToken);
        playerItems = playerItems.Where(x => !IsExpired(x.ExpireAt)).ToList();

        if (!definition.IsStackable)
        {
            var equipmentRows = await _playerEquipments.ListByPlayerItemIdsAsync(playerItems.Select(x => x.Id).ToArray(), cancellationToken);
            var equipmentByItemId = equipmentRows.ToDictionary(x => x.PlayerItemId);
            var removable = playerItems
                .Where(item => !equipmentByItemId.TryGetValue(item.Id, out var equipment) || !equipment.EquippedSlot.HasValue)
                .Take(quantity)
                .ToArray();
            if (removable.Length < quantity)
                throw new InvalidOperationException($"Player does not have enough removable items for template {itemTemplateId}.");

            for (var i = 0; i < removable.Length; i++)
            {
                await RemovePlayerItemAsync(playerId, removable[i].Id, cancellationToken);
            }

            return;
        }

        var remaining = quantity;
        foreach (var item in playerItems.OrderBy(x => x.AcquiredAt).ThenBy(x => x.Id))
        {
            if (remaining <= 0)
                break;

            var removed = Math.Min(remaining, item.Quantity);
            item.Quantity -= removed;
            item.UpdatedAt = DateTime.UtcNow;
            remaining -= removed;
            if (item.Quantity <= 0)
            {
                await _playerItems.DeleteAsync(item.Id, cancellationToken);
            }
            else
            {
                await _playerItems.UpdateAsync(item, cancellationToken);
            }
        }

        if (remaining > 0)
            throw new InvalidOperationException($"Player does not have enough quantity for template {itemTemplateId}.");
    }

    public async Task RemovePlayerItemAsync(
        Guid playerId,
        long playerItemId,
        CancellationToken cancellationToken = default)
    {
        var playerItem = await _playerItems.GetByIdAsync(playerItemId, cancellationToken)
                         ?? throw new InvalidOperationException($"Player item {playerItemId} was not found.");
        if (playerItem.PlayerId != playerId)
            throw new InvalidOperationException($"Player item {playerItemId} does not belong to player {playerId}.");

        var equipment = await _playerEquipments.GetByPlayerItemIdAsync(playerItemId, cancellationToken);
        if (equipment?.EquippedSlot.HasValue == true)
            throw new InvalidOperationException($"Player item {playerItemId} is currently equipped and cannot be removed.");

        var soil = await _playerSoils.GetByPlayerItemIdAsync(playerItemId, cancellationToken);
        if (soil is not null && soil.State == (int)PlayerSoilState.Inserted)
            throw new InvalidOperationException($"Player item {playerItemId} is currently inserted into a garden plot and cannot be removed.");

        await _playerEquipmentBonuses.DeleteByPlayerItemIdAsync(playerItemId, cancellationToken);
        if (equipment is not null)
            await _playerEquipments.DeleteAsync(playerItemId, cancellationToken);
        if (soil is not null)
            await _playerSoils.DeleteAsync(playerItemId, cancellationToken);

        await _playerItems.DeleteAsync(playerItemId, cancellationToken);
    }

    public async Task ConsumePlayerItemAsync(
        Guid playerId,
        long playerItemId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        var playerItem = await _playerItems.GetByIdAsync(playerItemId, cancellationToken)
                         ?? throw new InvalidOperationException($"Player item {playerItemId} was not found.");
        if (playerItem.PlayerId != playerId)
            throw new InvalidOperationException($"Player item {playerItemId} does not belong to player {playerId}.");

        var definition = GetDefinition(playerItem.ItemTemplateId);
        if (IsExpired(playerItem.ExpireAt))
            throw new InvalidOperationException($"Player item {playerItemId} has expired.");

        if (!definition.IsStackable)
        {
            if (quantity != 1)
                throw new InvalidOperationException($"Non-stackable item {playerItemId} can only be consumed one at a time.");

            await RemovePlayerItemAsync(playerId, playerItemId, cancellationToken);
            return;
        }

        if (playerItem.Quantity < quantity)
            throw new InvalidOperationException($"Player item {playerItemId} does not have enough quantity.");

        playerItem.Quantity -= quantity;
        playerItem.UpdatedAt = DateTime.UtcNow;

        if (playerItem.Quantity <= 0)
        {
            await _playerItems.DeleteAsync(playerItemId, cancellationToken);
            return;
        }

        await _playerItems.UpdateAsync(playerItem, cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryItemView>> GetInventoryAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        var playerItems = await _playerItems.ListByPlayerIdAsync(playerId, cancellationToken);
        playerItems = playerItems.Where(x => !IsExpired(x.ExpireAt)).ToList();
        var equipmentRows = await _playerEquipments.ListByPlayerItemIdsAsync(playerItems.Select(x => x.Id).ToArray(), cancellationToken);
        var equipmentByItemId = equipmentRows.ToDictionary(x => x.PlayerItemId);

        return playerItems
            .Select(item =>
            {
                var definition = GetDefinition(item.ItemTemplateId);
                var equipment = equipmentByItemId.GetValueOrDefault(item.Id);
                return new InventoryItemView(
                    item.Id,
                    item.PlayerId,
                    definition,
                    item.Quantity,
                    item.IsBound,
                    item.AcquiredAt,
                    item.ExpireAt,
                    equipment?.EquippedSlot.HasValue == true,
                    equipment?.EquippedSlot.HasValue == true ? (EquipmentSlot?)equipment.EquippedSlot.Value : null,
                    equipment?.EnhanceLevel ?? 0,
                    equipment?.Durability);
            })
            .ToArray();
    }

    private ItemDefinition GetDefinition(int itemTemplateId)
    {
        if (!_definitions.TryGetItem(itemTemplateId, out var definition))
            throw new InvalidOperationException($"Item template {itemTemplateId} was not found.");

        return definition;
    }

    private async Task EnsureEquipmentRecordIfNeededAsync(ItemDefinition definition, long playerItemId, CancellationToken cancellationToken)
    {
        if (definition.Equipment is null)
            return;

        var existing = await _playerEquipments.GetByPlayerItemIdAsync(playerItemId, cancellationToken);
        if (existing is not null)
            return;

        await _playerEquipments.CreateAsync(new PlayerEquipmentEntity
        {
            PlayerItemId = playerItemId,
            EquippedSlot = null,
            EnhanceLevel = 0,
            Durability = null,
            UpdatedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    private async Task EnsureSoilRecordIfNeededAsync(ItemDefinition definition, long playerItemId, CancellationToken cancellationToken)
    {
        if (definition.ItemType != ItemType.Soil)
            return;

        var existing = await _playerSoils.GetByPlayerItemIdAsync(playerItemId, cancellationToken);
        if (existing is not null)
            return;

        await _playerSoils.CreateAsync(new PlayerSoilEntity
        {
            PlayerItemId = playerItemId,
            TotalUsedSeconds = 0,
            State = (int)PlayerSoilState.InInventory,
            InsertedPlotId = null,
            UpdatedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    private static bool IsExpired(DateTime? expireAtUtc)
    {
        return expireAtUtc.HasValue && expireAtUtc.Value <= DateTime.UtcNow;
    }
}
