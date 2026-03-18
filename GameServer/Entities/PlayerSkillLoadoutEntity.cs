using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("player_skill_loadouts")]
public sealed class PlayerSkillLoadoutEntity
{
    [Column("id"), PrimaryKey, Identity] public long Id { get; set; }
    [Column("player_id"), NotNull] public Guid PlayerId { get; set; }
    [Column("slot_index"), NotNull] public int SlotIndex { get; set; }
    [Column("player_skill_id"), NotNull] public long PlayerSkillId { get; set; }
    [Column("created_at")] public DateTime? CreatedAt { get; set; }
    [Column("updated_at")] public DateTime? UpdatedAt { get; set; }
}
