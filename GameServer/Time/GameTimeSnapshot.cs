namespace GameServer.Time;

public readonly record struct GameTimeSnapshot(
    DateTime UtcNow,
    long CurrentGameMinute,
    int DaysPerGameYear,
    double GameMinutesPerRealMinute)
{
    public const int MinutesPerGameDay = 24 * 60;

    public long CurrentGameDay => CurrentGameMinute / MinutesPerGameDay;

    public long YearsToGameMinutes(int years)
    {
        if (years <= 0)
            return 0;

        return checked((long)years * DaysPerGameYear * MinutesPerGameDay);
    }

    public int RemainingLifespanYears(long lifespanEndGameMinute)
    {
        if (lifespanEndGameMinute == Runtime.CharacterLifespanRules.Unlimited)
            return Runtime.CharacterLifespanRules.Unlimited;

        var remainingGameMinutes = Math.Max(0, lifespanEndGameMinute - CurrentGameMinute);
        if (remainingGameMinutes == 0)
            return 0;

        var minutesPerGameYear = checked((long)DaysPerGameYear * MinutesPerGameDay);
        return (int)Math.Ceiling(remainingGameMinutes / (double)minutesPerGameYear);
    }
}
