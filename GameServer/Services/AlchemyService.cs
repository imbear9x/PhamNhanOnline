using GameServer.Entities;
using GameServer.Repositories;
using GameServer.Runtime;
using GameShared.Models;

namespace GameServer.Services;

public sealed class AlchemyService
{
    private readonly AlchemyDefinitionCatalog _definitions;
    private readonly ItemDefinitionCatalog _itemDefinitions;
    private readonly PlayerPillRecipeRepository _playerPillRecipes;
    private readonly PlayerItemRepository _playerItems;
    private readonly PlayerEquipmentRepository _playerEquipments;
    private readonly PlayerSoilRepository _playerSoils;

    public AlchemyService(
        GameDb db,
        AlchemyDefinitionCatalog definitions,
        ItemDefinitionCatalog itemDefinitions,
        PlayerPillRecipeRepository playerPillRecipes,
        PlayerItemRepository playerItems,
        PlayerEquipmentRepository playerEquipments,
        PlayerSoilRepository playerSoils,
        ItemService itemService,
        GameServer.Randomness.IGameRandomService randomService)
    {
        _definitions = definitions;
        _itemDefinitions = itemDefinitions;
        _playerPillRecipes = playerPillRecipes;
        _playerItems = playerItems;
        _playerEquipments = playerEquipments;
        _playerSoils = playerSoils;
    }

    public async Task<AlchemyValidationResult> ValidateCraftPillAsync(
        Guid playerId,
        int recipeId,
        int requestedCraftCount,
        IReadOnlyCollection<long>? selectedPlayerItemIds,
        IReadOnlyCollection<AlchemyOptionalInputSelectionModel>? selectedOptionalInputs = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedRequestedCraftCount = Math.Max(1, requestedCraftCount);
        if (!_definitions.TryGetPillRecipe(recipeId, out var recipe))
            return Failed("Dan phuong khong ton tai.", null, normalizedRequestedCraftCount);

        var learned = await _playerPillRecipes.GetByPlayerAndRecipeAsync(playerId, recipeId, cancellationToken);
        if (learned is null)
            return Failed("Player chua hoc dan phuong nay.", recipe, normalizedRequestedCraftCount);

        if (recipe.Inputs.Any(static x => x.RequiredHerbMaturity != HerbMaturityRequirement.None))
            return Failed("Recipe co required_herb_maturity, tinh nang nay de phase sau.", recipe, normalizedRequestedCraftCount);

        var inventory = (await _playerItems.ListByPlayerIdAsync(playerId, cancellationToken))
            .Where(static x => !IsExpired(x.ExpireAt))
            .ToArray();
        var selectedIds = (selectedPlayerItemIds ?? Array.Empty<long>()).Distinct().ToArray();
        var selectedItems = inventory.Where(x => selectedIds.Contains(x.Id)).ToDictionary(static x => x.Id);
        if (selectedItems.Count != selectedIds.Length)
            return Failed("Co player_item_id khong hop le hoac khong thuoc player.", recipe, normalizedRequestedCraftCount);

        var optionalSelectionByInputId = NormalizeOptionalSelections(selectedOptionalInputs);
        var unknownOptionalInputId = optionalSelectionByInputId.Keys
            .FirstOrDefault(inputId => !recipe.Inputs.Any(input => input.IsOptional && input.Id == inputId));
        if (unknownOptionalInputId > 0)
            return Failed("Catalyst khong hop le voi dan phuong nay.", recipe, normalizedRequestedCraftCount);

        var inventoryIds = inventory.Select(static x => x.Id).ToArray();
        var equipmentRows = await _playerEquipments.ListByPlayerItemIdsAsync(inventoryIds, cancellationToken);
        var equipmentByItemId = equipmentRows.ToDictionary(static x => x.PlayerItemId);
        var soilRows = await _playerSoils.ListByPlayerItemIdsAsync(inventoryIds, cancellationToken);
        var soilByItemId = soilRows.ToDictionary(static x => x.PlayerItemId);
        var mandatoryInputs = recipe.Inputs.Where(static x => !x.IsOptional).ToArray();
        var maxCraftableCount = mandatoryInputs.Length == 0
            ? 0
            : mandatoryInputs.Min(input => CalculateMaxCraftableCount(
                input,
                inventory,
                selectedItems,
                equipmentByItemId,
                soilByItemId));
        if (maxCraftableCount <= 0)
            return Failed("Khong du nguyen lieu bat buoc de luyen dan.", recipe, normalizedRequestedCraftCount);
        if (normalizedRequestedCraftCount > maxCraftableCount)
        {
            return Failed(
                $"Khong du nguyen lieu de luyen {normalizedRequestedCraftCount} vien. Toi da {maxCraftableCount} vien.",
                recipe,
                normalizedRequestedCraftCount,
                maxCraftableCount);
        }

        var consumedPlayerItemIds = new List<long>();
        var consumedStackQuantities = new Dictionary<long, int>();
        var appliedOptionalInputs = new List<AlchemyOptionalInputSelection>();
        var allocatedSelectedItemIds = new HashSet<long>();

        foreach (var input in mandatoryInputs)
        {
            var failure = TryAllocateInput(
                input,
                normalizedRequestedCraftCount,
                inventory,
                selectedItems,
                equipmentByItemId,
                soilByItemId,
                allocatedSelectedItemIds,
                consumedPlayerItemIds,
                consumedStackQuantities,
                allowPartial: false,
                out _);
            if (failure is not null)
                return Failed(failure, recipe, normalizedRequestedCraftCount, maxCraftableCount);
        }

        var remainingBoostableCrafts = normalizedRequestedCraftCount;
        foreach (var input in recipe.Inputs.Where(static x => x.IsOptional))
        {
            if (!optionalSelectionByInputId.TryGetValue(input.Id, out var requestedApplications) || requestedApplications <= 0)
                continue;

            var failure = TryAllocateInput(
                input,
                Math.Min(requestedApplications, remainingBoostableCrafts),
                inventory,
                selectedItems,
                equipmentByItemId,
                soilByItemId,
                allocatedSelectedItemIds,
                consumedPlayerItemIds,
                consumedStackQuantities,
                allowPartial: true,
                out var appliedApplications);
            if (failure is not null)
                return Failed(failure, recipe, normalizedRequestedCraftCount, maxCraftableCount);
            if (appliedApplications <= 0)
                continue;

            appliedOptionalInputs.Add(new AlchemyOptionalInputSelection(input, appliedApplications));
            remainingBoostableCrafts -= appliedApplications;
            if (remainingBoostableCrafts <= 0)
                break;
        }

        var ratePlan = BuildCraftRatePlan(recipe, learned.TotalCraftCount, learned.CurrentSuccessRateBonus, normalizedRequestedCraftCount, appliedOptionalInputs);
        return new AlchemyValidationResult(
            true,
            null,
            recipe,
            normalizedRequestedCraftCount,
            maxCraftableCount,
            consumedPlayerItemIds.ToArray(),
            new Dictionary<long, int>(consumedStackQuantities),
            appliedOptionalInputs.ToArray(),
            ratePlan.EffectiveSuccessRate,
            ratePlan.EffectiveMutationRate,
            ratePlan.BoostedSuccessRate,
            ratePlan.BoostedMutationRate,
            ratePlan.BoostedCraftCount);
    }

    public async Task<AlchemyCraftRatePlan> BuildCraftRatePlanAsync(
        Guid playerId,
        int recipeId,
        int requestedCraftCount,
        IReadOnlyCollection<PracticeOptionalInputEntry>? selectedOptionalInputs = null,
        CancellationToken cancellationToken = default)
    {
        if (!_definitions.TryGetPillRecipe(recipeId, out var recipe))
            return new AlchemyCraftRatePlan(0d, 0d, 0d, 0d, 0);

        var learned = await _playerPillRecipes.GetByPlayerAndRecipeAsync(playerId, recipeId, cancellationToken);
        if (learned is null)
            return new AlchemyCraftRatePlan(0d, 0d, 0d, 0d, 0);

        var optionalSelections = NormalizeOptionalSelections(recipe, selectedOptionalInputs);
        return BuildCraftRatePlan(
            recipe,
            learned.TotalCraftCount,
            learned.CurrentSuccessRateBonus,
            Math.Max(1, requestedCraftCount),
            optionalSelections);
    }

    public async Task<IReadOnlyList<double>> BuildSuccessRollRatesAsync(
        Guid playerId,
        int recipeId,
        int requestedCraftCount,
        IReadOnlyCollection<PracticeOptionalInputEntry>? selectedOptionalInputs = null,
        CancellationToken cancellationToken = default)
    {
        if (!_definitions.TryGetPillRecipe(recipeId, out var recipe))
            return Array.Empty<double>();

        var learned = await _playerPillRecipes.GetByPlayerAndRecipeAsync(playerId, recipeId, cancellationToken);
        if (learned is null)
            return Array.Empty<double>();

        var normalizedRequestedCraftCount = Math.Max(1, requestedCraftCount);
        var masteryBonus = ResolveMasteryBonus(recipe, learned.TotalCraftCount, learned.CurrentSuccessRateBonus);
        var baseRate = ResolveEffectiveSuccessRate(recipe, masteryBonus, null);
        var optionalSelections = NormalizeOptionalSelections(recipe, selectedOptionalInputs);
        var rates = new List<double>(normalizedRequestedCraftCount);
        foreach (var selection in optionalSelections)
        {
            var boostedRate = ResolveEffectiveSuccessRate(recipe, masteryBonus, selection.Input);
            for (var count = 0; count < selection.AppliedCount && rates.Count < normalizedRequestedCraftCount; count++)
                rates.Add(boostedRate);
        }

        while (rates.Count < normalizedRequestedCraftCount)
            rates.Add(baseRate);
        return rates;
    }

    public double ResolveMasteryBonusForCurrentProgress(
        PillRecipeTemplateDefinition recipe,
        int totalCraftCount,
        double fallbackCurrentBonus)
    {
        return ResolveMasteryBonus(recipe, totalCraftCount, fallbackCurrentBonus);
    }

    private static AlchemyValidationResult Failed(
        string failureReason,
        PillRecipeTemplateDefinition? recipe,
        int requestedCraftCount,
        int maxCraftableCount = 0)
    {
        return new AlchemyValidationResult(
            false,
            failureReason,
            recipe,
            requestedCraftCount,
            maxCraftableCount,
            Array.Empty<long>(),
            new Dictionary<long, int>(),
            Array.Empty<AlchemyOptionalInputSelection>(),
            0d,
            0d,
            0d,
            0d,
            0);
    }

    private static Dictionary<int, int> NormalizeOptionalSelections(IReadOnlyCollection<AlchemyOptionalInputSelectionModel>? selectedOptionalInputs)
    {
        var result = new Dictionary<int, int>();
        if (selectedOptionalInputs is null)
            return result;

        foreach (var selection in selectedOptionalInputs)
        {
            if (selection.InputId <= 0 || selection.Quantity <= 0)
                continue;

            result[selection.InputId] = result.TryGetValue(selection.InputId, out var current)
                ? current + selection.Quantity
                : selection.Quantity;
        }

        return result;
    }

    private static IReadOnlyList<AlchemyOptionalInputSelection> NormalizeOptionalSelections(
        PillRecipeTemplateDefinition recipe,
        IReadOnlyCollection<PracticeOptionalInputEntry>? selectedOptionalInputs)
    {
        if (selectedOptionalInputs is null || selectedOptionalInputs.Count == 0)
            return Array.Empty<AlchemyOptionalInputSelection>();

        var optionalInputsById = recipe.Inputs
            .Where(static input => input.IsOptional)
            .ToDictionary(static input => input.Id);
        return selectedOptionalInputs
            .Where(static selection => selection is not null && selection.AppliedCount > 0)
            .Where(selection => optionalInputsById.ContainsKey(selection.InputId))
            .Select(selection => new AlchemyOptionalInputSelection(
                optionalInputsById[selection.InputId],
                Math.Max(0, selection.AppliedCount)))
            .Where(static selection => selection.AppliedCount > 0)
            .ToArray();
    }

    private int CalculateMaxCraftableCount(
        PillRecipeInputDefinition input,
        IReadOnlyCollection<PlayerItemEntity> inventory,
        IReadOnlyDictionary<long, PlayerItemEntity> selectedItems,
        IReadOnlyDictionary<long, PlayerEquipmentEntity> equipmentByItemId,
        IReadOnlyDictionary<long, PlayerSoilEntity> soilByItemId)
    {
        if (!_itemDefinitions.TryGetItem(input.RequiredItemTemplateId, out var requiredItemDefinition))
            return 0;

        var requiredQuantityPerCraft = Math.Max(1, input.RequiredQuantity);
        if (requiredItemDefinition.IsStackable)
        {
            var totalQuantity = inventory
                .Where(item => item.ItemTemplateId == input.RequiredItemTemplateId)
                .Sum(static item => Math.Max(0, item.Quantity));
            return totalQuantity / requiredQuantityPerCraft;
        }

        var eligibleSelectedCount = selectedItems.Values
            .Where(item => item.ItemTemplateId == input.RequiredItemTemplateId)
            .Count(item => IsEligibleSelectedItem(item, equipmentByItemId, soilByItemId));
        return eligibleSelectedCount / requiredQuantityPerCraft;
    }

    private string? TryAllocateInput(
        PillRecipeInputDefinition input,
        int requestedApplicationCount,
        IReadOnlyCollection<PlayerItemEntity> inventory,
        IReadOnlyDictionary<long, PlayerItemEntity> selectedItems,
        IReadOnlyDictionary<long, PlayerEquipmentEntity> equipmentByItemId,
        IReadOnlyDictionary<long, PlayerSoilEntity> soilByItemId,
        ISet<long> allocatedSelectedItemIds,
        IList<long> consumedPlayerItemIds,
        IDictionary<long, int> consumedStackQuantities,
        bool allowPartial,
        out int appliedApplicationCount)
    {
        appliedApplicationCount = 0;
        if (requestedApplicationCount <= 0)
            return null;

        if (!_itemDefinitions.TryGetItem(input.RequiredItemTemplateId, out var requiredItemDefinition))
            return $"Item template {input.RequiredItemTemplateId} cua dan phuong khong ton tai.";

        var quantityPerApplication = Math.Max(1, input.RequiredQuantity);
        if (requiredItemDefinition.IsStackable)
        {
            var maxAvailableApplications = inventory
                .Where(item => item.ItemTemplateId == input.RequiredItemTemplateId)
                .Sum(item =>
                {
                    var alreadyAllocated = consumedStackQuantities.TryGetValue(item.Id, out var allocated) ? allocated : 0;
                    return Math.Max(0, item.Quantity - alreadyAllocated);
                }) / quantityPerApplication;
            if (!allowPartial && maxAvailableApplications < requestedApplicationCount)
                return $"Khong du so luong item template {input.RequiredItemTemplateId} de luyen dan.";

            appliedApplicationCount = allowPartial
                ? Math.Min(requestedApplicationCount, maxAvailableApplications)
                : requestedApplicationCount;
            var quantityToConsume = appliedApplicationCount * quantityPerApplication;
            foreach (var item in inventory.Where(item => item.ItemTemplateId == input.RequiredItemTemplateId).OrderBy(item => item.AcquiredAt).ThenBy(item => item.Id))
            {
                if (quantityToConsume <= 0)
                    break;

                var alreadyAllocated = consumedStackQuantities.TryGetValue(item.Id, out var allocated) ? allocated : 0;
                var available = Math.Max(0, item.Quantity - alreadyAllocated);
                if (available <= 0)
                    continue;

                var consumed = Math.Min(quantityToConsume, available);
                consumedStackQuantities[item.Id] = alreadyAllocated + consumed;
                quantityToConsume -= consumed;
            }

            return quantityToConsume > 0
                ? $"Khong du so luong item template {input.RequiredItemTemplateId} de luyen dan."
                : null;
        }

        var eligibleSelectedItems = selectedItems.Values
            .Where(item => item.ItemTemplateId == input.RequiredItemTemplateId && !allocatedSelectedItemIds.Contains(item.Id))
            .Where(item => IsEligibleSelectedItem(item, equipmentByItemId, soilByItemId))
            .OrderBy(item => item.AcquiredAt)
            .ThenBy(item => item.Id)
            .ToArray();
        var maxSelectableApplications = eligibleSelectedItems.Length / quantityPerApplication;
        if (!allowPartial && maxSelectableApplications < requestedApplicationCount)
            return $"Can chi dinh du player_item_id cho item khong stackable template {input.RequiredItemTemplateId}.";

        appliedApplicationCount = allowPartial
            ? Math.Min(requestedApplicationCount, maxSelectableApplications)
            : requestedApplicationCount;
        var requiredSelectionCount = appliedApplicationCount * quantityPerApplication;
        for (var index = 0; index < requiredSelectionCount; index++)
        {
            var selected = eligibleSelectedItems[index];
            allocatedSelectedItemIds.Add(selected.Id);
            consumedPlayerItemIds.Add(selected.Id);
        }

        return null;
    }

    private static bool IsEligibleSelectedItem(
        PlayerItemEntity item,
        IReadOnlyDictionary<long, PlayerEquipmentEntity> equipmentByItemId,
        IReadOnlyDictionary<long, PlayerSoilEntity> soilByItemId)
    {
        if (equipmentByItemId.TryGetValue(item.Id, out var equipment) && equipment.EquippedSlot.HasValue)
            return false;
        if (soilByItemId.TryGetValue(item.Id, out var soil) && soil.State == (int)PlayerSoilState.Inserted)
            return false;

        return true;
    }

    private static AlchemyCraftRatePlan BuildCraftRatePlan(
        PillRecipeTemplateDefinition recipe,
        int totalCraftCount,
        double fallbackCurrentBonus,
        int requestedCraftCount,
        IReadOnlyCollection<AlchemyOptionalInputSelection> appliedOptionalInputs)
    {
        var masteryBonus = ResolveMasteryBonus(recipe, totalCraftCount, fallbackCurrentBonus);
        var effectiveSuccessRate = ResolveEffectiveSuccessRate(recipe, masteryBonus, null);
        var effectiveMutationRate = ResolveEffectiveMutationRate(recipe, null);
        var boostedSelection = appliedOptionalInputs.FirstOrDefault(static selection => selection.AppliedCount > 0);
        var boostedSuccessRate = boostedSelection is null
            ? effectiveSuccessRate
            : ResolveEffectiveSuccessRate(recipe, masteryBonus, boostedSelection.Input);
        var boostedMutationRate = boostedSelection is null
            ? effectiveMutationRate
            : ResolveEffectiveMutationRate(recipe, boostedSelection.Input);
        var boostedCraftCount = Math.Min(
            Math.Max(1, requestedCraftCount),
            appliedOptionalInputs.Sum(static selection => Math.Max(0, selection.AppliedCount)));
        return new AlchemyCraftRatePlan(
            effectiveSuccessRate,
            effectiveMutationRate,
            boostedSuccessRate,
            boostedMutationRate,
            boostedCraftCount);
    }

    private static double ResolveMasteryBonus(PillRecipeTemplateDefinition recipe, int totalCraftCount, double fallbackCurrentBonus)
    {
        var stage = recipe.MasteryStages
            .Where(x => totalCraftCount >= x.RequiredTotalCraftCount)
            .OrderByDescending(x => x.RequiredTotalCraftCount)
            .FirstOrDefault();
        return stage?.SuccessRateBonus ?? fallbackCurrentBonus;
    }

    private static double ResolveEffectiveSuccessRate(
        PillRecipeTemplateDefinition recipe,
        double masteryBonus,
        PillRecipeInputDefinition? optionalInput)
    {
        var rate = NormalizeRate(recipe.BaseSuccessRate)
                   + NormalizeRate(masteryBonus)
                   + (optionalInput is null ? 0d : NormalizeRate(optionalInput.SuccessRateBonus));
        if (recipe.SuccessRateCap.HasValue)
            rate = Math.Min(rate, NormalizeRate(recipe.SuccessRateCap.Value));

        return Math.Clamp(rate, 0d, 1d);
    }

    private static double ResolveEffectiveMutationRate(
        PillRecipeTemplateDefinition recipe,
        PillRecipeInputDefinition? optionalInput)
    {
        var rate = NormalizeRate(recipe.MutationRate)
                   + (optionalInput is null ? 0d : NormalizeRate(optionalInput.MutationBonusRate));
        if (recipe.MutationRateCap > 0)
            rate = Math.Min(rate, NormalizeRate(recipe.MutationRateCap));

        return Math.Clamp(rate, 0d, 1d);
    }

    private static bool IsExpired(DateTime? expireAtUtc)
    {
        return expireAtUtc.HasValue && expireAtUtc.Value <= DateTime.UtcNow;
    }

    private static double NormalizeRate(double rawRate)
    {
        if (rawRate <= 0d)
            return 0d;

        var normalized = rawRate > 1d
            ? rawRate / 100d
            : rawRate;
        return Math.Clamp(normalized, 0d, 1d);
    }
}
