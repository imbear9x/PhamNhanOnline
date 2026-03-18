using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("game_random_fortune_tags")]
public sealed class GameRandomFortuneTagEntity
{
    [Column("id"), PrimaryKey] public int Id { get; set; }
    [Column("game_random_table_id"), NotNull] public int GameRandomTableId { get; set; }
    [Column("tag"), NotNull] public string Tag { get; set; } = string.Empty;
    [Column("created_at")] public DateTime? CreatedAt { get; set; }
}
