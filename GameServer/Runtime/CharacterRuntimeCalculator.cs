using System.Numerics;
using GameServer.DTO;

namespace GameServer.Runtime;

public sealed class CharacterRuntimeCalculator
{
    public CharacterCurrentStateDto ApplyDamage(
        CharacterBaseStatsDto baseStats,
        CharacterCurrentStateDto currentState,
        int damage)
    {
        var appliedDamage = Math.Max(0, damage);
        var hpAfter = Math.Max(0, currentState.CurrentHp - appliedDamage);
        var isCombatDead = hpAfter <= 0;
        var nextState = currentState.IsExpired || CharacterRuntimeStateCodes.IsPermanentlyDead(currentState.CurrentState)
            ? CharacterRuntimeStateCodes.LifespanExpired
            : (isCombatDead ? CharacterRuntimeStateCodes.CombatDead : currentState.CurrentState);

        return currentState with
        {
            CurrentHp = hpAfter,
            IsExpired = currentState.IsExpired,
            CurrentState = nextState,
            CurrentMp = Clamp(currentState.CurrentMp, 0, baseStats.GetEffectiveMp()),
            CurrentStamina = Clamp(currentState.CurrentStamina, 0, baseStats.GetEffectiveStamina()),
        };
    }

    public CharacterCurrentStateDto RestoreResources(
        CharacterBaseStatsDto baseStats,
        CharacterCurrentStateDto currentState,
        int hpDelta,
        int mpDelta)
    {
        return ApplyResourceDelta(baseStats, currentState, hpDelta, mpDelta, 0);
    }

    public CharacterCurrentStateDto ApplyResourceDelta(
        CharacterBaseStatsDto baseStats,
        CharacterCurrentStateDto currentState,
        int hpDelta,
        int mpDelta,
        int staminaDelta)
    {
        var maxHp = baseStats.GetEffectiveHp();
        var maxMp = baseStats.GetEffectiveMp();
        var maxStamina = baseStats.GetEffectiveStamina();
        var hp = Clamp(currentState.CurrentHp + hpDelta, 0, maxHp);
        var mp = Clamp(currentState.CurrentMp + mpDelta, 0, maxMp);
        var stamina = Clamp(currentState.CurrentStamina + staminaDelta, 0, maxStamina);
        var isCombatDead = hp <= 0;
        var nextState = currentState.IsExpired || CharacterRuntimeStateCodes.IsPermanentlyDead(currentState.CurrentState)
            ? CharacterRuntimeStateCodes.LifespanExpired
            : (isCombatDead ? CharacterRuntimeStateCodes.CombatDead : currentState.CurrentState);

        return currentState with
        {
            CurrentHp = hp,
            CurrentMp = mp,
            CurrentStamina = stamina,
            IsExpired = currentState.IsExpired,
            CurrentState = nextState,
        };
    }

    public CharacterCurrentStateDto ClampCurrentStateToBaseStats(
        CharacterBaseStatsDto baseStats,
        CharacterCurrentStateDto currentState)
    {
        var maxHp = baseStats.GetEffectiveHp();
        var maxMp = baseStats.GetEffectiveMp();
        var maxStamina = baseStats.GetEffectiveStamina();
        var hp = Clamp(currentState.CurrentHp, 0, maxHp);
        var mp = Clamp(currentState.CurrentMp, 0, maxMp);
        var stamina = Clamp(currentState.CurrentStamina, 0, maxStamina);
        var isCombatDead = hp <= 0;
        var nextState = currentState.IsExpired || CharacterRuntimeStateCodes.IsPermanentlyDead(currentState.CurrentState)
            ? CharacterRuntimeStateCodes.LifespanExpired
            : (isCombatDead ? CharacterRuntimeStateCodes.CombatDead : currentState.CurrentState);

        return currentState with
        {
            CurrentHp = hp,
            CurrentMp = mp,
            CurrentStamina = stamina,
            IsExpired = currentState.IsExpired,
            CurrentState = nextState,
        };
    }

    public CharacterCurrentStateDto UpdatePosition(
        CharacterCurrentStateDto currentState,
        int? mapId,
        int? zoneIndex,
        Vector2 position)
    {
        return currentState with
        {
            CurrentMapId = mapId,
            CurrentZoneIndex = zoneIndex ?? currentState.CurrentZoneIndex,
            CurrentPosX = position.X,
            CurrentPosY = position.Y,
        };
    }

    private static int Clamp(int value, int min, int max)
    {
        if (max < min)
            return min;

        return Math.Clamp(value, min, max);
    }
}
