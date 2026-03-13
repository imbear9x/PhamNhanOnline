using GameServer.DTO;

namespace GameServer.Runtime;

public sealed record CharacterRuntimeSnapshot(
    CharacterBaseStatsDto BaseStats,
    CharacterCurrentStateDto CurrentState,
    long BaseStatsVersion,
    long CurrentStateVersion,
    CharacterRuntimeDirtyFlags DirtyFlags);
