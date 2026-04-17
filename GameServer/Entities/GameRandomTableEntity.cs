using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("game_random_tables")]
public sealed class GameRandomTableEntity
{
    [Column("id"), PrimaryKey] public int Id { get; set; }
    [Column("table_id"), NotNull] public string TableId { get; set; } = string.Empty;
    [Column("mode"), NotNull] public int Mode { get; set; }
    [Column("luck_enabled"), NotNull] public bool LuckEnabled { get; set; }
    [Column("luck_bonus_parts_per_million_per_luck_point"), NotNull] public int LuckBonusPartsPerMillionPerLuckPoint { get; set; }
    [Column("luck_max_bonus_parts_per_million"), NotNull] public int LuckMaxBonusPartsPerMillion { get; set; }
    [Column("none_entry_id"), NotNull] public string NoneEntryId { get; set; } = "__none__";
    [Column("description")] public string? Description { get; set; }
    [Column("created_at")] public DateTime? CreatedAt { get; set; }
}
