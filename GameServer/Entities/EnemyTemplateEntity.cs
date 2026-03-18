using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("enemy_templates")]
public sealed class EnemyTemplateEntity
{
    [Column("id"), PrimaryKey] public int Id { get; set; }
    [Column("code"), NotNull] public string Code { get; set; } = string.Empty;
    [Column("name"), NotNull] public string Name { get; set; } = string.Empty;
    [Column("kind"), NotNull] public int Kind { get; set; }
    [Column("max_hp"), NotNull] public int MaxHp { get; set; }
    [Column("base_attack"), NotNull] public int BaseAttack { get; set; }
    [Column("patrol_radius"), NotNull] public decimal PatrolRadius { get; set; }
    [Column("detection_radius"), NotNull] public decimal DetectionRadius { get; set; }
    [Column("combat_radius"), NotNull] public decimal CombatRadius { get; set; }
    [Column("minimum_skill_interval_ms"), NotNull] public int MinimumSkillIntervalMs { get; set; }
    [Column("cultivation_reward_total"), NotNull] public long CultivationRewardTotal { get; set; }
    [Column("potential_reward_total"), NotNull] public int PotentialRewardTotal { get; set; }
    [Column("description")] public string? Description { get; set; }
    [Column("created_at"), NotNull] public DateTime CreatedAt { get; set; }
}
