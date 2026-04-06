using GameServer.DTO;

namespace GameServer.Runtime;

public static class CharacterRuntimeStateCodes
{
    public const int Idle = 0;
    public const int CombatDead = 1;
    public const int Dead = CombatDead;
    public const int LifespanExpired = 2;
    public const int Cultivating = 3;
    public const int Casting = 4;

    public static bool IsCombatDead(int currentState)
    {
        return currentState == CombatDead;
    }

    public static bool IsPermanentlyDead(int currentState)
    {
        return currentState == LifespanExpired;
    }

    public static bool IsDefeated(CharacterCurrentStateDto? currentState)
    {
        if (currentState is null)
            return true;

        return currentState.IsExpired ||
               IsCombatDead(currentState.CurrentState) ||
               IsPermanentlyDead(currentState.CurrentState);
    }
}
