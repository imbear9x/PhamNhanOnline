using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("martial_art_skills")]
public sealed class MartialArtSkillEntity
{
    [Column("id"), PrimaryKey] public int Id { get; set; }
    [Column("martial_art_id"), NotNull] public int MartialArtId { get; set; }
    [Column("skill_id"), NotNull] public int SkillId { get; set; }
    [Column("unlock_stage"), NotNull] public int UnlockStage { get; set; }
    [Column("created_at")] public DateTime? CreatedAt { get; set; }
}
