using GameServer.Descriptions;
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
    private readonly GameplayDescriptionService _descriptions;

    public ItemService(
        ItemDefinitionCatalog definitions,
        PlayerItemRepository playerItems,
        PlayerEquipmentRepository playerEquipments,
        PlayerEquipmentStatBonusRepository playerEquipmentBonuses,
        PlayerSoilRepository playerSoils,
        GameplayDescriptionService descriptions)
    {
        _definitions = definitions;
        _playerItems = playerItems;
        _playerEquipments = playerEquipments;
        _playerEquipmentBonuses = playerEquipmentBonuses;
        _playerSoils = playerSoils;
        _descriptions = descriptions;
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
            var created = await CreateItemInstancesExactAsync(
                playerId,
                itemTemplateId,
                quantity,
                ItemLocationType.Inventory,
                isBound,
                expireAtUtc,
                cancellationToken);
            touched.AddRange(created);
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

        if (remaining > 0)
        {
            var created = await CreateItemInstancesExactAsync(
                playerId,
                itemTemplateId,
                remaining,
                ItemLocationType.Inventory,
                isBound,
                expireAtUtc,
                cancellationToken);
            touched.AddRange(created);
        }

        return touched;
    }

    public async Task<IReadOnlyList<PlayerItemEntity>> CreateGroundItemInstancesAsync(
        int itemTemplateId,
        int quantity,
        bool isBound = false,
        DateTime? expireAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        return await CreateItemInstancesExactAsync(
            ownerCharacterId: null,
            itemTemplateId,
            quantity,
            ItemLocationType.Ground,
            isBound,
            expireAtUtc,
            cancellationToken);
    }

    public async Task<PlayerItemEntity> MoveItemToGroundAsync(
        Guid playerId,
        long playerItemId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        var playerItem = await _playerItems.GetByIdAsync(playerItemId, cancellationToken)
                         ?? throw new InvalidOperationException($"Player item {playerItemId} was not found.");
        if (playerItem.PlayerId != playerId || playerItem.LocationType != (int)ItemLocationType.Inventory)
            throw new InvalidOperationException($"Player item {playerItemId} does not belong to player inventory {playerId}.");

        var definition = GetDefinition(playerItem.ItemTemplateId);
        if (!definition.IsDroppable)
            throw new InvalidOperationException($"Item template {playerItem.ItemTemplateId} is not droppable.");

        if (!definition.IsStackable)
        {
            if (quantity != 1)
                throw new InvalidOperationException($"Non-stackable item {playerItemId} can only move one instance to ground.");

            await EnsureItemCanLeaveInventoryAsync(playerItemId, cancellationToken);
            playerItem.PlayerId = null;
            playerItem.LocationType = (int)ItemLocationType.Ground;
            playerItem.UpdatedAt = DateTime.UtcNow;
            await _playerItems.UpdateAsync(playerItem, cancellationToken);
            return playerItem;
        }

        if (playerItem.Quantity < quantity)
            throw new InvalidOperationException($"Player item {playerItemId} does not have enough quantity.");

        if (playerItem.Quantity == quantity)
        {
            playerItem.PlayerId = null;
            playerItem.LocationType = (int)ItemLocationType.Ground;
            playerItem.UpdatedAt = DateTime.UtcNow;
            await _playerItems.UpdateAsync(playerItem, cancellationToken);
            return playerItem;
        }

        var split = await SplitItemStackAsync(playerId, playerItemId, quantity, cancellationToken);
        split.PlayerId = null;
        split.LocationType = (int)ItemLocationType.Ground;
        split.UpdatedAt = DateTime.UtcNow;
        await _playerItems.UpdateAsync(split, cancellationToken);
        return split;
    }

    public async Task<PlayerItemEntity> SplitItemStackAsync(
        Guid playerId,
        long playerItemId,
        int quantityToSplit,
        CancellationToken cancellationToken = default)
    {
        if (quantityToSplit <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantityToSplit));

        var source = await _playerItems.GetByIdAsync(playerItemId, cancellationToken)
                     ?? throw new InvalidOperationException($"Player item {playerItemId} was not found.");
        if (source.PlayerId != playerId || source.LocationType != (int)ItemLocationType.Inventory)
            throw new InvalidOperationException($"Player item {playerItemId} does not belong to player inventory {playerId}.");

        var definition = GetDefinition(source.ItemTemplateId);
        if (!definition.IsStackable)
            throw new InvalidOperationException($"Player item {playerItemId} is not stackable.");
        if (quantityToSplit >= source.Quantity)
            throw new InvalidOperationException($"Split quantity must be smaller than current stack quantity for player item {playerItemId}.");

        source.Quantity -= quantityToSplit;
        source.UpdatedAt = DateTime.UtcNow;
        await _playerItems.UpdateAsync(source, cancellationToken);

        var splitEntity = new PlayerItemEntity
        {
            PlayerId = source.PlayerId,
            ItemTemplateId = source.ItemTemplateId,
            LocationType = source.LocationType,
            Quantity = quantityToSplit,
            IsBound = source.IsBound,
            AcquiredAt = DateTime.UtcNow,
            ExpireAt = source.ExpireAt,
            UpdatedAt = DateTime.UtcNow
        };
        splitEntity.Id = await _playerItems.CreateAsync(splitEntity, cancellationToken);
        return splitEntity;
    }

    public async Task MoveGroundItemToInventoryAsync(
        Guid playerId,
        long playerItemId,
        CancellationToken cancellationToken = default)
    {
        var playerItem = await _playerItems.GetByIdAsync(playerItemId, cancellationToken)
                         ?? throw new InvalidOperationException($"Ground item {playerItemId} was not found.");
        if (playerItem.LocationType != (int)ItemLocationType.Ground)
            throw new InvalidOperationException($"Player item {playerItemId} is not on the ground.");

        var definition = GetDefinition(playerItem.ItemTemplateId);
        if (!definition.IsStackable)
        {
            playerItem.PlayerId = playerId;
            playerItem.LocationType = (int)ItemLocationType.Inventory;
            playerItem.UpdatedAt = DateTime.UtcNow;
            await _playerItems.UpdateAsync(playerItem, cancellationToken);
            return;
        }

        var remaining = playerItem.Quantity;
        var existingStacks = await _playerItems.ListByTemplateIdAsync(playerId, playerItem.ItemTemplateId, cancellationToken);
        foreach (var stack in existingStacks.Where(x => !IsExpired(x.ExpireAt) && x.IsBound == playerItem.IsBound && x.ExpireAt == playerItem.ExpireAt && x.Quantity < definition.MaxStack))
        {
            var availableCapacity = definition.MaxStack - stack.Quantity;
            if (availableCapacity <= 0)
                continue;

            var moved = Math.Min(remaining, availableCapacity);
            stack.Quantity += moved;
            stack.UpdatedAt = DateTime.UtcNow;
            await _playerItems.UpdateAsync(stack, cancellationToken);
            remaining -= moved;
            if (remaining <= 0)
                break;
        }

        if (remaining <= 0)
        {
            await DeleteItemInstanceAsync(playerItemId, cancellationToken);
            return;
        }

        playerItem.Quantity = remaining;
        playerItem.PlayerId = playerId;
        playerItem.LocationType = (int)ItemLocationType.Inventory;
        playerItem.UpdatedAt = DateTime.UtcNow;
        await _playerItems.UpdateAsync(playerItem, cancellationToken);
    }

    public async Task CleanupResidualGroundItemsAsync(CancellationToken cancellationToken = default)
    {
        var groundItems = await _playerItems.ListByLocationAsync(ItemLocationType.Ground, cancellationToken);
        if (groundItems.Count == 0)
            return;

        foreach (var item in groundItems)
            await DeleteItemInstanceAsync(item.Id, cancellationToken);
    }

    public async Task CleanupExpiredGroundItemsAsync(
        IReadOnlyCollection<long> playerItemIds,
        CancellationToken cancellationToken = default)
    {
        if (playerItemIds.Count == 0)
            return;

        var items = await _playerItems.ListByIdsAsync(playerItemIds, cancellationToken);
        foreach (var item in items)
        {
            if (item.LocationType != (int)ItemLocationType.Ground)
                continue;

            await DeleteItemInstanceAsync(item.Id, cancellationToken);
        }
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
                await RemovePlayerItemAsync(playerId, removable[i].Id, cancellationToken);

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
                await DeleteItemInstanceAsync(item.Id, cancellationToken);
            else
                await _playerItems.UpdateAsync(item, cancellationToken);
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
        if (playerItem.PlayerId != playerId || playerItem.LocationType != (int)ItemLocationType.Inventory)
            throw new InvalidOperationException($"Player item {playerItemId} does not belong to player inventory {playerId}.");

        await EnsureItemCanLeaveInventoryAsync(playerItemId, cancellationToken);
        await DeleteItemInstanceAsync(playerItemId, cancellationToken);
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
        if (playerItem.PlayerId != playerId || playerItem.LocationType != (int)ItemLocationType.Inventory)
            throw new InvalidOperationException($"Player item {playerItemId} does not belong to player inventory {playerId}.");

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
            await DeleteItemInstanceAsync(playerItemId, cancellationToken);
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
                    item.PlayerId ?? throw new InvalidOperationException($"Inventory item {item.Id} is missing owner."),
                    definition,
                    _descriptions.BuildItemDescription(definition),
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

    private async Task<IReadOnlyList<PlayerItemEntity>> CreateItemInstancesExactAsync(
        Guid? ownerCharacterId,
        int itemTemplateId,
        int quantity,
        ItemLocationType locationType,
        bool isBound,
        DateTime? expireAtUtc,
        CancellationToken cancellationToken)
    {
        var definition = GetDefinition(itemTemplateId);
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        var created = new List<PlayerItemEntity>();
        if (!definition.IsStackable)
        {
            for (var i = 0; i < quantity; i++)
            {
                var entity = new PlayerItemEntity
                {
                    PlayerId = ownerCharacterId,
                    ItemTemplateId = itemTemplateId,
                    LocationType = (int)locationType,
                    Quantity = 1,
                    IsBound = isBound,
                    AcquiredAt = DateTime.UtcNow,
                    ExpireAt = expireAtUtc,
                    UpdatedAt = DateTime.UtcNow
                };
                entity.Id = await _playerItems.CreateAsync(entity, cancellationToken);
                await EnsureEquipmentRecordIfNeededAsync(definition, entity.Id, cancellationToken);
                await EnsureSoilRecordIfNeededAsync(definition, entity.Id, cancellationToken);
                created.Add(entity);
            }

            return created;
        }

        var remaining = quantity;
        while (remaining > 0)
        {
            var stackQuantity = Math.Min(remaining, definition.MaxStack);
            var entity = new PlayerItemEntity
            {
                PlayerId = ownerCharacterId,
                ItemTemplateId = itemTemplateId,
                LocationType = (int)locationType,
                Quantity = stackQuantity,
                IsBound = isBound,
                AcquiredAt = DateTime.UtcNow,
                ExpireAt = expireAtUtc,
                UpdatedAt = DateTime.UtcNow
            };
            entity.Id = await _playerItems.CreateAsync(entity, cancellationToken);
            created.Add(entity);
            remaining -= stackQuantity;
        }

        return created;
    }

    private async Task EnsureItemCanLeaveInventoryAsync(long playerItemId, CancellationToken cancellationToken)
    {
        var equipment = await _playerEquipments.GetByPlayerItemIdAsync(playerItemId, cancellationToken);
        if (equipment?.EquippedSlot.HasValue == true)
            throw new InvalidOperationException($"Player item {playerItemId} is currently equipped and cannot be removed.");

        var soil = await _playerSoils.GetByPlayerItemIdAsync(playerItemId, cancellationToken);
        if (soil is not null && soil.State == (int)PlayerSoilState.Inserted)
            throw new InvalidOperationException($"Player item {playerItemId} is currently inserted into a garden plot and cannot be removed.");
    }

    private async Task DeleteItemInstanceAsync(long playerItemId, CancellationToken cancellationToken)
    {
        await _playerEquipmentBonuses.DeleteByPlayerItemIdAsync(playerItemId, cancellationToken);

        var equipment = await _playerEquipments.GetByPlayerItemIdAsync(playerItemId, cancellationToken);
        if (equipment is not null)
            await _playerEquipments.DeleteAsync(playerItemId, cancellationToken);

        var soil = await _playerSoils.GetByPlayerItemIdAsync(playerItemId, cancellationToken);
        if (soil is not null)
            await _playerSoils.DeleteAsync(playerItemId, cancellationToken);

        await _playerItems.DeleteAsync(playerItemId, cancellationToken);
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
