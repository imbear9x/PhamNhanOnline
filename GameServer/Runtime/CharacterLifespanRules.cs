using GameServer.DTO;

namespace GameServer.Runtime;

public static class CharacterLifespanRules
{
    public const int Unlimited = -1;

    public static int ResolveMaxLifespan(CharacterBaseStatsDto baseStats, int fallback)
    {
        if (baseStats.RealmLifespan == Unlimited)
            return Unlimited;

        if (!baseStats.RealmLifespan.HasValue)
            return fallback;

        var lifespanBonus = baseStats.LifespanBonus ?? 0;
        return Math.Max(0, baseStats.RealmLifespan.Value + lifespanBonus);
    }

    public static int NormalizeRemainingLifespan(int remainingLifespan, int maxLifespan)
    {
        if (maxLifespan == Unlimited)
            return Unlimited;

        return Math.Clamp(remainingLifespan, 0, maxLifespan);
    }
}
