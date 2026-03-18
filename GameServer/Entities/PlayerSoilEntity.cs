using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("player_soils")]
public sealed class PlayerSoilEntity
{
    [Column("player_item_id"), PrimaryKey] public long PlayerItemId { get; set; }
    [Column("total_used_seconds"), NotNull] public long TotalUsedSeconds { get; set; }
    [Column("state"), NotNull] public int State { get; set; }
    [Column("inserted_plot_id")] public long? InsertedPlotId { get; set; }
    [Column("updated_at"), NotNull] public DateTime UpdatedAt { get; set; }
}

