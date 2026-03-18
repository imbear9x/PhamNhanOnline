using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("martial_art_skill_scalings")]
public sealed class MartialArtSkillScalingEntity
{
    [Column("id"), PrimaryKey] public int Id { get; set; }
    [Column("martial_art_skill_id"), NotNull] public int MartialArtSkillId { get; set; }
    [Column("skill_effect_id")] public int? SkillEffectId { get; set; }
    [Column("scaling_target"), NotNull] public int ScalingTarget { get; set; }
    [Column("base_value"), NotNull] public decimal BaseValue { get; set; }
    [Column("per_stage_value"), NotNull] public decimal PerStageValue { get; set; }
    [Column("value_type")] public int? ValueType { get; set; }
    [Column("created_at")] public DateTime? CreatedAt { get; set; }
}
