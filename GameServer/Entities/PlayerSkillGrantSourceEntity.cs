using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("player_skill_grant_sources")]
public sealed class PlayerSkillGrantSourceEntity
{
    [Column("id"), PrimaryKey, Identity] public long Id { get; set; }
    [Column("player_id"), NotNull] public Guid PlayerId { get; set; }
    [Column("player_skill_id"), NotNull] public long PlayerSkillId { get; set; }
    [Column("source_type"), NotNull] public int SourceType { get; set; }
    [Column("granted_skill_id"), NotNull] public int GrantedSkillId { get; set; }
    [Column("source_player_item_id")] public long? SourcePlayerItemId { get; set; }
    [Column("source_equipment_template_id")] public int? SourceEquipmentTemplateId { get; set; }
    [Column("created_at")] public DateTime? CreatedAt { get; set; }
    [Column("updated_at")] public DateTime? UpdatedAt { get; set; }
}
