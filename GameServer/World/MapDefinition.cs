using System.Numerics;

namespace GameServer.World;

public sealed record MapDefinition(MapTemplate Template)
{
    public int MapId => Template.TemplateId;
    public string Name => Template.Name;
    public MapType Type => Template.Type;
    public string ClientMapKey => Template.ClientMapKey;
    public decimal SpiritualEnergyPerMinute => Template.SpiritualEnergyPerMinute;
    public IReadOnlyList<int> AdjacentMapIds => Template.AdjacentMapIds;
    public float Width => Template.Width;
    public float Height => Template.Height;
    public float CellSize => Template.CellSize;
    public int MaxPublicZoneCount => Template.MaxPublicZoneCount;
    public int MaxPlayersPerZone => Template.MaxPlayersPerZone;
    public bool SupportsCavePlacement => Template.SupportsCavePlacement;
    public Vector2 DefaultSpawnPosition => Template.DefaultSpawnPosition;
    public bool IsPrivatePerPlayer => Template.IsPrivatePerPlayer;
    public int DefaultZoneIndex => Template.DefaultZoneIndex;
    public IReadOnlyList<MapSpawnPointDefinition> SpawnPoints { get; init; } = Array.Empty<MapSpawnPointDefinition>();
    public IReadOnlyList<MapPortalDefinition> Portals { get; init; } = Array.Empty<MapPortalDefinition>();

    public Vector2 ClampPosition(Vector2 position) => Template.ClampPosition(position);

    public bool CanTravelTo(int otherMapId) => AdjacentMapIds.Contains(otherMapId);

    public bool TryGetSpawnPoint(int spawnPointId, out MapSpawnPointDefinition spawnPoint)
    {
        for (var i = 0; i < SpawnPoints.Count; i++)
        {
            var candidate = SpawnPoints[i];
            if (candidate.Id != spawnPointId)
                continue;

            spawnPoint = candidate;
            return true;
        }

        spawnPoint = null!;
        return false;
    }

    public bool TryGetPortal(int portalId, out MapPortalDefinition portal)
    {
        for (var i = 0; i < Portals.Count; i++)
        {
            var candidate = Portals[i];
            if (candidate.Id != portalId)
                continue;

            portal = candidate;
            return true;
        }

        portal = null!;
        return false;
    }

    public Vector2 ResolveSpawnPosition(int? spawnPointId)
    {
        if (spawnPointId.HasValue && TryGetSpawnPoint(spawnPointId.Value, out var spawnPoint))
            return ClampPosition(spawnPoint.Position);

        return DefaultSpawnPosition;
    }
}
