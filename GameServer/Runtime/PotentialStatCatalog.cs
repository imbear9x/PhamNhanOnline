using GameServer.Entities;
using GameServer.Repositories;
using GameServer.DTO;
using GameShared.Models;
using Microsoft.Extensions.DependencyInjection;

namespace GameServer.Runtime;

public sealed class PotentialStatCatalog
{
    private readonly IReadOnlyDictionary<PotentialAllocationTarget, IReadOnlyList<PotentialStatUpgradeTier>> _tiersByTarget;

    public PotentialStatCatalog(IServiceScopeFactory scopeFactory)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<PotentialStatUpgradeTierRepository>();
        var tiers = repository.GetAllAsync().GetAwaiter().GetResult();
        _tiersByTarget = tiers
            .Where(static x => Enum.IsDefined(typeof(PotentialAllocationTarget), x.TargetStat))
            .GroupBy(x => (PotentialAllocationTarget)x.TargetStat)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<PotentialStatUpgradeTier>)x
                    .Where(static tier => tier.IsEnabled)
                    .OrderBy(tier => tier.MaxUpgradeCount)
                    .Select(tier => new PotentialStatUpgradeTier(
                        x.Key,
                        tier.TierIndex,
                        tier.MaxUpgradeCount,
                        tier.PotentialCostPerUpgrade,
                        tier.StatGainPerUpgrade))
                    .ToArray());
    }

    public bool Supports(PotentialAllocationTarget target)
    {
        return target != PotentialAllocationTarget.None &&
               _tiersByTarget.TryGetValue(target, out var tiers) &&
               tiers.Count > 0;
    }

    public CharacterBaseStatsDto AttachPreviews(CharacterBaseStatsDto baseStats)
    {
        return baseStats with
        {
            PotentialUpgradePreviews = BuildPreviews(baseStats)
        };
    }

    public IReadOnlyList<PotentialUpgradePreviewDto> BuildPreviews(CharacterBaseStatsDto baseStats)
    {
        var targets = new[]
        {
            PotentialAllocationTarget.BaseHp,
            PotentialAllocationTarget.BaseMp,
            PotentialAllocationTarget.BaseAttack,
            PotentialAllocationTarget.BaseSpeed,
            PotentialAllocationTarget.BaseLuck,
            PotentialAllocationTarget.BaseSense
        };

        var previews = new List<PotentialUpgradePreviewDto>(targets.Length);
        for (var i = 0; i < targets.Length; i++)
        {
            previews.Add(BuildPreview(baseStats, targets[i]));
        }

        return previews;
    }

    public PotentialUpgradePreviewDto BuildPreview(CharacterBaseStatsDto baseStats, PotentialAllocationTarget target)
    {
        var nextUpgradeCount = GetUpgradeCount(baseStats, target) + 1;
        var tier = TryGetTier(target, nextUpgradeCount);
        if (!tier.HasValue)
        {
            return new PotentialUpgradePreviewDto(
                target,
                nextUpgradeCount,
                0,
                0,
                0m,
                false,
                false);
        }

        return new PotentialUpgradePreviewDto(
            target,
            nextUpgradeCount,
            tier.Value.TierIndex,
            tier.Value.PotentialCostPerUpgrade,
            tier.Value.StatGainPerUpgrade,
            true,
            (baseStats.UnallocatedPotential ?? 0) >= tier.Value.PotentialCostPerUpgrade);
    }

    public PotentialStatUpgradeTier? TryGetTier(PotentialAllocationTarget target, int nextUpgradeCount)
    {
        if (!_tiersByTarget.TryGetValue(target, out var tiers) || tiers.Count == 0)
            return null;

        for (var i = 0; i < tiers.Count; i++)
        {
            if (nextUpgradeCount <= tiers[i].MaxUpgradeCount)
                return tiers[i];
        }

        return null;
    }

    public FlatStatBonusBundle ResolveAllocatedBonuses(CharacterBaseStatsDto baseStats)
    {
        var total = FlatStatBonusBundle.Empty;
        total = total with { MaxHp = ResolveAllocatedIntBonus(baseStats, PotentialAllocationTarget.BaseHp) };
        total = total with { MaxMp = ResolveAllocatedIntBonus(baseStats, PotentialAllocationTarget.BaseMp) };
        total = total with { Attack = ResolveAllocatedIntBonus(baseStats, PotentialAllocationTarget.BaseAttack) };
        total = total with { Speed = ResolveAllocatedIntBonus(baseStats, PotentialAllocationTarget.BaseSpeed) };
        total = total with { Sense = ResolveAllocatedIntBonus(baseStats, PotentialAllocationTarget.BaseSense) };
        total = total with { Luck = ResolveAllocatedLuckBonus(baseStats) };
        return total;
    }

    public readonly record struct PotentialStatUpgradeTier(
        PotentialAllocationTarget Target,
        int TierIndex,
        int MaxUpgradeCount,
        int PotentialCostPerUpgrade,
        decimal StatGainPerUpgrade);

    private static int GetUpgradeCount(CharacterBaseStatsDto baseStats, PotentialAllocationTarget target)
    {
        return target switch
        {
            PotentialAllocationTarget.BaseHp => baseStats.HpUpgradeCount ?? 0,
            PotentialAllocationTarget.BaseMp => baseStats.MpUpgradeCount ?? 0,
            PotentialAllocationTarget.BaseAttack => baseStats.AttackUpgradeCount ?? 0,
            PotentialAllocationTarget.BaseSpeed => baseStats.SpeedUpgradeCount ?? 0,
            PotentialAllocationTarget.BaseSense => baseStats.SenseUpgradeCount ?? 0,
            PotentialAllocationTarget.BaseLuck => baseStats.LuckUpgradeCount ?? 0,
            _ => 0
        };
    }

    private int ResolveAllocatedIntBonus(CharacterBaseStatsDto baseStats, PotentialAllocationTarget target)
    {
        var totalGain = ResolveAllocatedGain(baseStats, target);
        return decimal.ToInt32(decimal.Truncate(totalGain));
    }

    private double ResolveAllocatedLuckBonus(CharacterBaseStatsDto baseStats)
    {
        return (double)ResolveAllocatedGain(baseStats, PotentialAllocationTarget.BaseLuck);
    }

    private decimal ResolveAllocatedGain(CharacterBaseStatsDto baseStats, PotentialAllocationTarget target)
    {
        if (!_tiersByTarget.TryGetValue(target, out var tiers) || tiers.Count == 0)
            return 0m;

        var appliedUpgrades = GetUpgradeCount(baseStats, target);
        if (appliedUpgrades <= 0)
            return 0m;

        decimal totalGain = 0m;
        var remainingUpgrades = appliedUpgrades;
        var previousTierMax = 0;

        for (var i = 0; i < tiers.Count && remainingUpgrades > 0; i++)
        {
            var tier = tiers[i];
            var tierCapacity = Math.Max(0, tier.MaxUpgradeCount - previousTierMax);
            if (tierCapacity <= 0)
            {
                previousTierMax = tier.MaxUpgradeCount;
                continue;
            }

            var appliedInTier = Math.Min(remainingUpgrades, tierCapacity);
            totalGain += tier.StatGainPerUpgrade * appliedInTier;
            remainingUpgrades -= appliedInTier;
            previousTierMax = tier.MaxUpgradeCount;
        }

        return totalGain;
    }
}
