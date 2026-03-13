namespace GameServer.Runtime;

[Flags]
public enum CharacterRuntimeDirtyFlags
{
    None = 0,
    BaseStats = 1 << 0,
    CurrentState = 1 << 1,
}
