using System.Numerics;

namespace GameServer.World;

public enum MapSpawnPointCategory
{
    Start = 1,
    End = 2,
    Middle = 3,
    Custom = 4
}

public enum MapPortalInteractionMode
{
    Touch = 1,
    Interact = 2
}

public enum MapEntryReason
{
    Unknown = 0,
    SavedPosition = 1,
    DefaultSpawn = 2,
    SpawnPoint = 3,
    Portal = 4
}

public sealed record MapEntryContext(
    MapEntryReason Reason,
    int? PortalId,
    int? SpawnPointId,
    Vector2 Position);

public sealed record MapSpawnPointDefinition(
    int Id,
    int MapId,
    string Code,
    string Name,
    MapSpawnPointCategory Category,
    Vector2 Position,
    float? FacingDegrees,
    string Description);

public sealed record MapPortalDefinition(
    int Id,
    int SourceMapId,
    string Code,
    string Name,
    Vector2 SourcePosition,
    float InteractionRadius,
    MapPortalInteractionMode InteractionMode,
    int TargetMapId,
    string TargetMapName,
    int TargetSpawnPointId,
    bool IsEnabled,
    int OrderIndex,
    string Description);
