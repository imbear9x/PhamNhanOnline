namespace GameServer.Time;

public sealed class GameTimeConfig
{
    public DateTime AnchorUtc { get; set; } = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public long AnchorGameMinute { get; set; } = 0;
    public double GameMinutesPerRealMinute { get; set; } = 1440;
    public int DaysPerGameYear { get; set; } = 360;
    public int RuntimeSaveIntervalSeconds { get; set; } = 2;
    public int DerivedStateRefreshIntervalSeconds { get; set; } = 5;
}
