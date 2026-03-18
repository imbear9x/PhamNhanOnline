using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("skill_effects")]
public sealed class SkillEffectEntity
{
    [Column("id"), PrimaryKey] public int Id { get; set; }
    [Column("skill_id"), NotNull] public int SkillId { get; set; }
    [Column("effect_type"), NotNull] public int EffectType { get; set; }
    [Column("order_index"), NotNull] public int OrderIndex { get; set; }
    [Column("formula_type")] public int? FormulaType { get; set; }
    [Column("value_type")] public int? ValueType { get; set; }
    [Column("base_value")] public decimal? BaseValue { get; set; }
    [Column("ratio_value")] public decimal? RatioValue { get; set; }
    [Column("extra_value")] public decimal? ExtraValue { get; set; }
    [Column("chance_value")] public decimal? ChanceValue { get; set; }
    [Column("duration_ms")] public int? DurationMs { get; set; }
    [Column("stat_type")] public int? StatType { get; set; }
    [Column("resource_type")] public int? ResourceType { get; set; }
    [Column("target_scope")] public int? TargetScope { get; set; }
    [Column("trigger_timing")] public int? TriggerTiming { get; set; }
    [Column("created_at")] public DateTime? CreatedAt { get; set; }
}
