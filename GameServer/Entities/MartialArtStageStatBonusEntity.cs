using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("martial_art_stage_stat_bonuses")]
public sealed class MartialArtStageStatBonusEntity
{
    [Column("id"), PrimaryKey] public int Id { get; set; }
    [Column("martial_art_stage_id"), NotNull] public int MartialArtStageId { get; set; }
    [Column("stat_type"), NotNull] public int StatType { get; set; }
    [Column("value"), NotNull] public decimal Value { get; set; }
    [Column("value_type"), NotNull] public int ValueType { get; set; }
    [Column("created_at")] public DateTime? CreatedAt { get; set; }
}
