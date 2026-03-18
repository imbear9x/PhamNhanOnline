using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("game_random_entries")]
public sealed class GameRandomEntryEntity
{
    [Column("id"), PrimaryKey] public int Id { get; set; }
    [Column("game_random_table_id"), NotNull] public int GameRandomTableId { get; set; }
    [Column("entry_id"), NotNull] public string EntryId { get; set; } = string.Empty;
    [Column("chance_parts_per_million"), NotNull] public int ChancePartsPerMillion { get; set; }
    [Column("is_none"), NotNull] public bool IsNone { get; set; }
    [Column("order_index"), NotNull] public int OrderIndex { get; set; }
    [Column("created_at")] public DateTime? CreatedAt { get; set; }
}
