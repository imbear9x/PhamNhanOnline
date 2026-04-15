using GameServer.DTO;

namespace GameServer.Runtime;

public static class CharacterLifespanRules
{
    public const int Unlimited = -1;
    public static readonly DateTime UnlimitedUtc = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);

    public static int ResolveMaxLifespanDays(CharacterBaseStatsDto baseStats, int fallbackDays)
    {
        if (baseStats.RealmLifespan == Unlimited)
            return Unlimited;

        if (!baseStats.RealmLifespan.HasValue)
            return fallbackDays;

        var lifespanBonus = baseStats.LifespanBonus ?? 0;
        return Math.Max(0, baseStats.RealmLifespan.Value + lifespanBonus);
    }

    public static DateTime CreateLifespanEndUtc(CharacterBaseStatsDto baseStats, DateTime utcNow, int fallbackRealmLifespanDays)
    {
        var maxLifespanDays = ResolveMaxLifespanDays(baseStats, fallbackRealmLifespanDays);
        if (maxLifespanDays == Unlimited)
            return UnlimitedUtc;

        return NormalizeUtc(utcNow).AddDays(maxLifespanDays);
    }

    public static DateTime? ResolveLifespanEndUtc(DateTime? firstEnterWorldAtUtc, CharacterBaseStatsDto? baseStats, int fallbackRealmLifespanDays)
    {
        if (!firstEnterWorldAtUtc.HasValue || baseStats is null)
            return null;

        return CreateLifespanEndUtc(baseStats, firstEnterWorldAtUtc.Value, fallbackRealmLifespanDays);
    }

    public static TimeSpan CalculateRemainingLifespan(DateTime lifespanEndUtc, DateTime utcNow)
    {
        if (IsUnlimited(lifespanEndUtc))
            return TimeSpan.MaxValue;

        return NormalizeUtc(lifespanEndUtc) - NormalizeUtc(utcNow);
    }

    public static bool IsExpired(DateTime lifespanEndUtc, DateTime utcNow)
    {
        if (IsUnlimited(lifespanEndUtc))
            return false;

        return NormalizeUtc(lifespanEndUtc) <= NormalizeUtc(utcNow);
    }

    public static DateTime AdjustLifespanEndUtc(
        CharacterBaseStatsDto previousBaseStats,
        CharacterBaseStatsDto nextBaseStats,
        DateTime currentLifespanEndUtc,
        DateTime utcNow)
    {
        var previousMaxDays = ResolveMaxLifespanDays(previousBaseStats, 0);
        var nextMaxDays = ResolveMaxLifespanDays(nextBaseStats, 0);

        if (nextMaxDays == Unlimited)
            return UnlimitedUtc;

        var normalizedNow = NormalizeUtc(utcNow);
        if (previousMaxDays == Unlimited)
            return normalizedNow.AddDays(nextMaxDays);

        var deltaDays = nextMaxDays - previousMaxDays;
        var adjusted = NormalizeUtc(currentLifespanEndUtc).AddDays(deltaDays);
        return adjusted < normalizedNow ? normalizedNow : adjusted;
    }

    public static bool IsUnlimited(DateTime lifespanEndUtc)
    {
        return NormalizeUtc(lifespanEndUtc) >= UnlimitedUtc;
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
