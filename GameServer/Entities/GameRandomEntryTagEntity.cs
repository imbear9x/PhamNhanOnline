using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("game_random_entry_tags")]
public sealed class GameRandomEntryTagEntity
{
    [Column("id"), PrimaryKey] public int Id { get; set; }
    [Column("game_random_entry_id"), NotNull] public int GameRandomEntryId { get; set; }
    [Column("tag"), NotNull] public string Tag { get; set; } = string.Empty;
    [Column("created_at")] public DateTime? CreatedAt { get; set; }
}
