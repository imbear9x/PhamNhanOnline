namespace PhamNhanOnline.Client.Features.Character.Application
{
    public static class ClientCharacterRuntimeStateCodes
    {
        public const int Unknown = 0;
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
    }
}
