using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("player_martial_arts")]
public sealed class PlayerMartialArtEntity
{
    [Column("id"), PrimaryKey, Identity] public long Id { get; set; }
    [Column("player_id"), NotNull] public Guid PlayerId { get; set; }
    [Column("martial_art_id"), NotNull] public int MartialArtId { get; set; }
    [Column("current_stage"), NotNull] public int CurrentStage { get; set; }
    [Column("current_exp"), NotNull] public long CurrentExp { get; set; }
    [Column("created_at")] public DateTime? CreatedAt { get; set; }
    [Column("updated_at")] public DateTime? UpdatedAt { get; set; }
}
