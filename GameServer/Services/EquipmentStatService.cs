using GameServer.Repositories;
using GameServer.Runtime;

namespace GameServer.Services;

public sealed class EquipmentStatService
{
    private readonly PlayerItemRepository _playerItems;
    private readonly PlayerEquipmentRepository _playerEquipments;
    private readonly PlayerEquipmentStatBonusRepository _playerEquipmentBonuses;
    private readonly ItemDefinitionCatalog _definitions;

    public EquipmentStatService(
        PlayerItemRepository playerItems,
        PlayerEquipmentRepository playerEquipments,
        PlayerEquipmentStatBonusRepository playerEquipmentBonuses,
        ItemDefinitionCatalog definitions)
    {
        _playerItems = playerItems;
        _playerEquipments = playerEquipments;
        _playerEquipmentBonuses = playerEquipmentBonuses;
        _definitions = definitions;
    }

    public async Task<ItemStatModifierBundle> BuildEquipmentStatModifiersAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        var inventory = await _playerItems.ListByPlayerIdAsync(playerId, cancellationToken);
        var playerItemIds = inventory.Select(x => x.Id).ToArray();
        var equipmentRows = await _playerEquipments.ListByPlayerItemIdsAsync(playerItemIds, cancellationToken);
        var equippedRows = equipmentRows.Where(x => x.EquippedSlot.HasValue).ToArray();
        if (equippedRows.Length == 0)
            return ItemStatModifierBundle.Empty;

        var bonuses = await _playerEquipmentBonuses.ListByPlayerItemIdsAsync(equippedRows.Select(x => x.PlayerItemId).ToArray(), cancellationToken);
        var bonusesByItemId = bonuses.GroupBy(x => x.PlayerItemId).ToDictionary(x => x.Key, x => x.ToArray());
        var inventoryById = inventory.ToDictionary(x => x.Id);
        var flat = new Dictionary<CharacterStatType, decimal>();
        var percent = new Dictionary<CharacterStatType, decimal>();

        foreach (var equipped in equippedRows)
        {
            if (!inventoryById.TryGetValue(equipped.PlayerItemId, out var playerItem))
                continue;
            if (!_definitions.TryGetItem(playerItem.ItemTemplateId, out var definition) || definition.Equipment is null)
                continue;

            ApplyModifiers(flat, percent, definition.Equipment.BaseStats);
            if (bonusesByItemId.TryGetValue(equipped.PlayerItemId, out var itemBonuses))
            {
                ApplyModifiers(flat, percent, itemBonuses.Select(x => new ItemStatModifierDefinition(
                    x.Id,
                    (CharacterStatType)x.StatType,
                    x.Value,
                    (CombatValueType)x.ValueType)));
            }
        }

        return new ItemStatModifierBundle(flat, percent);
    }

    private static void ApplyModifiers(
        IDictionary<CharacterStatType, decimal> flat,
        IDictionary<CharacterStatType, decimal> percent,
        IEnumerable<ItemStatModifierDefinition> modifiers)
    {
        foreach (var modifier in modifiers)
        {
            if (modifier.StatType == CharacterStatType.None)
                continue;

            var target = modifier.ValueType == CombatValueType.Percent ? percent : flat;
            target[modifier.StatType] = target.TryGetValue(modifier.StatType, out var current)
                ? current + modifier.Value
                : modifier.Value;
        }
    }
}
