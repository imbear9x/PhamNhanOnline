namespace GameServer.Runtime;

public static class CharacterRuntimeStateCodes
{
    public const int Idle = 0;
    public const int Dead = 1;
    public const int LifespanExpired = 2;
    public const int Cultivating = 3;
    public const int Casting = 4;
}
