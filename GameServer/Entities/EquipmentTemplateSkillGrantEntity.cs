using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("equipment_template_skill_grants")]
public sealed class EquipmentTemplateSkillGrantEntity
{
    [Column("id"), PrimaryKey, Identity] public long Id { get; set; }
    [Column("equipment_template_id"), NotNull] public int EquipmentTemplateId { get; set; }
    [Column("skill_id"), NotNull] public int SkillId { get; set; }
    [Column("required_realm_template_id")] public int? RequiredRealmTemplateId { get; set; }
    [Column("display_order"), NotNull] public int DisplayOrder { get; set; }
    [Column("created_at")] public DateTime? CreatedAt { get; set; }
    [Column("updated_at")] public DateTime? UpdatedAt { get; set; }
}
