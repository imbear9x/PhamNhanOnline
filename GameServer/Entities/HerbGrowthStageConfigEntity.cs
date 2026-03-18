using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("herb_growth_stage_configs")]
public sealed class HerbGrowthStageConfigEntity
{
    [Column("id"), PrimaryKey, Identity] public int Id { get; set; }
    [Column("herb_template_id"), NotNull] public int HerbTemplateId { get; set; }
    [Column("stage"), NotNull] public int Stage { get; set; }
    [Column("stage_name"), NotNull] public string StageName { get; set; } = string.Empty;
    [Column("required_growth_seconds"), NotNull] public long RequiredGrowthSeconds { get; set; }
    [Column("age_years"), NotNull] public int AgeYears { get; set; }
}

