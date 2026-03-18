using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("player_garden_plots")]
public sealed class PlayerGardenPlotEntity
{
    [Column("id"), PrimaryKey, Identity] public long Id { get; set; }
    [Column("player_id"), NotNull] public Guid PlayerId { get; set; }
    [Column("cave_id"), NotNull] public long CaveId { get; set; }
    [Column("plot_index"), NotNull] public int PlotIndex { get; set; }
    [Column("current_soil_player_item_id")] public long? CurrentSoilPlayerItemId { get; set; }
    [Column("current_player_herb_id")] public long? CurrentPlayerHerbId { get; set; }
    [Column("created_at"), NotNull] public DateTime CreatedAt { get; set; }
    [Column("updated_at"), NotNull] public DateTime UpdatedAt { get; set; }
}

