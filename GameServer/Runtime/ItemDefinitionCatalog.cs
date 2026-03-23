using GameServer.Entities;
using GameServer.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace GameServer.Runtime;

public sealed class ItemDefinitionCatalog
{
    private readonly IReadOnlyDictionary<int, ItemDefinition> _itemsById;
    private readonly IReadOnlyDictionary<string, ItemDefinition> _itemsByCode;
    private readonly IReadOnlyDictionary<int, CraftRecipeDefinition> _craftRecipesById;

    public ItemDefinitionCatalog(IServiceScopeFactory scopeFactory)
    {
        using var scope = scopeFactory.CreateScope();
        var itemTemplates = scope.ServiceProvider.GetRequiredService<ItemTemplateRepository>().GetAllAsync().GetAwaiter().GetResult();
        var equipmentTemplates = scope.ServiceProvider.GetRequiredService<EquipmentTemplateRepository>().GetAllAsync().GetAwaiter().GetResult();
        var equipmentTemplateStats = scope.ServiceProvider.GetRequiredService<EquipmentTemplateStatRepository>().GetAllAsync().GetAwaiter().GetResult();
        var martialArtBooks = scope.ServiceProvider.GetRequiredService<MartialArtBookTemplateRepository>().GetAllAsync().GetAwaiter().GetResult();
        var craftRecipes = scope.ServiceProvider.GetRequiredService<CraftRecipeRepository>().GetAllAsync().GetAwaiter().GetResult();
        var craftRequirements = scope.ServiceProvider.GetRequiredService<CraftRecipeRequirementRepository>().GetAllAsync().GetAwaiter().GetResult();
        var craftMutationBonuses = scope.ServiceProvider.GetRequiredService<CraftRecipeMutationBonusRepository>().GetAllAsync().GetAwaiter().GetResult();

        _itemsById = BuildItems(itemTemplates, equipmentTemplates, equipmentTemplateStats, martialArtBooks);
        _itemsByCode = _itemsById.Values.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
        _craftRecipesById = BuildCraftRecipes(craftRecipes, craftRequirements, craftMutationBonuses);
    }

    public bool TryGetItem(int itemTemplateId, out ItemDefinition definition) =>
        _itemsById.TryGetValue(itemTemplateId, out definition!);

    public bool TryGetItemByCode(string code, out ItemDefinition definition) =>
        _itemsByCode.TryGetValue(code, out definition!);

    public bool TryGetCraftRecipe(int recipeId, out CraftRecipeDefinition definition) =>
        _craftRecipesById.TryGetValue(recipeId, out definition!);

    public IReadOnlyCollection<ItemDefinition> GetAllItems() => _itemsById.Values.ToArray();

    public IReadOnlyCollection<CraftRecipeDefinition> GetAllCraftRecipes() => _craftRecipesById.Values.ToArray();

    private static IReadOnlyDictionary<int, ItemDefinition> BuildItems(
        IReadOnlyCollection<ItemTemplateEntity> itemTemplates,
        IReadOnlyCollection<EquipmentTemplateEntity> equipmentTemplates,
        IReadOnlyCollection<EquipmentTemplateStatEntity> equipmentTemplateStats,
        IReadOnlyCollection<MartialArtBookTemplateEntity> martialArtBooks)
    {
        var statDefsByEquipmentId = equipmentTemplateStats
            .GroupBy(x => x.EquipmentTemplateId)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<ItemStatModifierDefinition>)x
                    .OrderBy(stat => stat.Id)
                    .Select(stat => new ItemStatModifierDefinition(
                        stat.Id,
                        (CharacterStatType)stat.StatType,
                        stat.Value,
                        (CombatValueType)stat.ValueType))
                    .ToArray());

        var equipmentByTemplateId = equipmentTemplates.ToDictionary(
            x => x.ItemTemplateId,
            x => new EquipmentDefinition(
                x.ItemTemplateId,
                (EquipmentSlot)x.SlotType,
                (EquipmentType)x.EquipmentType,
                x.LevelRequirement,
                statDefsByEquipmentId.GetValueOrDefault(x.ItemTemplateId, Array.Empty<ItemStatModifierDefinition>())));

        var martialArtBooksByItemId = martialArtBooks.ToDictionary(
            x => x.ItemTemplateId,
            x => new MartialArtBookDefinition(x.ItemTemplateId, x.MartialArtId));

        var result = new Dictionary<int, ItemDefinition>(itemTemplates.Count);
        foreach (var template in itemTemplates)
        {
            var itemType = (ItemType)template.ItemType;
            var definition = new ItemDefinition(
                template.Id,
                template.Code,
                template.Name,
                itemType,
                (ItemRarity)template.Rarity,
                template.MaxStack,
                template.IsTradeable,
                template.IsDroppable,
                template.IsDestroyable,
                template.Icon,
                template.BackgroundIcon,
                template.Description,
                equipmentByTemplateId.GetValueOrDefault(template.Id),
                martialArtBooksByItemId.GetValueOrDefault(template.Id));

            ValidateItemDefinition(definition);
            result[template.Id] = definition;
        }

        return result;
    }

    private static IReadOnlyDictionary<int, CraftRecipeDefinition> BuildCraftRecipes(
        IReadOnlyCollection<CraftRecipeEntity> craftRecipes,
        IReadOnlyCollection<CraftRecipeRequirementEntity> craftRequirements,
        IReadOnlyCollection<CraftRecipeMutationBonusEntity> craftMutationBonuses)
    {
        var requirementsByRecipeId = craftRequirements
            .GroupBy(x => x.CraftRecipeId)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<CraftRecipeRequirementDefinition>)x
                    .OrderBy(requirement => requirement.IsOptional)
                    .ThenBy(requirement => requirement.Id)
                    .Select(requirement => new CraftRecipeRequirementDefinition(
                        requirement.Id,
                        requirement.CraftRecipeId,
                        requirement.RequiredItemTemplateId,
                        requirement.RequiredQuantity,
                        (CraftConsumeMode)requirement.ConsumeMode,
                        requirement.IsOptional,
                        requirement.MutationBonusRate))
                    .ToArray());

        var mutationBonusesByRecipeId = craftMutationBonuses
            .GroupBy(x => x.CraftRecipeId)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<ItemStatModifierDefinition>)x
                    .OrderBy(bonus => bonus.Id)
                    .Select(bonus => new ItemStatModifierDefinition(
                        bonus.Id,
                        (CharacterStatType)bonus.StatType,
                        bonus.Value,
                        (CombatValueType)bonus.ValueType))
                    .ToArray());

        return craftRecipes.ToDictionary(
            x => x.Id,
            x => new CraftRecipeDefinition(
                x.Id,
                x.Code,
                x.Name,
                x.ResultItemTemplateId,
                x.ResultQuantity,
                x.SuccessRate,
                x.MutationRate,
                x.MutationRateCap,
                x.CostCurrencyType,
                x.CostCurrencyValue,
                x.Description,
                requirementsByRecipeId.GetValueOrDefault(x.Id, Array.Empty<CraftRecipeRequirementDefinition>()),
                mutationBonusesByRecipeId.GetValueOrDefault(x.Id, Array.Empty<ItemStatModifierDefinition>())));
    }

    private static void ValidateItemDefinition(ItemDefinition definition)
    {
        if (definition.MaxStack <= 0)
            throw new InvalidOperationException($"Item template {definition.Id} has invalid max stack {definition.MaxStack}.");

        if (definition.ItemType == ItemType.Equipment && definition.MaxStack != 1)
            throw new InvalidOperationException($"Equipment item template {definition.Id} must have max_stack = 1.");

        if (definition.ItemType == ItemType.Soil && definition.MaxStack != 1)
            throw new InvalidOperationException($"Soil item template {definition.Id} must have max_stack = 1.");

        if (definition.ItemType == ItemType.HerbPlant && definition.MaxStack != 1)
            throw new InvalidOperationException($"Herb plant item template {definition.Id} must have max_stack = 1.");

        if (definition.Equipment is not null && definition.ItemType != ItemType.Equipment)
            throw new InvalidOperationException($"Item template {definition.Id} has equipment data but item_type is not Equipment.");

        if (definition.MartialArtBook is not null && definition.ItemType != ItemType.MartialArtBook)
            throw new InvalidOperationException($"Item template {definition.Id} has martial art book data but item_type is not MartialArtBook.");
    }
}
