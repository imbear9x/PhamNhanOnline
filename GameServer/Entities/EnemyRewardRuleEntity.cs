using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("enemy_reward_rules")]
public sealed class EnemyRewardRuleEntity
{
    [Column("id"), PrimaryKey, Identity] public int Id { get; set; }
    [Column("enemy_template_id"), NotNull] public int EnemyTemplateId { get; set; }
    [Column("delivery_type"), NotNull] public int DeliveryType { get; set; }
    [Column("target_rule"), NotNull] public int TargetRule { get; set; }
    [Column("game_random_table_id"), NotNull] public int GameRandomTableId { get; set; }
    [Column("roll_count"), NotNull] public int RollCount { get; set; }
    [Column("ownership_duration_seconds")] public int? OwnershipDurationSeconds { get; set; }
    [Column("free_for_all_duration_seconds")] public int? FreeForAllDurationSeconds { get; set; }
    [Column("minimum_damage_parts_per_million"), NotNull] public int MinimumDamagePartsPerMillion { get; set; }
    [Column("order_index"), NotNull] public int OrderIndex { get; set; }
    [Column("created_at"), NotNull] public DateTime CreatedAt { get; set; }
}
