using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("skills")]
public sealed class SkillEntity
{
    [Column("id"), PrimaryKey] public int Id { get; set; }
    [Column("code"), NotNull] public string Code { get; set; } = string.Empty;
    [Column("name"), NotNull] public string Name { get; set; } = string.Empty;
    [Column("skill_group_code"), NotNull] public string SkillGroupCode { get; set; } = string.Empty;
    [Column("skill_level"), NotNull] public int SkillLevel { get; set; }
    [Column("skill_type"), NotNull] public int SkillType { get; set; }
    [Column("skill_category"), NotNull] public int SkillCategory { get; set; }
    [Column("target_type"), NotNull] public int TargetType { get; set; }
    [Column("cast_range"), NotNull] public decimal CastRange { get; set; }
    [Column("cooldown_ms"), NotNull] public int CooldownMs { get; set; }
    [Column("description")] public string? Description { get; set; }
    [Column("created_at")] public DateTime? CreatedAt { get; set; }
}
