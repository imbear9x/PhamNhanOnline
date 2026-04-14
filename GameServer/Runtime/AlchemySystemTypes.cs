namespace GameServer.Runtime;

public enum PillCategory
{
    Recovery = 1,
    Buff = 2,
    Breakthrough = 3,
    Special = 4
}

public enum PillUsageType
{
    ConsumeDirectly = 1,
    PassiveMaterial = 2
}

public enum PillEffectType
{
    RecoverHp = 1,
    RecoverMp = 2,
    AddBuffStat = 3,
    AddBreakthroughRate = 4,
    ClearDebuff = 5,
    Special = 6
}

public enum HerbMaturityRequirement
{
    None = 0,
    Mature = 1,
    Perfect = 2
}

public enum PlayerSoilState
{
    InInventory = 1,
    Inserted = 2,
    Depleted = 3
}

public enum PlayerHerbState
{
    InInventory = 1,
    Planting = 2
}

public enum HerbGrowthStage
{
    Seedling = 1,
    Mature = 2,
    Perfect = 3
}

public enum HerbHarvestOutputType
{
    Material = 1,
    Seed = 2
}

public sealed record PillEffectDefinition(
    int Id,
    int PillTemplateId,
    PillEffectType EffectType,
    int OrderIndex,
    CombatValueType? ValueType,
    decimal? BaseValue,
    decimal? RatioValue,
    int? DurationMs,
    CharacterStatType? StatType,
    string? Note);

public sealed record PillTemplateDefinition(
    int ItemTemplateId,
    PillCategory PillCategory,
    PillUsageType UsageType,
    int? CooldownMs,
    IReadOnlyList<PillEffectDefinition> Effects);

public sealed record PillRecipeInputDefinition(
    int Id,
    int PillRecipeTemplateId,
    int RequiredItemTemplateId,
    int RequiredQuantity,
    CraftConsumeMode ConsumeMode,
    bool IsOptional,
    double SuccessRateBonus,
    double MutationBonusRate,
    HerbMaturityRequirement RequiredHerbMaturity);

public sealed record PillRecipeMasteryStageDefinition(
    int Id,
    int PillRecipeTemplateId,
    int RequiredTotalCraftCount,
    double SuccessRateBonus);

public sealed record PillRecipeTemplateDefinition(
    int Id,
    string Code,
    string Name,
    int RecipeBookItemTemplateId,
    int ResultPillItemTemplateId,
    string? Description,
    long CraftDurationSeconds,
    double BaseSuccessRate,
    double? SuccessRateCap,
    double MutationRate,
    double MutationRateCap,
    IReadOnlyList<PillRecipeInputDefinition> Inputs,
    IReadOnlyList<PillRecipeMasteryStageDefinition> MasteryStages);

public sealed record SoilTemplateDefinition(
    int ItemTemplateId,
    decimal GrowthSpeedRate,
    long MaxActiveSeconds,
    string? Description);

public sealed record HerbGrowthStageDefinition(
    int Id,
    int HerbTemplateId,
    HerbGrowthStage Stage,
    string StageName,
    long RequiredGrowthSeconds);

public sealed record HerbHarvestOutputDefinition(
    int Id,
    int HerbTemplateId,
    HerbGrowthStage RequiredStage,
    HerbHarvestOutputType OutputType,
    int ResultItemTemplateId,
    int ResultQuantity,
    double OutputChance);

public sealed record HerbTemplateDefinition(
    int Id,
    string Code,
    string Name,
    int SeedItemTemplateId,
    int? ReplantItemTemplateId,
    string? Description,
    IReadOnlyList<HerbGrowthStageDefinition> GrowthStages,
    IReadOnlyList<HerbHarvestOutputDefinition> HarvestOutputs);

public sealed record LearnedPillRecipeView(
    int PillRecipeTemplateId,
    string Code,
    string Name,
    int ResultPillItemTemplateId,
    int TotalCraftCount,
    double CurrentSuccessRateBonus,
    DateTime LearnedAt);

public sealed record AlchemyValidationResult(
    bool Success,
    string? FailureReason,
    PillRecipeTemplateDefinition? Recipe,
    int RequestedCraftCount,
    int MaxCraftableCount,
    IReadOnlyList<long> ConsumedPlayerItemIds,
    IReadOnlyDictionary<long, int> ConsumedStackQuantities,
    IReadOnlyList<AlchemyOptionalInputSelection> AppliedOptionalInputs,
    double EffectiveSuccessRate,
    double EffectiveMutationRate,
    double BoostedSuccessRate,
    double BoostedMutationRate,
    int BoostedCraftCount);

public sealed record AlchemyOptionalInputSelection(
    PillRecipeInputDefinition Input,
    int AppliedCount);

public sealed record AlchemyCraftRatePlan(
    double EffectiveSuccessRate,
    double EffectiveMutationRate,
    double BoostedSuccessRate,
    double BoostedMutationRate,
    int BoostedCraftCount);

public sealed record AlchemyExecutionResult(
    bool Success,
    string? FailureReason,
    PillRecipeTemplateDefinition? Recipe,
    IReadOnlyList<InventoryItemView> CreatedItems,
    IReadOnlyList<long> ConsumedPlayerItemIds,
    IReadOnlyDictionary<long, int> ConsumedStackQuantities,
    double EffectiveSuccessRate,
    double EffectiveMutationRate);

public sealed record HerbRuntimeState(
    long PlayerHerbId,
    int HerbTemplateId,
    HerbGrowthStage CurrentStage,
    long AccumulatedGrowthSeconds,
    bool IsGrowing,
    long? CurrentPlotId,
    long? CurrentSoilPlayerItemId,
    long SoilRemainingSeconds);
