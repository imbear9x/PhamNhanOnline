using GameServer.DTO;
using GameServer.Time;

namespace GameServer.Runtime;

public static class CharacterLifespanRules
{
    public const int Unlimited = -1;

    public static int ResolveMaxLifespanYears(CharacterBaseStatsDto baseStats, int fallback)
    {
        if (baseStats.RealmLifespan == Unlimited)
            return Unlimited;

        if (!baseStats.RealmLifespan.HasValue)
            return fallback;

        var lifespanBonus = baseStats.LifespanBonus ?? 0;
        return Math.Max(0, baseStats.RealmLifespan.Value + lifespanBonus);
    }

    public static long CreateLifespanEndGameMinute(CharacterBaseStatsDto baseStats, GameTimeSnapshot snapshot, int fallbackRealmLifespan)
    {
        var maxLifespanYears = ResolveMaxLifespanYears(baseStats, fallbackRealmLifespan);
        if (maxLifespanYears == Unlimited)
            return Unlimited;

        return checked(snapshot.CurrentGameMinute + snapshot.YearsToGameMinutes(maxLifespanYears));
    }

    public static int CalculateRemainingLifespanYears(long lifespanEndGameMinute, GameTimeSnapshot snapshot)
    {
        return snapshot.RemainingLifespanYears(lifespanEndGameMinute);
    }

    public static long AdjustLifespanEndGameMinute(
        CharacterBaseStatsDto previousBaseStats,
        CharacterBaseStatsDto nextBaseStats,
        long currentLifespanEndGameMinute,
        GameTimeSnapshot snapshot)
    {
        var previousMaxYears = ResolveMaxLifespanYears(previousBaseStats, 0);
        var nextMaxYears = ResolveMaxLifespanYears(nextBaseStats, 0);

        if (nextMaxYears == Unlimited)
            return Unlimited;

        if (previousMaxYears == Unlimited)
            return checked(snapshot.CurrentGameMinute + snapshot.YearsToGameMinutes(nextMaxYears));

        var deltaYears = nextMaxYears - previousMaxYears;
        var adjusted = checked(currentLifespanEndGameMinute + snapshot.YearsToGameMinutes(deltaYears));
        return Math.Max(snapshot.CurrentGameMinute, adjusted);
    }
}
