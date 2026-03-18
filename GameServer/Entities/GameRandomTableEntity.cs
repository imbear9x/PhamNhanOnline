using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("game_random_tables")]
public sealed class GameRandomTableEntity
{
    [Column("id"), PrimaryKey] public int Id { get; set; }
    [Column("table_id"), NotNull] public string TableId { get; set; } = string.Empty;
    [Column("mode"), NotNull] public int Mode { get; set; }
    [Column("fortune_enabled"), NotNull] public bool FortuneEnabled { get; set; }
    [Column("fortune_bonus_parts_per_million_per_fortune_point"), NotNull] public int FortuneBonusPartsPerMillionPerFortunePoint { get; set; }
    [Column("fortune_max_bonus_parts_per_million"), NotNull] public int FortuneMaxBonusPartsPerMillion { get; set; }
    [Column("none_entry_id"), NotNull] public string NoneEntryId { get; set; } = "__none__";
    [Column("description")] public string? Description { get; set; }
    [Column("created_at")] public DateTime? CreatedAt { get; set; }
}
