using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("potential_stat_upgrade_tiers")]
public sealed class PotentialStatUpgradeTierEntity
{
    [Column("target_stat"), PrimaryKey(1)] public int TargetStat { get; set; }
    [Column("tier_index"), PrimaryKey(2)] public int TierIndex { get; set; }
    [Column("max_upgrade_count"), NotNull] public int MaxUpgradeCount { get; set; }
    [Column("potential_cost_per_upgrade"), NotNull] public int PotentialCostPerUpgrade { get; set; }
    [Column("stat_gain_per_upgrade"), NotNull] public decimal StatGainPerUpgrade { get; set; }
    [Column("is_enabled"), NotNull] public bool IsEnabled { get; set; }
}
