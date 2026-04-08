namespace GameServer.Runtime;

public enum ItemType
{
    Equipment = 1,
    Consumable = 2,
    Material = 3,
    Talisman = 4,
    MartialArtBook = 5,
    Currency = 6,
    QuestItem = 7,
    PillRecipeBook = 8,
    HerbSeed = 9,
    HerbMaterial = 10,
    Soil = 11,
    HerbPlant = 12
}

public enum ItemLocationType
{
    Inventory = 1,
    Ground = 2,
    TradeHold = 3,
    Mail = 4,
    Storage = 5
}

public enum ItemRarity
{
    Common = 1,
    Uncommon = 2,
    Rare = 3,
    Epic = 4,
    Legendary = 5
}

public enum EquipmentSlot
{
    Weapon = 1,
    Armor = 2,
    Pants = 3,
    Shoes = 4
}

public enum EquipmentType
{
    Sword = 1,
    Bow = 2,
    Armor = 3,
    Pants = 4,
    Shoes = 5
}

public enum EquipmentBonusSourceType
{
    DropBonus = 1,
    CraftBonus = 2,
    MutationBonus = 3,
    RefineBonus = 4,
    EventBonus = 5
}

public enum CraftConsumeMode
{
    Consume = 1
}

public sealed record ItemStatModifierDefinition(
    long Id,
    CharacterStatType StatType,
    decimal Value,
    CombatValueType ValueType);

public sealed record EquipmentDefinition(
    int ItemTemplateId,
    EquipmentSlot SlotType,
    EquipmentType EquipmentType,
    int LevelRequirement,
    IReadOnlyList<ItemStatModifierDefinition> BaseStats);

public sealed record MartialArtBookDefinition(
    int ItemTemplateId,
    int MartialArtId);

public sealed record ItemDefinition(
    int Id,
    string Code,
    string Name,
    ItemType ItemType,
    ItemRarity Rarity,
    int MaxStack,
    bool IsTradeable,
    bool IsDroppable,
    bool IsDestroyable,
    string? Icon,
    string? BackgroundIcon,
    string? Description,
    string? DescriptionTemplate,
    EquipmentDefinition? Equipment,
    MartialArtBookDefinition? MartialArtBook)
{
    public bool IsStackable => MaxStack > 1;
}

public sealed record CraftRecipeRequirementDefinition(
    int Id,
    int CraftRecipeId,
    int RequiredItemTemplateId,
    int RequiredQuantity,
    CraftConsumeMode ConsumeMode,
    bool IsOptional,
    double MutationBonusRate);

public sealed record CraftRecipeDefinition(
    int Id,
    string Code,
    string Name,
    int ResultItemTemplateId,
    int ResultQuantity,
    double SuccessRate,
    double MutationRate,
    double MutationRateCap,
    int? CostCurrencyType,
    long CostCurrencyValue,
    string? Description,
    IReadOnlyList<CraftRecipeRequirementDefinition> Requirements,
    IReadOnlyList<ItemStatModifierDefinition> MutationBonuses);

public sealed record InventoryItemView(
    long PlayerItemId,
    Guid PlayerId,
    ItemDefinition Definition,
    string? Description,
    int Quantity,
    bool IsBound,
    DateTime AcquiredAt,
    DateTime? ExpireAt,
    bool IsEquipped,
    EquipmentSlot? EquippedSlot,
    int EnhanceLevel,
    int? Durability);

public sealed record EquippedItemView(
    long PlayerItemId,
    ItemDefinition Definition,
    EquipmentDefinition Equipment,
    EquipmentSlot EquippedSlot,
    int EnhanceLevel,
    int? Durability);

public sealed record ItemStatModifierBundle(
    IReadOnlyDictionary<CharacterStatType, decimal> FlatValues,
    IReadOnlyDictionary<CharacterStatType, decimal> PercentValues)
{
    public static ItemStatModifierBundle Empty { get; } = new(
        new Dictionary<CharacterStatType, decimal>(),
        new Dictionary<CharacterStatType, decimal>());
}

public sealed record CraftValidationResult(
    bool Success,
    string? FailureReason,
    CraftRecipeDefinition? Recipe,
    IReadOnlyList<long> ConsumedPlayerItemIds,
    IReadOnlyDictionary<long, int> ConsumedStackQuantities,
    IReadOnlyList<CraftRecipeRequirementDefinition> AppliedOptionalRequirements,
    double EffectiveMutationRate);

public sealed record CraftExecutionResult(
    bool Success,
    string? FailureReason,
    CraftRecipeDefinition? Recipe,
    bool MutationTriggered,
    IReadOnlyList<InventoryItemView> CreatedItems,
    IReadOnlyList<long> ConsumedPlayerItemIds,
    IReadOnlyDictionary<long, int> ConsumedStackQuantities,
    double EffectiveMutationRate);
