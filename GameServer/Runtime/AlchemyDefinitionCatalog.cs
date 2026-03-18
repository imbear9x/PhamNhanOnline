using GameServer.Entities;
using GameServer.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace GameServer.Runtime;

public sealed class AlchemyDefinitionCatalog
{
    private readonly IReadOnlyDictionary<int, PillTemplateDefinition> _pillTemplatesByItemId;
    private readonly IReadOnlyDictionary<int, PillRecipeTemplateDefinition> _pillRecipesById;
    private readonly IReadOnlyDictionary<int, PillRecipeTemplateDefinition> _pillRecipesByBookItemId;
    private readonly IReadOnlyDictionary<int, HerbTemplateDefinition> _herbsById;
    private readonly IReadOnlyDictionary<int, HerbTemplateDefinition> _herbsBySeedItemId;
    private readonly IReadOnlyDictionary<int, HerbTemplateDefinition> _herbsByReplantItemId;
    private readonly IReadOnlyDictionary<int, SoilTemplateDefinition> _soilsByItemId;

    public AlchemyDefinitionCatalog(IServiceScopeFactory scopeFactory)
    {
        using var scope = scopeFactory.CreateScope();
        var pillTemplates = scope.ServiceProvider.GetRequiredService<PillTemplateRepository>().GetAllAsync().GetAwaiter().GetResult();
        var pillEffects = scope.ServiceProvider.GetRequiredService<PillEffectRepository>().GetAllAsync().GetAwaiter().GetResult();
        var pillRecipes = scope.ServiceProvider.GetRequiredService<PillRecipeTemplateRepository>().GetAllAsync().GetAwaiter().GetResult();
        var pillRecipeInputs = scope.ServiceProvider.GetRequiredService<PillRecipeInputRepository>().GetAllAsync().GetAwaiter().GetResult();
        var pillRecipeMasteryStages = scope.ServiceProvider.GetRequiredService<PillRecipeMasteryStageRepository>().GetAllAsync().GetAwaiter().GetResult();
        var herbTemplates = scope.ServiceProvider.GetRequiredService<HerbTemplateRepository>().GetAllAsync().GetAwaiter().GetResult();
        var herbGrowthStages = scope.ServiceProvider.GetRequiredService<HerbGrowthStageConfigRepository>().GetAllAsync().GetAwaiter().GetResult();
        var herbHarvestOutputs = scope.ServiceProvider.GetRequiredService<HerbHarvestOutputRepository>().GetAllAsync().GetAwaiter().GetResult();
        var soilTemplates = scope.ServiceProvider.GetRequiredService<SoilTemplateRepository>().GetAllAsync().GetAwaiter().GetResult();

        _pillTemplatesByItemId = BuildPillTemplates(pillTemplates, pillEffects);
        _pillRecipesById = BuildPillRecipes(pillRecipes, pillRecipeInputs, pillRecipeMasteryStages);
        _pillRecipesByBookItemId = _pillRecipesById.Values.ToDictionary(x => x.RecipeBookItemTemplateId);
        _herbsById = BuildHerbs(herbTemplates, herbGrowthStages, herbHarvestOutputs);
        _herbsBySeedItemId = _herbsById.Values.ToDictionary(x => x.SeedItemTemplateId);
        _herbsByReplantItemId = _herbsById.Values
            .Where(x => x.ReplantItemTemplateId.HasValue)
            .ToDictionary(x => x.ReplantItemTemplateId!.Value);
        _soilsByItemId = soilTemplates.ToDictionary(
            x => x.ItemTemplateId,
            x => new SoilTemplateDefinition(x.ItemTemplateId, x.GrowthSpeedRate, x.MaxActiveSeconds, x.Description));
    }

    public bool TryGetPillTemplate(int itemTemplateId, out PillTemplateDefinition definition) =>
        _pillTemplatesByItemId.TryGetValue(itemTemplateId, out definition!);

    public bool TryGetPillRecipe(int recipeId, out PillRecipeTemplateDefinition definition) =>
        _pillRecipesById.TryGetValue(recipeId, out definition!);

    public bool TryGetPillRecipeByBookItemTemplate(int itemTemplateId, out PillRecipeTemplateDefinition definition) =>
        _pillRecipesByBookItemId.TryGetValue(itemTemplateId, out definition!);

    public bool TryGetHerb(int herbTemplateId, out HerbTemplateDefinition definition) =>
        _herbsById.TryGetValue(herbTemplateId, out definition!);

    public bool TryGetHerbBySeedItemTemplate(int seedItemTemplateId, out HerbTemplateDefinition definition) =>
        _herbsBySeedItemId.TryGetValue(seedItemTemplateId, out definition!);

    public bool TryGetHerbByReplantItemTemplate(int replantItemTemplateId, out HerbTemplateDefinition definition) =>
        _herbsByReplantItemId.TryGetValue(replantItemTemplateId, out definition!);

    public bool TryGetSoil(int itemTemplateId, out SoilTemplateDefinition definition) =>
        _soilsByItemId.TryGetValue(itemTemplateId, out definition!);

    public IReadOnlyCollection<PillRecipeTemplateDefinition> GetAllPillRecipes() => _pillRecipesById.Values.ToArray();

    public IReadOnlyCollection<HerbTemplateDefinition> GetAllHerbs() => _herbsById.Values.ToArray();

    private static IReadOnlyDictionary<int, PillTemplateDefinition> BuildPillTemplates(
        IReadOnlyCollection<PillTemplateEntity> pillTemplates,
        IReadOnlyCollection<PillEffectEntity> pillEffects)
    {
        var effectsByTemplateId = pillEffects
            .GroupBy(x => x.PillTemplateId)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<PillEffectDefinition>)x
                    .OrderBy(effect => effect.OrderIndex)
                    .Select(effect => new PillEffectDefinition(
                        effect.Id,
                        effect.PillTemplateId,
                        (PillEffectType)effect.EffectType,
                        effect.OrderIndex,
                        effect.ValueType.HasValue ? (CombatValueType)effect.ValueType.Value : null,
                        effect.BaseValue,
                        effect.RatioValue,
                        effect.DurationMs,
                        effect.StatType.HasValue ? (CharacterStatType)effect.StatType.Value : null,
                        effect.Note))
                    .ToArray());

        return pillTemplates.ToDictionary(
            x => x.ItemTemplateId,
            x => new PillTemplateDefinition(
                x.ItemTemplateId,
                (PillCategory)x.PillCategory,
                (PillUsageType)x.UsageType,
                x.CooldownMs,
                effectsByTemplateId.GetValueOrDefault(x.ItemTemplateId, Array.Empty<PillEffectDefinition>())));
    }

    private static IReadOnlyDictionary<int, PillRecipeTemplateDefinition> BuildPillRecipes(
        IReadOnlyCollection<PillRecipeTemplateEntity> pillRecipes,
        IReadOnlyCollection<PillRecipeInputEntity> inputs,
        IReadOnlyCollection<PillRecipeMasteryStageEntity> masteryStages)
    {
        var inputsByRecipeId = inputs
            .GroupBy(x => x.PillRecipeTemplateId)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<PillRecipeInputDefinition>)x
                    .OrderBy(input => input.IsOptional)
                    .ThenBy(input => input.Id)
                    .Select(input => new PillRecipeInputDefinition(
                        input.Id,
                        input.PillRecipeTemplateId,
                        input.RequiredItemTemplateId,
                        input.RequiredQuantity,
                        (CraftConsumeMode)input.ConsumeMode,
                        input.IsOptional,
                        input.SuccessRateBonus,
                        input.MutationBonusRate,
                        (HerbMaturityRequirement)input.RequiredHerbMaturity))
                    .ToArray());

        var masteryByRecipeId = masteryStages
            .GroupBy(x => x.PillRecipeTemplateId)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<PillRecipeMasteryStageDefinition>)x
                    .OrderBy(stage => stage.RequiredTotalCraftCount)
                    .Select(stage => new PillRecipeMasteryStageDefinition(
                        stage.Id,
                        stage.PillRecipeTemplateId,
                        stage.RequiredTotalCraftCount,
                        stage.SuccessRateBonus))
                    .ToArray());

        return pillRecipes.ToDictionary(
            x => x.Id,
            x => new PillRecipeTemplateDefinition(
                x.Id,
                x.Code,
                x.Name,
                x.RecipeBookItemTemplateId,
                x.ResultPillItemTemplateId,
                x.Description,
                x.BaseSuccessRate,
                x.SuccessRateCap,
                x.MutationRate,
                x.MutationRateCap,
                inputsByRecipeId.GetValueOrDefault(x.Id, Array.Empty<PillRecipeInputDefinition>()),
                masteryByRecipeId.GetValueOrDefault(x.Id, Array.Empty<PillRecipeMasteryStageDefinition>())));
    }

    private static IReadOnlyDictionary<int, HerbTemplateDefinition> BuildHerbs(
        IReadOnlyCollection<HerbTemplateEntity> herbs,
        IReadOnlyCollection<HerbGrowthStageConfigEntity> growthStages,
        IReadOnlyCollection<HerbHarvestOutputEntity> harvestOutputs)
    {
        var growthStagesByHerbId = growthStages
            .GroupBy(x => x.HerbTemplateId)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<HerbGrowthStageDefinition>)x
                    .OrderBy(stage => stage.RequiredGrowthSeconds)
                    .ThenBy(stage => stage.Stage)
                    .Select(stage => new HerbGrowthStageDefinition(
                        stage.Id,
                        stage.HerbTemplateId,
                        (HerbGrowthStage)stage.Stage,
                        stage.StageName,
                        stage.RequiredGrowthSeconds))
                    .ToArray());

        var harvestOutputsByHerbId = harvestOutputs
            .GroupBy(x => x.HerbTemplateId)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<HerbHarvestOutputDefinition>)x
                    .OrderBy(output => output.RequiredStage)
                    .ThenBy(output => output.Id)
                    .Select(output => new HerbHarvestOutputDefinition(
                        output.Id,
                        output.HerbTemplateId,
                        (HerbGrowthStage)output.RequiredStage,
                        (HerbHarvestOutputType)output.OutputType,
                        output.ResultItemTemplateId,
                        output.ResultQuantity,
                        output.OutputChance))
                    .ToArray());

        return herbs.ToDictionary(
            x => x.Id,
            x => new HerbTemplateDefinition(
                x.Id,
                x.Code,
                x.Name,
                x.SeedItemTemplateId,
                x.ReplantItemTemplateId,
                x.Description,
                growthStagesByHerbId.GetValueOrDefault(x.Id, Array.Empty<HerbGrowthStageDefinition>()),
                harvestOutputsByHerbId.GetValueOrDefault(x.Id, Array.Empty<HerbHarvestOutputDefinition>())));
    }
}
