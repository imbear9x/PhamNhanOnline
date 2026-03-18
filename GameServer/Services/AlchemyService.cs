using GameServer.Entities;
using GameServer.Randomness;
using GameServer.Repositories;
using GameServer.Runtime;

namespace GameServer.Services;

public sealed class AlchemyService
{
    private readonly GameDb _db;
    private readonly AlchemyDefinitionCatalog _definitions;
    private readonly ItemDefinitionCatalog _itemDefinitions;
    private readonly PlayerPillRecipeRepository _playerPillRecipes;
    private readonly PlayerItemRepository _playerItems;
    private readonly PlayerEquipmentRepository _playerEquipments;
    private readonly PlayerSoilRepository _playerSoils;
    private readonly ItemService _itemService;
    private readonly IGameRandomService _randomService;

    public AlchemyService(
        GameDb db,
        AlchemyDefinitionCatalog definitions,
        ItemDefinitionCatalog itemDefinitions,
        PlayerPillRecipeRepository playerPillRecipes,
        PlayerItemRepository playerItems,
        PlayerEquipmentRepository playerEquipments,
        PlayerSoilRepository playerSoils,
        ItemService itemService,
        IGameRandomService randomService)
    {
        _db = db;
        _definitions = definitions;
        _itemDefinitions = itemDefinitions;
        _playerPillRecipes = playerPillRecipes;
        _playerItems = playerItems;
        _playerEquipments = playerEquipments;
        _playerSoils = playerSoils;
        _itemService = itemService;
        _randomService = randomService;
    }

    public async Task<AlchemyValidationResult> ValidateCraftPillAsync(
        Guid playerId,
        int recipeId,
        IReadOnlyCollection<long>? selectedPlayerItemIds,
        IReadOnlyCollection<int>? selectedOptionalInputIds = null,
        CancellationToken cancellationToken = default)
    {
        if (!_definitions.TryGetPillRecipe(recipeId, out var recipe))
            return new AlchemyValidationResult(false, "Dan phuong khong ton tai.", null, Array.Empty<long>(), new Dictionary<long, int>(), Array.Empty<PillRecipeInputDefinition>(), 0d, 0d);

        var learned = await _playerPillRecipes.GetByPlayerAndRecipeAsync(playerId, recipeId, cancellationToken);
        if (learned is null)
            return new AlchemyValidationResult(false, "Player chua hoc dan phuong nay.", recipe, Array.Empty<long>(), new Dictionary<long, int>(), Array.Empty<PillRecipeInputDefinition>(), 0d, 0d);

        if (recipe.Inputs.Any(x => x.RequiredHerbMaturity != HerbMaturityRequirement.None))
        {
            return new AlchemyValidationResult(
                false,
                "Recipe co required_herb_maturity, tinh nang nay de phase sau.",
                recipe,
                Array.Empty<long>(),
                new Dictionary<long, int>(),
                Array.Empty<PillRecipeInputDefinition>(),
                0d,
                0d);
        }

        var inventory = (await _playerItems.ListByPlayerIdAsync(playerId, cancellationToken))
            .Where(x => !IsExpired(x.ExpireAt))
            .ToArray();
        var selectedIds = (selectedPlayerItemIds ?? Array.Empty<long>()).Distinct().ToArray();
        var selectedOptionalIds = new HashSet<int>(selectedOptionalInputIds ?? Array.Empty<int>());
        var selectedItems = inventory.Where(x => selectedIds.Contains(x.Id)).ToDictionary(x => x.Id);
        if (selectedItems.Count != selectedIds.Length)
        {
            return new AlchemyValidationResult(
                false,
                "Co player_item_id khong hop le hoac khong thuoc player.",
                recipe,
                Array.Empty<long>(),
                new Dictionary<long, int>(),
                Array.Empty<PillRecipeInputDefinition>(),
                0d,
                0d);
        }

        var equipmentRows = await _playerEquipments.ListByPlayerItemIdsAsync(inventory.Select(x => x.Id).ToArray(), cancellationToken);
        var equipmentByItemId = equipmentRows.ToDictionary(x => x.PlayerItemId);
        var soilRows = await _playerSoils.ListByPlayerItemIdsAsync(inventory.Select(x => x.Id).ToArray(), cancellationToken);
        var soilByItemId = soilRows.ToDictionary(x => x.PlayerItemId);
        var consumedPlayerItemIds = new List<long>();
        var consumedStackQuantities = new Dictionary<long, int>();
        var appliedOptionalInputs = new List<PillRecipeInputDefinition>();
        var allocatedSelectedItemIds = new HashSet<long>();

        foreach (var input in recipe.Inputs.Where(x => !x.IsOptional))
        {
            var failure = TryAllocateInput(
                input,
                inventory,
                selectedItems,
                equipmentByItemId,
                soilByItemId,
                allocatedSelectedItemIds,
                consumedPlayerItemIds,
                consumedStackQuantities);
            if (failure is not null)
                return new AlchemyValidationResult(false, failure, recipe, Array.Empty<long>(), new Dictionary<long, int>(), Array.Empty<PillRecipeInputDefinition>(), 0d, 0d);
        }

        foreach (var input in recipe.Inputs.Where(x => x.IsOptional && selectedOptionalIds.Contains(x.Id)))
        {
            var failure = TryAllocateInput(
                input,
                inventory,
                selectedItems,
                equipmentByItemId,
                soilByItemId,
                allocatedSelectedItemIds,
                consumedPlayerItemIds,
                consumedStackQuantities);
            if (failure is not null)
                return new AlchemyValidationResult(false, failure, recipe, Array.Empty<long>(), new Dictionary<long, int>(), Array.Empty<PillRecipeInputDefinition>(), 0d, 0d);

            appliedOptionalInputs.Add(input);
        }

        var masteryBonus = ResolveMasteryBonus(recipe, learned.TotalCraftCount, learned.CurrentSuccessRateBonus);
        var effectiveSuccessRate = ResolveEffectiveSuccessRate(recipe, masteryBonus, appliedOptionalInputs);
        var effectiveMutationRate = ResolveEffectiveMutationRate(recipe, appliedOptionalInputs);
        return new AlchemyValidationResult(
            true,
            null,
            recipe,
            consumedPlayerItemIds.ToArray(),
            new Dictionary<long, int>(consumedStackQuantities),
            appliedOptionalInputs.ToArray(),
            effectiveSuccessRate,
            effectiveMutationRate);
    }

    public async Task<AlchemyExecutionResult> ExecuteCraftPillAsync(
        Guid playerId,
        int recipeId,
        IReadOnlyCollection<long>? selectedPlayerItemIds,
        IReadOnlyCollection<int>? selectedOptionalInputIds = null,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateCraftPillAsync(playerId, recipeId, selectedPlayerItemIds, selectedOptionalInputIds, cancellationToken);
        if (!validation.Success)
        {
            return new AlchemyExecutionResult(
                false,
                validation.FailureReason,
                validation.Recipe,
                Array.Empty<InventoryItemView>(),
                validation.ConsumedPlayerItemIds,
                validation.ConsumedStackQuantities,
                validation.EffectiveSuccessRate,
                validation.EffectiveMutationRate);
        }

        var recipe = validation.Recipe ?? throw new InvalidOperationException("Validated alchemy recipe is missing.");
        await using var tx = await _db.BeginTransactionAsync(cancellationToken);
        foreach (var playerItemId in validation.ConsumedPlayerItemIds)
            await _itemService.RemovePlayerItemAsync(playerId, playerItemId, cancellationToken);

        foreach (var stackReduction in validation.ConsumedStackQuantities)
        {
            var playerItem = await _playerItems.GetByIdAsync(stackReduction.Key, cancellationToken)
                             ?? throw new InvalidOperationException($"Player item {stackReduction.Key} was not found during alchemy execution.");
            if (playerItem.PlayerId != playerId)
                throw new InvalidOperationException($"Player item {stackReduction.Key} does not belong to player {playerId}.");

            playerItem.Quantity -= stackReduction.Value;
            playerItem.UpdatedAt = DateTime.UtcNow;
            if (playerItem.Quantity <= 0)
            {
                await _itemService.RemovePlayerItemAsync(playerId, playerItem.Id, cancellationToken);
            }
            else
            {
                await _playerItems.UpdateAsync(playerItem, cancellationToken);
            }
        }

        var successCheck = _randomService.CheckChance(ToPartsPerMillion(validation.EffectiveSuccessRate));
        if (!successCheck.Success)
        {
            await tx.CommitAsync(cancellationToken);
            return new AlchemyExecutionResult(
                false,
                "Luyen dan that bai.",
                recipe,
                Array.Empty<InventoryItemView>(),
                validation.ConsumedPlayerItemIds,
                validation.ConsumedStackQuantities,
                validation.EffectiveSuccessRate,
                validation.EffectiveMutationRate);
        }

        var createdItems = await _itemService.AddItemAsync(
            playerId,
            recipe.ResultPillItemTemplateId,
            1,
            false,
            null,
            cancellationToken);

        var learned = await _playerPillRecipes.GetByPlayerAndRecipeAsync(playerId, recipe.Id, cancellationToken)
                      ?? throw new InvalidOperationException($"Player {playerId} lost learned recipe {recipe.Id} during execution.");
        learned.TotalCraftCount += 1;
        learned.CurrentSuccessRateBonus = ResolveMasteryBonus(recipe, learned.TotalCraftCount, learned.CurrentSuccessRateBonus);
        learned.UpdatedAt = DateTime.UtcNow;
        await _playerPillRecipes.UpdateAsync(learned, cancellationToken);

        await tx.CommitAsync(cancellationToken);

        var createdViews = (await _itemService.GetInventoryAsync(playerId, cancellationToken))
            .Where(x => createdItems.Any(created => created.Id == x.PlayerItemId))
            .ToArray();

        return new AlchemyExecutionResult(
            true,
            null,
            recipe,
            createdViews,
            validation.ConsumedPlayerItemIds,
            validation.ConsumedStackQuantities,
            validation.EffectiveSuccessRate,
            validation.EffectiveMutationRate);
    }

    public async Task<double> CalculateFinalSuccessRateAsync(
        Guid playerId,
        int recipeId,
        IReadOnlyCollection<int>? selectedOptionalInputIds = null,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateCraftPillAsync(playerId, recipeId, Array.Empty<long>(), selectedOptionalInputIds, cancellationToken);
        return validation.EffectiveSuccessRate;
    }

    public async Task UpdateRecipeMasteryAsync(Guid playerId, int recipeId, CancellationToken cancellationToken = default)
    {
        var learned = await _playerPillRecipes.GetByPlayerAndRecipeAsync(playerId, recipeId, cancellationToken)
                      ?? throw new InvalidOperationException($"Player {playerId} has not learned pill recipe {recipeId}.");
        if (!_definitions.TryGetPillRecipe(recipeId, out var recipe))
            throw new InvalidOperationException($"Pill recipe {recipeId} was not found.");

        learned.CurrentSuccessRateBonus = ResolveMasteryBonus(recipe, learned.TotalCraftCount, learned.CurrentSuccessRateBonus);
        learned.UpdatedAt = DateTime.UtcNow;
        await _playerPillRecipes.UpdateAsync(learned, cancellationToken);
    }

    private string? TryAllocateInput(
        PillRecipeInputDefinition input,
        IReadOnlyCollection<PlayerItemEntity> inventory,
        IReadOnlyDictionary<long, PlayerItemEntity> selectedItems,
        IReadOnlyDictionary<long, PlayerEquipmentEntity> equipmentByItemId,
        IReadOnlyDictionary<long, PlayerSoilEntity> soilByItemId,
        ISet<long> allocatedSelectedItemIds,
        IList<long> consumedPlayerItemIds,
        IDictionary<long, int> consumedStackQuantities)
    {
        if (!_itemDefinitions.TryGetItem(input.RequiredItemTemplateId, out var requiredItemDefinition))
            return $"Item template {input.RequiredItemTemplateId} cua dan phuong khong ton tai.";

        if (requiredItemDefinition.IsStackable)
        {
            var remaining = input.RequiredQuantity;
            foreach (var item in inventory.Where(x => x.ItemTemplateId == input.RequiredItemTemplateId).OrderBy(x => x.AcquiredAt).ThenBy(x => x.Id))
            {
                var alreadyAllocated = consumedStackQuantities.TryGetValue(item.Id, out var allocated) ? allocated : 0;
                var available = item.Quantity - alreadyAllocated;
                if (available <= 0)
                    continue;

                var consumed = Math.Min(remaining, available);
                consumedStackQuantities[item.Id] = alreadyAllocated + consumed;
                remaining -= consumed;
                if (remaining <= 0)
                    break;
            }

            return remaining > 0
                ? $"Khong du so luong item template {input.RequiredItemTemplateId} de luyen dan."
                : null;
        }

        var selectedMatches = selectedItems.Values
            .Where(x => x.ItemTemplateId == input.RequiredItemTemplateId && !allocatedSelectedItemIds.Contains(x.Id))
            .OrderBy(x => x.AcquiredAt)
            .ThenBy(x => x.Id)
            .Take(input.RequiredQuantity)
            .ToArray();
        if (selectedMatches.Length < input.RequiredQuantity)
            return $"Can chi dinh du player_item_id cho item khong stackable template {input.RequiredItemTemplateId}.";

        foreach (var selected in selectedMatches)
        {
            if (equipmentByItemId.TryGetValue(selected.Id, out var equipment) && equipment.EquippedSlot.HasValue)
                return $"Player item {selected.Id} dang duoc equip, khong the dung de luyen dan.";

            if (soilByItemId.TryGetValue(selected.Id, out var soil) && soil.State == (int)PlayerSoilState.Inserted)
                return $"Player item {selected.Id} dang la linh tho duoc cam trong plot, khong the dung de luyen dan.";

            allocatedSelectedItemIds.Add(selected.Id);
            consumedPlayerItemIds.Add(selected.Id);
        }

        return null;
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
        IReadOnlyCollection<PillRecipeInputDefinition> appliedOptionalInputs)
    {
        var rate = NormalizeRate(recipe.BaseSuccessRate)
                   + appliedOptionalInputs.Sum(x => NormalizeRate(x.SuccessRateBonus))
                   + NormalizeRate(masteryBonus);
        if (recipe.SuccessRateCap.HasValue)
            rate = Math.Min(rate, NormalizeRate(recipe.SuccessRateCap.Value));

        return Math.Clamp(rate, 0d, 1d);
    }

    private static double ResolveEffectiveMutationRate(
        PillRecipeTemplateDefinition recipe,
        IReadOnlyCollection<PillRecipeInputDefinition> appliedOptionalInputs)
    {
        var rate = NormalizeRate(recipe.MutationRate)
                   + appliedOptionalInputs.Sum(x => NormalizeRate(x.MutationBonusRate));
        if (recipe.MutationRateCap > 0)
            rate = Math.Min(rate, NormalizeRate(recipe.MutationRateCap));

        return Math.Clamp(rate, 0d, 1d);
    }

    private static bool IsExpired(DateTime? expireAtUtc)
    {
        return expireAtUtc.HasValue && expireAtUtc.Value <= DateTime.UtcNow;
    }

    private static int ToPartsPerMillion(double rawRate)
    {
        var normalized = NormalizeRate(rawRate);
        return (int)Math.Round(normalized * 1_000_000d, MidpointRounding.AwayFromZero);
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
