using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("game_time_state")]
public sealed class GameTimeState
{
    [Column("id", IsPrimaryKey = true)]
    public int Id { get; set; }

    [Column("anchor_utc")]
    public DateTime AnchorUtc { get; set; }

    [Column("anchor_game_minute")]
    public long AnchorGameMinute { get; set; }

    [Column("game_minutes_per_real_minute")]
    public double GameMinutesPerRealMinute { get; set; }

    [Column("days_per_game_year")]
    public int DaysPerGameYear { get; set; }

    [Column("runtime_save_interval_seconds")]
    public int RuntimeSaveIntervalSeconds { get; set; }

    [Column("derived_state_refresh_interval_seconds")]
    public int DerivedStateRefreshIntervalSeconds { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
