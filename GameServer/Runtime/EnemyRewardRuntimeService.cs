using System.Numerics;
using GameServer.Config;
using GameServer.Entities;
using GameServer.DTO;
using GameServer.Randomness;
using GameServer.Repositories;
using GameServer.Services;
using GameServer.Network.Interface;
using GameServer.World;
using GameShared.Logging;
using GameShared.Messages;
using GameShared.Packets;
using Microsoft.Extensions.DependencyInjection;

namespace GameServer.Runtime;

public sealed class EnemyRewardRuntimeService
{
    private readonly GameConfigValues _gameConfig;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IGameRandomService _gameRandomService;
    private readonly ItemDefinitionCatalog _itemDefinitions;
    private readonly WorldManager _worldManager;
    private readonly CharacterRuntimeService _runtimeService;
    private readonly CharacterRuntimeSaveService _runtimeSaveService;
    private readonly CharacterCultivationService _cultivationService;
    private readonly PotentialStatCatalog _potentialStatCatalog;
    private readonly INetworkSender _network;
    private readonly IReadOnlyDictionary<int, RealmTemplate> _realmsById;

    public EnemyRewardRuntimeService(
        GameConfigValues gameConfig,
        IServiceScopeFactory scopeFactory,
        IGameRandomService gameRandomService,
        ItemDefinitionCatalog itemDefinitions,
        WorldManager worldManager,
        CharacterRuntimeService runtimeService,
        CharacterRuntimeSaveService runtimeSaveService,
        CharacterCultivationService cultivationService,
        PotentialStatCatalog potentialStatCatalog,
        INetworkSender network)
    {
        _gameConfig = gameConfig;
        _scopeFactory = scopeFactory;
        _gameRandomService = gameRandomService;
        _itemDefinitions = itemDefinitions;
        _worldManager = worldManager;
        _runtimeService = runtimeService;
        _runtimeSaveService = runtimeSaveService;
        _cultivationService = cultivationService;
        _potentialStatCatalog = potentialStatCatalog;
        _network = network;

        using var scope = scopeFactory.CreateScope();
        var realmRepository = scope.ServiceProvider.GetRequiredService<RealmTemplateRepository>();
        _realmsById = realmRepository
            .GetAllAsync()
            .GetAwaiter()
            .GetResult()
            .ToDictionary(x => x.Id);
    }

    public void ProcessPendingEvents(
        MapInstance instance,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ProcessPendingEventsAsync(instance, utcNow, cancellationToken).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Enemy reward runtime processing failed for map {instance.MapId}, instance {instance.InstanceId}.");
        }
    }

    private async Task ProcessPendingEventsAsync(
        MapInstance instance,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var deaths = instance.DequeuePendingDeaths();
        if (deaths.Count == 0)
            return;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var itemService = scope.ServiceProvider.GetRequiredService<ItemService>();
        var bagService = scope.ServiceProvider.GetRequiredService<BagService>();
        var inventoryTransactions = scope.ServiceProvider.GetRequiredService<PlayerInventoryTransactionService>();

        foreach (var death in deaths)
        {
            var progressionRewardPlayers = ApplyProgressionRewards(death);
            foreach (var rewardedPlayer in progressionRewardPlayers)
                await _runtimeSaveService.FlushPlayerAsync(rewardedPlayer.PlayerId, cancellationToken);

            foreach (var rewardRule in death.Definition.RewardRules)
            {
                var targets = ResolveRewardTargets(instance, death, rewardRule);
                if (targets.Count == 0)
                    continue;

                foreach (var target in targets)
                {
                    var directGrantItems = new List<RewardItemSeed>();
                    var groundDropItems = new List<RewardItemSeed>();
                    for (var rollIndex = 0; rollIndex < rewardRule.RollCount; rollIndex++)
                    {
                        var luck = target.Player.RuntimeState.CaptureSnapshot().BaseStats.GetEffectiveLuck();
                        GameRandomRollResult rollResult;
                        try
                        {
                            rollResult = _gameRandomService.Roll(
                                rewardRule.RandomTableId,
                                new GameRandomContext(target.Player.CharacterData.CharacterId),
                                new GameRandomOptions(luck));
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, $"Failed to roll random table '{rewardRule.RandomTableId}' for enemy template {death.Definition.Id}.");
                            break;
                        }

                        if (rollResult.SelectedEntry.IsNone)
                            continue;

                        if (!TryResolveRewardItem(rollResult.SelectedEntry.EntryId, out var rewardItem))
                        {
                            Logger.Info($"Unsupported enemy reward entry '{rollResult.SelectedEntry.EntryId}' in table '{rewardRule.RandomTableId}'.");
                            continue;
                        }

                        if (rewardRule.DeliveryType == RewardDeliveryType.DirectGrant)
                        {
                            directGrantItems.Add(rewardItem);
                        }
                        else
                        {
                            groundDropItems.Add(rewardItem);
                        }
                    }

                    if (directGrantItems.Count > 0)
                    {
                        await GrantDirectRewardItemsAsync(
                            inventoryTransactions,
                            bagService,
                            itemService,
                            target.Player,
                            directGrantItems,
                            cancellationToken);
                    }

                    if (rewardRule.DeliveryType != RewardDeliveryType.GroundDrop || groundDropItems.Count == 0)
                        continue;

                    var createdGroundItems = await CreateGroundRewardItemsAsync(itemService, groundDropItems, cancellationToken);

                    var ownerCharacterId = rewardRule.OwnershipDurationSeconds is > 0
                        ? target.Player.CharacterData.CharacterId
                        : (Guid?)null;
                    var defaultOwnershipSeconds = Math.Max(0, _gameConfig.ItemDropEnemyDefaultOwnershipSeconds);
                    var defaultFreeForAllSeconds = Math.Max(0, _gameConfig.ItemDropEnemyDefaultFreeForAllSeconds);
                    var freeAtUtc = utcNow.AddSeconds(rewardRule.OwnershipDurationSeconds ?? defaultOwnershipSeconds);
                    if (ownerCharacterId is null)
                        freeAtUtc = utcNow;

                    var destroyAtUtc = freeAtUtc.AddSeconds(rewardRule.FreeForAllDurationSeconds ?? defaultFreeForAllSeconds);
                    var dropPosition = ResolveGroundRewardSpawnPosition(instance, death.Position, target.Player.Position.X);
                    instance.AddGroundReward(new GroundRewardEntity(
                        instance.AllocateGroundRewardId(),
                        ownerCharacterId,
                        dropPosition,
                        createdGroundItems,
                        utcNow,
                        freeAtUtc,
                        destroyAtUtc));
                }
            }
        }
    }

    private IReadOnlyList<PlayerSession> ApplyProgressionRewards(EnemyDeathRuntimeEvent death)
    {
        var targets = ResolveContributionTargets(death.Targets);
        if (targets.Count == 0)
            return Array.Empty<PlayerSession>();

        var cultivationShares = AllocateLongByDamage(targets, death.Definition.CultivationRewardTotal);
        var potentialShares = AllocateIntByDamage(targets, death.Definition.PotentialRewardTotal);
        var rewardedPlayers = new List<PlayerSession>(targets.Count);

        foreach (var target in targets)
        {
            var cultivationReward = cultivationShares.GetValueOrDefault(target.Player.PlayerId);
            var potentialReward = potentialShares.GetValueOrDefault(target.Player.PlayerId);
            if (cultivationReward <= 0 && potentialReward <= 0)
                continue;

            var snapshot = target.Player.RuntimeState.CaptureSnapshot();
            if (!snapshot.BaseStats.RealmTemplateId.HasValue ||
                !_realmsById.TryGetValue(snapshot.BaseStats.RealmTemplateId.Value, out var realm))
            {
                continue;
            }

            var currentCultivation = snapshot.BaseStats.Cultivation ?? 0L;
            var maxCultivation = realm.MaxCultivation ?? long.MaxValue;
            var remainingToCap = Math.Max(0L, maxCultivation - currentCultivation);
            var grantedCultivation = Math.Min(cultivationReward, remainingToCap);
            var grantedPotential = _cultivationService.IsPotentialRewardLocked(snapshot.BaseStats, realm)
                ? 0
                : Math.Max(0, potentialReward);

            if (grantedCultivation <= 0 && grantedPotential <= 0)
                continue;

            var reachedCap = currentCultivation + grantedCultivation >= maxCultivation;
            var updatedBaseStats = snapshot.BaseStats with
            {
                Cultivation = checked(currentCultivation + grantedCultivation),
                UnallocatedPotential = checked((snapshot.BaseStats.UnallocatedPotential ?? 0) + grantedPotential),
                CultivationProgress = reachedCap ? 0m : snapshot.BaseStats.CultivationProgress,
                PotentialRewardLocked = reachedCap || snapshot.BaseStats.PotentialRewardLocked == true
            };

            var effectiveBaseStats = _potentialStatCatalog.AttachPreviews(updatedBaseStats);
            _runtimeService.ApplyBaseStatsMutation(target.Player, _ => effectiveBaseStats);
            rewardedPlayers.Add(target.Player);
        }

        return rewardedPlayers;
    }

    private List<ResolvedRewardTarget> ResolveRewardTargets(
        MapInstance instance,
        EnemyDeathRuntimeEvent death,
        EnemyRewardRuleDefinition rule)
    {
        var targets = ResolveContributionTargets(death.Targets);
        if (targets.Count == 0)
            return [];

        var totalDamage = targets.Sum(x => x.Snapshot.DamageDealt);
        var minimumDamagePpm = Math.Max(0, rule.MinimumDamagePartsPerMillion);

        bool PassesThreshold(ResolvedRewardTarget candidate)
        {
            if (minimumDamagePpm <= 0)
                return true;

            if (totalDamage <= 0)
                return false;

            return (long)candidate.Snapshot.DamageDealt * 1_000_000L >= (long)totalDamage * minimumDamagePpm;
        }

        return rule.TargetRule switch
        {
            RewardTargetRule.EligibleAll => targets.Where(PassesThreshold).ToList(),
            RewardTargetRule.LastHit => death.LastHitPlayerId.HasValue
                ? targets.Where(x => x.Player.PlayerId == death.LastHitPlayerId.Value && PassesThreshold(x)).Take(1).ToList()
                : [],
            RewardTargetRule.TopDamage => targets
                .Where(PassesThreshold)
                .OrderByDescending(x => x.Snapshot.DamageDealt)
                .ThenBy(x => x.Snapshot.LastHitAtUtc)
                .Take(1)
                .ToList(),
            _ => []
        };
    }

    private List<ResolvedRewardTarget> ResolveContributionTargets(IReadOnlyList<RewardTargetSnapshot> snapshots)
    {
        var targets = new List<ResolvedRewardTarget>(snapshots.Count);
        foreach (var snapshot in snapshots.Where(x => x.DamageDealt > 0))
        {
            if (!_worldManager.TryGetPlayer(snapshot.PlayerId, out var player))
                continue;

            targets.Add(new ResolvedRewardTarget(player, snapshot));
        }

        return targets;
    }

    private bool TryResolveRewardItem(string entryId, out RewardItemSeed rewardItem)
    {
        rewardItem = default!;
        if (string.IsNullOrWhiteSpace(entryId))
            return false;

        if (TryResolvePrefixedItemEntry(entryId, out rewardItem))
            return true;

        if (_itemDefinitions.TryGetItemByCode(entryId, out var directCodeDefinition))
        {
            rewardItem = new RewardItemSeed(directCodeDefinition.Id, 1, false);
            return true;
        }

        if (entryId.Contains('.', StringComparison.Ordinal))
        {
            var trimmedCode = entryId[(entryId.IndexOf('.') + 1)..];
            if (_itemDefinitions.TryGetItemByCode(trimmedCode, out var dottedDefinition))
            {
                rewardItem = new RewardItemSeed(dottedDefinition.Id, 1, false);
                return true;
            }
        }

        return false;
    }

    private bool TryResolvePrefixedItemEntry(string entryId, out RewardItemSeed rewardItem)
    {
        rewardItem = default!;
        var parts = entryId.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return false;

        var quantity = 1;
        if (parts.Length >= 3 && (!int.TryParse(parts[2], out quantity) || quantity <= 0))
            return false;

        if (parts[0].Equals("item", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(parts[1], out var itemTemplateId) &&
                _itemDefinitions.TryGetItem(itemTemplateId, out _))
            {
                rewardItem = new RewardItemSeed(itemTemplateId, quantity, false);
                return true;
            }

            if (_itemDefinitions.TryGetItemByCode(parts[1], out var definitionByCode))
            {
                rewardItem = new RewardItemSeed(definitionByCode.Id, quantity, false);
                return true;
            }
        }

        if ((parts[0].Equals("item_code", StringComparison.OrdinalIgnoreCase) ||
             parts[0].Equals("currency", StringComparison.OrdinalIgnoreCase)) &&
            _itemDefinitions.TryGetItemByCode(parts[1], out var itemByCode))
        {
            rewardItem = new RewardItemSeed(itemByCode.Id, quantity, false);
            return true;
        }

        return false;
    }

    private bool IsHerbRewardItem(RewardItemSeed rewardItem)
    {
        if (!_itemDefinitions.TryGetItem(rewardItem.ItemTemplateId, out var definition))
            return false;

        return definition.ItemType is ItemType.HerbSeed or ItemType.HerbPlant;
    }

    private async Task GrantDirectRewardItemsAsync(
        PlayerInventoryTransactionService inventoryTransactions,
        BagService bagService,
        ItemService itemService,
        PlayerSession player,
        IReadOnlyList<RewardItemSeed> rolledItems,
        CancellationToken cancellationToken)
    {
        var shouldNotifyInventoryFull = false;

        await inventoryTransactions.ExecuteAsync(
            player.CharacterData.CharacterId,
            async ct =>
            {
                var herbDirectGrantItems = rolledItems
                    .Where(IsHerbRewardItem)
                    .ToArray();
                var itemsToGrant = rolledItems.ToList();

                if (herbDirectGrantItems.Length > 0)
                {
                    var herbCapacityCheck = await bagService.CheckCapacityForAsync(
                        player.CharacterData.CharacterId,
                        herbDirectGrantItems
                            .Select(x => new ItemGrantRequest(x.ItemTemplateId, x.Quantity, x.IsBound, null))
                            .ToArray(),
                        ct);
                    if (!herbCapacityCheck.CanFit)
                    {
                        shouldNotifyInventoryFull = true;
                        itemsToGrant.RemoveAll(IsHerbRewardItem);
                    }
                }

                foreach (var group in itemsToGrant
                             .GroupBy(static x => new { x.ItemTemplateId, x.IsBound })
                             .OrderBy(static x => x.Key.ItemTemplateId))
                {
                    await itemService.AddItemAsync(
                        player.CharacterData.CharacterId,
                        group.Key.ItemTemplateId,
                        group.Sum(static x => x.Quantity),
                        group.Key.IsBound,
                        cancellationToken: ct);
                }
            },
            cancellationToken);

        if (shouldNotifyInventoryFull)
        {
            _network.Send(player.ConnectionId, new PickupGroundRewardResultPacket
            {
                Success = false,
                Code = MessageCode.InventoryFull,
                RewardId = null
            });
        }
    }

    private async Task<IReadOnlyList<GroundRewardItem>> CreateGroundRewardItemsAsync(
        ItemService itemService,
        IReadOnlyList<RewardItemSeed> rolledItems,
        CancellationToken cancellationToken)
    {
        var result = new List<GroundRewardItem>();
        foreach (var group in rolledItems
                     .GroupBy(x => new { x.ItemTemplateId, x.IsBound })
                     .OrderBy(x => x.Key.ItemTemplateId))
        {
            if (!_itemDefinitions.TryGetItem(group.Key.ItemTemplateId, out _))
                throw new InvalidOperationException($"Reward item template {group.Key.ItemTemplateId} was not found.");

            var totalQuantity = group.Sum(x => x.Quantity);
            var created = await itemService.CreateGroundItemInstancesAsync(
                group.Key.ItemTemplateId,
                totalQuantity,
                group.Key.IsBound,
                cancellationToken: cancellationToken);

            foreach (var item in created.OrderBy(x => x.Id))
            {
                if (!_itemDefinitions.TryGetItem(item.ItemTemplateId, out var definition))
                    throw new InvalidOperationException($"Reward item template {item.ItemTemplateId} was not found.");

                result.Add(new GroundRewardItem(
                    item.Id,
                    item.ItemTemplateId,
                    definition.Code,
                    definition.Name,
                    definition.ItemType,
                    definition.Rarity,
                    item.Quantity,
                    item.IsBound,
                    definition.Icon,
                    definition.BackgroundIcon));
            }
        }

        return result;
    }

    private Vector2 ResolveGroundRewardSpawnPosition(MapInstance instance, Vector2 originPosition, float? sourcePositionX = null)
    {
        var offset = Math.Max(0f, _gameConfig.ItemDropGroundSpawnOffsetServerUnits);
        if (offset <= 0f)
            return instance.Definition.ClampPosition(originPosition);

        var direction = !sourcePositionX.HasValue || sourcePositionX.Value >= originPosition.X ? 1f : -1f;
        if (sourcePositionX.HasValue && Math.Abs(sourcePositionX.Value - originPosition.X) <= 0.01f)
            direction = 1f;

        return instance.Definition.ClampPosition(new Vector2(
            originPosition.X + (offset * direction),
            originPosition.Y));
    }

    private static Dictionary<Guid, long> AllocateLongByDamage(
        IReadOnlyList<ResolvedRewardTarget> targets,
        long totalReward)
    {
        if (totalReward <= 0 || targets.Count == 0)
            return new Dictionary<Guid, long>();

        var totalDamage = targets.Sum(x => x.Snapshot.DamageDealt);
        if (totalDamage <= 0)
            return new Dictionary<Guid, long>();

        var allocations = targets.ToDictionary(x => x.Player.PlayerId, _ => 0L);
        var remainders = new List<(Guid PlayerId, decimal Remainder)>(targets.Count);
        long assigned = 0;

        foreach (var target in targets)
        {
            var exactShare = (decimal)totalReward * target.Snapshot.DamageDealt / totalDamage;
            var baseShare = decimal.ToInt64(decimal.Floor(exactShare));
            allocations[target.Player.PlayerId] = baseShare;
            assigned += baseShare;
            remainders.Add((target.Player.PlayerId, exactShare - baseShare));
        }

        var remaining = totalReward - assigned;
        foreach (var extra in remainders.OrderByDescending(x => x.Remainder).ThenBy(x => x.PlayerId))
        {
            if (remaining <= 0)
                break;

            allocations[extra.PlayerId]++;
            remaining--;
        }

        return allocations;
    }

    private static Dictionary<Guid, int> AllocateIntByDamage(
        IReadOnlyList<ResolvedRewardTarget> targets,
        int totalReward)
    {
        if (totalReward <= 0 || targets.Count == 0)
            return new Dictionary<Guid, int>();

        var totalDamage = targets.Sum(x => x.Snapshot.DamageDealt);
        if (totalDamage <= 0)
            return new Dictionary<Guid, int>();

        var allocations = targets.ToDictionary(x => x.Player.PlayerId, _ => 0);
        var remainders = new List<(Guid PlayerId, decimal Remainder)>(targets.Count);
        var assigned = 0;

        foreach (var target in targets)
        {
            var exactShare = (decimal)totalReward * target.Snapshot.DamageDealt / totalDamage;
            var baseShare = decimal.ToInt32(decimal.Floor(exactShare));
            allocations[target.Player.PlayerId] = baseShare;
            assigned += baseShare;
            remainders.Add((target.Player.PlayerId, exactShare - baseShare));
        }

        var remaining = totalReward - assigned;
        foreach (var extra in remainders.OrderByDescending(x => x.Remainder).ThenBy(x => x.PlayerId))
        {
            if (remaining <= 0)
                break;

            allocations[extra.PlayerId]++;
            remaining--;
        }

        return allocations;
    }

    private readonly record struct ResolvedRewardTarget(
        PlayerSession Player,
        RewardTargetSnapshot Snapshot);

    private readonly record struct RewardItemSeed(
        int ItemTemplateId,
        int Quantity,
        bool IsBound);
}


