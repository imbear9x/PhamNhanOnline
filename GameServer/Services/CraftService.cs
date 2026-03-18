using GameServer.Entities;
using GameServer.Randomness;
using GameServer.Repositories;
using GameServer.Runtime;

namespace GameServer.Services;

public sealed class CraftService
{
    private readonly GameDb _db;
    private readonly ItemDefinitionCatalog _definitions;
    private readonly PlayerItemRepository _playerItems;
    private readonly PlayerEquipmentRepository _playerEquipments;
    private readonly PlayerEquipmentStatBonusRepository _playerEquipmentBonuses;
    private readonly ItemService _itemService;
    private readonly IGameRandomService _randomService;

    public CraftService(
        GameDb db,
        ItemDefinitionCatalog definitions,
        PlayerItemRepository playerItems,
        PlayerEquipmentRepository playerEquipments,
        PlayerEquipmentStatBonusRepository playerEquipmentBonuses,
        ItemService itemService,
        IGameRandomService randomService)
    {
        _db = db;
        _definitions = definitions;
        _playerItems = playerItems;
        _playerEquipments = playerEquipments;
        _playerEquipmentBonuses = playerEquipmentBonuses;
        _itemService = itemService;
        _randomService = randomService;
    }

    public async Task<CraftValidationResult> ValidateCraftAsync(
        Guid playerId,
        int recipeId,
        IReadOnlyCollection<long>? selectedPlayerItemIds,
        IReadOnlyCollection<int>? selectedOptionalRequirementIds = null,
        CancellationToken cancellationToken = default)
    {
        if (!_definitions.TryGetCraftRecipe(recipeId, out var recipe))
            return new CraftValidationResult(false, "Cong thuc che tao khong ton tai.", null, Array.Empty<long>(), new Dictionary<long, int>(), Array.Empty<CraftRecipeRequirementDefinition>(), 0d);

        if (recipe.CostCurrencyType.HasValue && recipe.CostCurrencyValue > 0)
        {
            return new CraftValidationResult(
                false,
                "Currency cost cho crafting chua duoc noi vao he thong hien tai.",
                recipe,
                Array.Empty<long>(),
                new Dictionary<long, int>(),
                Array.Empty<CraftRecipeRequirementDefinition>(),
                0d);
        }

        var inventory = (await _playerItems.ListByPlayerIdAsync(playerId, cancellationToken))
            .Where(x => !IsExpired(x.ExpireAt))
            .ToArray();
        var selectedIds = (selectedPlayerItemIds ?? Array.Empty<long>()).Distinct().ToArray();
        var selectedOptionalIds = new HashSet<int>(selectedOptionalRequirementIds ?? Array.Empty<int>());
        var selectedItems = inventory.Where(x => selectedIds.Contains(x.Id)).ToDictionary(x => x.Id);
        if (selectedItems.Count != selectedIds.Length)
        {
            return new CraftValidationResult(
                false,
                "Co player_item_id khong hop le hoac khong thuoc player.",
                recipe,
                Array.Empty<long>(),
                new Dictionary<long, int>(),
                Array.Empty<CraftRecipeRequirementDefinition>(),
                0d);
        }

        var equipmentRows = await _playerEquipments.ListByPlayerItemIdsAsync(inventory.Select(x => x.Id).ToArray(), cancellationToken);
        var equipmentByItemId = equipmentRows.ToDictionary(x => x.PlayerItemId);
        var consumedPlayerItemIds = new List<long>();
        var consumedStackQuantities = new Dictionary<long, int>();
        var appliedOptionalRequirements = new List<CraftRecipeRequirementDefinition>();
        var allocatedSelectedItemIds = new HashSet<long>();

        foreach (var requirement in recipe.Requirements.Where(x => !x.IsOptional))
        {
            var failure = TryAllocateRequirement(
                requirement,
                inventory,
                selectedItems,
                equipmentByItemId,
                allocatedSelectedItemIds,
                consumedPlayerItemIds,
                consumedStackQuantities);
            if (failure is not null)
                return new CraftValidationResult(false, failure, recipe, Array.Empty<long>(), new Dictionary<long, int>(), Array.Empty<CraftRecipeRequirementDefinition>(), 0d);
        }

        foreach (var requirement in recipe.Requirements.Where(x => x.IsOptional && selectedOptionalIds.Contains(x.Id)))
        {
            var failure = TryAllocateRequirement(
                requirement,
                inventory,
                selectedItems,
                equipmentByItemId,
                allocatedSelectedItemIds,
                consumedPlayerItemIds,
                consumedStackQuantities);
            if (failure is not null)
                return new CraftValidationResult(false, failure, recipe, Array.Empty<long>(), new Dictionary<long, int>(), Array.Empty<CraftRecipeRequirementDefinition>(), 0d);

            appliedOptionalRequirements.Add(requirement);
        }

        var effectiveMutationRate = ResolveEffectiveMutationRate(recipe, appliedOptionalRequirements);
        return new CraftValidationResult(
            true,
            null,
            recipe,
            consumedPlayerItemIds.ToArray(),
            new Dictionary<long, int>(consumedStackQuantities),
            appliedOptionalRequirements.ToArray(),
            effectiveMutationRate);
    }

    public async Task<CraftExecutionResult> ExecuteCraftAsync(
        Guid playerId,
        int recipeId,
        IReadOnlyCollection<long>? selectedPlayerItemIds,
        IReadOnlyCollection<int>? selectedOptionalRequirementIds = null,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateCraftAsync(playerId, recipeId, selectedPlayerItemIds, selectedOptionalRequirementIds, cancellationToken);
        if (!validation.Success)
        {
            return new CraftExecutionResult(
                false,
                validation.FailureReason,
                validation.Recipe,
                false,
                Array.Empty<InventoryItemView>(),
                validation.ConsumedPlayerItemIds,
                validation.ConsumedStackQuantities,
                validation.EffectiveMutationRate);
        }

        var recipe = validation.Recipe ?? throw new InvalidOperationException("Validated craft recipe is missing.");
        await using var tx = await _db.BeginTransactionAsync(cancellationToken);
        foreach (var playerItemId in validation.ConsumedPlayerItemIds)
        {
            await _itemService.RemovePlayerItemAsync(playerId, playerItemId, cancellationToken);
        }

        foreach (var stackReduction in validation.ConsumedStackQuantities)
        {
            var playerItem = await _playerItems.GetByIdAsync(stackReduction.Key, cancellationToken)
                             ?? throw new InvalidOperationException($"Player item {stackReduction.Key} was not found during craft execution.");
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

        var successCheck = _randomService.CheckChance(ToPartsPerMillion(recipe.SuccessRate));
        if (!successCheck.Success)
        {
            await tx.CommitAsync(cancellationToken);
            return new CraftExecutionResult(
                false,
                "Che tao that bai.",
                recipe,
                false,
                Array.Empty<InventoryItemView>(),
                validation.ConsumedPlayerItemIds,
                validation.ConsumedStackQuantities,
                validation.EffectiveMutationRate);
        }

        var createdItems = await _itemService.AddItemAsync(
            playerId,
            recipe.ResultItemTemplateId,
            recipe.ResultQuantity,
            false,
            null,
            cancellationToken);

        var mutationTriggered = false;
        if (recipe.MutationBonuses.Count > 0 &&
            validation.EffectiveMutationRate > 0 &&
            _definitions.TryGetItem(recipe.ResultItemTemplateId, out var resultDefinition) &&
            resultDefinition.Equipment is not null)
        {
            var mutationCheck = _randomService.CheckChance(ToPartsPerMillion(validation.EffectiveMutationRate));
            mutationTriggered = mutationCheck.Success;
            if (mutationTriggered)
            {
                foreach (var created in createdItems)
                {
                    foreach (var bonus in recipe.MutationBonuses)
                    {
                        await _playerEquipmentBonuses.CreateAsync(new PlayerEquipmentStatBonusEntity
                        {
                            PlayerItemId = created.Id,
                            StatType = (int)bonus.StatType,
                            Value = bonus.Value,
                            ValueType = (int)bonus.ValueType,
                            SourceType = (int)EquipmentBonusSourceType.MutationBonus,
                            CreatedAt = DateTime.UtcNow
                        }, cancellationToken);
                    }
                }
            }
        }

        await tx.CommitAsync(cancellationToken);

        var createdViews = (await _itemService.GetInventoryAsync(playerId, cancellationToken))
            .Where(x => createdItems.Any(created => created.Id == x.PlayerItemId))
            .ToArray();

        return new CraftExecutionResult(
            true,
            null,
            recipe,
            mutationTriggered,
            createdViews,
            validation.ConsumedPlayerItemIds,
            validation.ConsumedStackQuantities,
            validation.EffectiveMutationRate);
    }

    private string? TryAllocateRequirement(
        CraftRecipeRequirementDefinition requirement,
        IReadOnlyCollection<PlayerItemEntity> inventory,
        IReadOnlyDictionary<long, PlayerItemEntity> selectedItems,
        IReadOnlyDictionary<long, PlayerEquipmentEntity> equipmentByItemId,
        ISet<long> allocatedSelectedItemIds,
        IList<long> consumedPlayerItemIds,
        IDictionary<long, int> consumedStackQuantities)
    {
        if (!_definitions.TryGetItem(requirement.RequiredItemTemplateId, out var requiredItemDefinition))
            return $"Item template {requirement.RequiredItemTemplateId} cua recipe khong ton tai.";

        if (requiredItemDefinition.IsStackable)
        {
            var remaining = requirement.RequiredQuantity;
            foreach (var item in inventory.Where(x => x.ItemTemplateId == requirement.RequiredItemTemplateId).OrderBy(x => x.AcquiredAt).ThenBy(x => x.Id))
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
                ? $"Khong du so luong item template {requirement.RequiredItemTemplateId} de craft."
                : null;
        }

        var selectedMatches = selectedItems.Values
            .Where(x => x.ItemTemplateId == requirement.RequiredItemTemplateId && !allocatedSelectedItemIds.Contains(x.Id))
            .OrderBy(x => x.AcquiredAt)
            .ThenBy(x => x.Id)
            .Take(requirement.RequiredQuantity)
            .ToArray();

        if (selectedMatches.Length < requirement.RequiredQuantity)
            return $"Can chi dinh du player_item_id cho item khong stackable template {requirement.RequiredItemTemplateId}.";

        foreach (var selected in selectedMatches)
        {
            if (equipmentByItemId.TryGetValue(selected.Id, out var equipment) && equipment.EquippedSlot.HasValue)
                return $"Player item {selected.Id} dang duoc equip, khong the dung de craft.";

            allocatedSelectedItemIds.Add(selected.Id);
            consumedPlayerItemIds.Add(selected.Id);
        }

        return null;
    }

    private static bool IsExpired(DateTime? expireAtUtc)
    {
        return expireAtUtc.HasValue && expireAtUtc.Value <= DateTime.UtcNow;
    }

    private static double ResolveEffectiveMutationRate(
        CraftRecipeDefinition recipe,
        IReadOnlyCollection<CraftRecipeRequirementDefinition> appliedOptionalRequirements)
    {
        var baseRate = NormalizeRate(recipe.MutationRate);
        var cap = NormalizeRate(recipe.MutationRateCap);
        var bonus = appliedOptionalRequirements.Sum(x => NormalizeRate(x.MutationBonusRate));
        var effective = baseRate + bonus;
        if (cap > 0d)
            effective = Math.Min(effective, cap);

        return Math.Clamp(effective, 0d, 1d);
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
