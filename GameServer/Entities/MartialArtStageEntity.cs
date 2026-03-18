using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("martial_art_stages")]
public sealed class MartialArtStageEntity
{
    [Column("id"), PrimaryKey] public int Id { get; set; }
    [Column("martial_art_id"), NotNull] public int MartialArtId { get; set; }
    [Column("stage_level"), NotNull] public int StageLevel { get; set; }
    [Column("exp_required"), NotNull] public long ExpRequired { get; set; }
    [Column("is_bottleneck"), NotNull] public bool IsBottleneck { get; set; }
    [Column("breakthrough_base_rate")] public double? BreakthroughBaseRate { get; set; }
    [Column("breakthrough_exp_penalty"), NotNull] public long BreakthroughExpPenalty { get; set; }
    [Column("created_at")] public DateTime? CreatedAt { get; set; }
}
