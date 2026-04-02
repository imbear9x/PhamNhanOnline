namespace GameServer.Runtime;

public static class CharacterStateTransitionReasons
{
    public const int LifespanExpired = 1;
    public const int CombatDead = 2;
    public const int Dead = CombatDead;
}
