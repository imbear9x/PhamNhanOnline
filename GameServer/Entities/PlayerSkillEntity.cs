using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("player_skills")]
public sealed class PlayerSkillEntity
{
    [Column("id"), PrimaryKey, Identity] public long Id { get; set; }
    [Column("player_id"), NotNull] public Guid PlayerId { get; set; }
    [Column("skill_id"), NotNull] public int SkillId { get; set; }
    [Column("source_martial_art_id"), NotNull] public int SourceMartialArtId { get; set; }
    [Column("source_martial_art_skill_id"), NotNull] public int SourceMartialArtSkillId { get; set; }
    [Column("unlocked_at")] public DateTime? UnlockedAt { get; set; }
    [Column("is_active"), NotNull] public bool IsActive { get; set; }
    [Column("created_at")] public DateTime? CreatedAt { get; set; }
    [Column("updated_at")] public DateTime? UpdatedAt { get; set; }
}
