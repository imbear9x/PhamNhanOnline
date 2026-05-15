using GameServer.Config;
using GameServer.DTO;
using GameServer.Entities;
using GameServer.Exceptions;
using GameServer.Repositories;
using GameServer.Runtime;
using GameShared.Messages;
using LinqToDB;

namespace GameServer.Services;

public sealed class BagService
{
    private const int DefaultBagGrade = 1;

    private readonly GameConfigValues _gameConfig;
    private readonly ItemDefinitionCatalog _definitions;
    private readonly BagGradeConfigRepository _bagGrades;
    private readonly PlayerBagRepository _playerBags;
    private readonly PlayerItemRepository _playerItems;
    private readonly PlayerInventoryTransactionService _inventoryTransactions;
    private readonly ItemService _itemService;

    public BagService(
        GameConfigValues gameConfig,
        ItemDefinitionCatalog definitions,
        BagGradeConfigRepository bagGrades,
        PlayerBagRepository playerBags,
        PlayerItemRepository playerItems,
        PlayerInventoryTransactionService inventoryTransactions,
        ItemService itemService)
    {
        _gameConfig = gameConfig;
        _definitions = definitions;
        _bagGrades = bagGrades;
        _playerBags = playerBags;
        _playerItems = playerItems;
        _inventoryTransactions = inventoryTransactions;
        _itemService = itemService;
    }

    public async Task<BagStateDto> GetBagStateAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        await EnsureDefaultBagAsync(playerId, cancellationToken);
        return await GetBagStateCoreAsync(playerId, cancellationToken);
    }

    public async Task EnsureDefaultBagAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        var existing = await _playerBags.GetByPlayerIdAsync(playerId, cancellationToken);
        if (existing is not null)
            return;

        var defaultConfig = await _bagGrades.GetByGradeAsync(DefaultBagGrade, cancellationToken)
                            ?? throw new InvalidOperationException($"Bag grade config {DefaultBagGrade} was not found.");

        try
        {
            await _playerBags.CreateAsync(new PlayerBagEntity
            {
                PlayerId = playerId,
                Grade = defaultConfig.Grade,
                UpdatedAt = DateTime.UtcNow
            }, cancellationToken);
        }
        catch (LinqToDBException)
        {
            existing = await _playerBags.GetByPlayerIdAsync(playerId, cancellationToken);
            if (existing is null)
                throw;
        }
    }

    public async Task<bool> HasCapacityForAsync(Guid playerId, IReadOnlyList<ItemGrantRequest> grants, CancellationToken cancellationToken = default)
    {
        var result = await CheckCapacityForAsync(playerId, grants, cancellationToken);
        return result.CanFit;
    }

    public async Task<InventoryCapacityCheckResult> CheckCapacityForAsync(Guid playerId, IReadOnlyList<ItemGrantRequest> grants, CancellationToken cancellationToken = default)
    {
        await EnsureDefaultBagAsync(playerId, cancellationToken);
        var bag = await RequireBagAsync(playerId, cancellationToken);
        var grade = await RequireBagGradeAsync(bag.Grade, cancellationToken);
        var inventory = await _playerItems.ListByPlayerIdAsync(playerId, cancellationToken);
        inventory = inventory.Where(x => !IsExpired(x.ExpireAt)).ToList();

        var additionalSlots = EstimateAdditionalSlotsNeeded(inventory, grants);
        var usedSlots = inventory.Count;
        var totalSlots = grade.SlotCount;
        return new InventoryCapacityCheckResult(
            usedSlots + additionalSlots <= totalSlots,
            usedSlots,
            totalSlots,
            additionalSlots);
    }

    public async Task<BagUpgradeResult> UpgradeBagAsync(Guid playerId, int targetGrade, CancellationToken cancellationToken = default)
    {
        if (targetGrade <= 0)
            return BagUpgradeResult.Failed(MessageCode.BagUpgradeTargetInvalid);

        return await _inventoryTransactions.ExecuteAsync(
            playerId,
            ct => UpgradeBagCoreAsync(playerId, targetGrade, ct),
            cancellationToken);
    }

    private async Task<BagUpgradeResult> UpgradeBagCoreAsync(Guid playerId, int targetGrade, CancellationToken cancellationToken)
    {
        await EnsureDefaultBagAsync(playerId, cancellationToken);
        var currentBag = await RequireBagAsync(playerId, cancellationToken);
        var currentConfig = await RequireBagGradeAsync(currentBag.Grade, cancellationToken);
        var targetConfig = await _bagGrades.GetByGradeAsync(targetGrade, cancellationToken);
        if (targetConfig is null)
            return BagUpgradeResult.Failed(MessageCode.BagUpgradeTargetInvalid, "Target bag grade config was not found.");
        if (targetGrade <= currentBag.Grade)
            return BagUpgradeResult.Failed(MessageCode.BagUpgradeTargetInvalid, "Target bag grade must be higher than current grade.");

        var currencyCode = _gameConfig.InventoryBagUpgradeCurrencyCode;
        if (string.IsNullOrWhiteSpace(currencyCode) || !_definitions.TryGetItemByCode(currencyCode, out var currencyDefinition))
            throw new InvalidOperationException($"Inventory bag upgrade currency '{currencyCode}' was not found.");

        var ownedCurrency = await _playerItems.ListByTemplateIdAsync(playerId, currencyDefinition.Id, cancellationToken);
        var availableQuantity = ownedCurrency.Where(x => !IsExpired(x.ExpireAt)).Sum(x => x.Quantity);
        if (availableQuantity < targetConfig.UpgradeCostLinhThach)
            return BagUpgradeResult.Failed(MessageCode.BagUpgradeCurrencyInsufficient, "Not enough linh thạch to upgrade bag.");

        await _itemService.RemoveItemAsync(playerId, currencyDefinition.Id, checked((int)targetConfig.UpgradeCostLinhThach), cancellationToken);
        currentBag.Grade = targetConfig.Grade;
        currentBag.UpdatedAt = DateTime.UtcNow;
        await _playerBags.UpdateAsync(currentBag, cancellationToken);

        var state = await BuildStateAsync(currentBag, targetConfig, playerId, cancellationToken);
        var remaining = availableQuantity - checked((int)targetConfig.UpgradeCostLinhThach);
        return BagUpgradeResult.Succeeded(state, remaining);
    }

    private async Task<BagStateDto> GetBagStateCoreAsync(Guid playerId, CancellationToken cancellationToken)
    {
        var bag = await RequireBagAsync(playerId, cancellationToken);
        var grade = await RequireBagGradeAsync(bag.Grade, cancellationToken);
        return await BuildStateAsync(bag, grade, playerId, cancellationToken);
    }

    private async Task<BagStateDto> BuildStateAsync(PlayerBagEntity bag, BagGradeConfigEntity grade, Guid playerId, CancellationToken cancellationToken)
    {
        var usedSlots = await _playerItems.CountInventoryActiveAsync(playerId, cancellationToken);
        return new BagStateDto(bag.Grade, usedSlots, grade.SlotCount, grade.DisplayName);
    }

    private async Task<PlayerBagEntity> RequireBagAsync(Guid playerId, CancellationToken cancellationToken) =>
        await _playerBags.GetByPlayerIdAsync(playerId, cancellationToken)
        ?? throw new InvalidOperationException($"Player bag for {playerId} was not found.");

    private async Task<BagGradeConfigEntity> RequireBagGradeAsync(int grade, CancellationToken cancellationToken) =>
        await _bagGrades.GetByGradeAsync(grade, cancellationToken)
        ?? throw new InvalidOperationException($"Bag grade config {grade} was not found.");

    private int EstimateAdditionalSlotsNeeded(IReadOnlyList<PlayerItemEntity> inventory, IReadOnlyList<ItemGrantRequest> grants)
    {
        if (grants.Count == 0)
            return 0;

        var additionalSlots = 0;
        var stackQuantities = inventory
            .GroupBy(x => new StackKey(x.ItemTemplateId, x.IsBound, x.ExpireAt))
            .ToDictionary(g => g.Key, g => g.Select(x => x.Quantity).ToList());

        foreach (var grant in grants)
        {
            if (grant.Quantity <= 0)
                continue;

            if (!_definitions.TryGetItem(grant.ItemTemplateId, out var definition))
                throw new InvalidOperationException($"Item template {grant.ItemTemplateId} was not found.");

            if (!definition.IsStackable)
            {
                additionalSlots += grant.Quantity;
                continue;
            }

            var remaining = grant.Quantity;
            var key = new StackKey(grant.ItemTemplateId, grant.IsBound, grant.ExpireAtUtc);
            if (!stackQuantities.TryGetValue(key, out var stacks))
            {
                stacks = new List<int>();
                stackQuantities[key] = stacks;
            }

            for (var i = 0; i < stacks.Count && remaining > 0; i++)
            {
                var available = definition.MaxStack - stacks[i];
                if (available <= 0)
                    continue;

                var add = Math.Min(remaining, available);
                stacks[i] += add;
                remaining -= add;
            }

            while (remaining > 0)
            {
                var stackQuantity = Math.Min(remaining, definition.MaxStack);
                stacks.Add(stackQuantity);
                additionalSlots += 1;
                remaining -= stackQuantity;
            }
        }

        return additionalSlots;
    }

    private static bool IsExpired(DateTime? expireAtUtc) =>
        expireAtUtc.HasValue && expireAtUtc.Value <= DateTime.UtcNow;

    private readonly record struct StackKey(int ItemTemplateId, bool IsBound, DateTime? ExpireAtUtc);
}