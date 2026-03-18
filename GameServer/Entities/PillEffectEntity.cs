using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("pill_effects")]
public sealed class PillEffectEntity
{
    [Column("id"), PrimaryKey, Identity] public int Id { get; set; }
    [Column("pill_template_id"), NotNull] public int PillTemplateId { get; set; }
    [Column("effect_type"), NotNull] public int EffectType { get; set; }
    [Column("order_index"), NotNull] public int OrderIndex { get; set; }
    [Column("value_type")] public int? ValueType { get; set; }
    [Column("base_value")] public decimal? BaseValue { get; set; }
    [Column("ratio_value")] public decimal? RatioValue { get; set; }
    [Column("duration_ms")] public int? DurationMs { get; set; }
    [Column("stat_type")] public int? StatType { get; set; }
    [Column("note")] public string? Note { get; set; }
}

