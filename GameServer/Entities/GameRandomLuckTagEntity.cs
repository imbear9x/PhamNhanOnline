using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("game_random_luck_tags")]
public sealed class GameRandomLuckTagEntity
{
    [Column("id"), PrimaryKey] public int Id { get; set; }
    [Column("game_random_table_id"), NotNull] public int GameRandomTableId { get; set; }
    [Column("tag"), NotNull] public string Tag { get; set; } = string.Empty;
    [Column("created_at")] public DateTime? CreatedAt { get; set; }
}
