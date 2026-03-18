using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("player_herbs")]
public sealed class PlayerHerbEntity
{
    [Column("id"), PrimaryKey, Identity] public long Id { get; set; }
    [Column("player_id"), NotNull] public Guid PlayerId { get; set; }
    [Column("herb_template_id"), NotNull] public int HerbTemplateId { get; set; }
    [Column("current_stage"), NotNull] public int CurrentStage { get; set; }
    [Column("planted_at")] public DateTime? PlantedAt { get; set; }
    [Column("accumulated_growth_seconds"), NotNull] public long AccumulatedGrowthSeconds { get; set; }
    [Column("state"), NotNull] public int State { get; set; }
    [Column("current_plot_id")] public long? CurrentPlotId { get; set; }
    [Column("created_at"), NotNull] public DateTime CreatedAt { get; set; }
    [Column("updated_at"), NotNull] public DateTime UpdatedAt { get; set; }
}
