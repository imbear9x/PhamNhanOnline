using System.Numerics;

namespace GameServer.World;

public sealed record MapTemplate(
    int TemplateId,
    string Name,
    MapType Type,
    string ClientMapKey,
    decimal SpiritualEnergyPerMinute,
    IReadOnlyList<int> AdjacentMapIds,
    float Width,
    float Height,
    float CellSize,
    float InterestRadius,
    int MaxPublicZoneCount,
    int MaxPlayersPerZone,
    bool SupportsCavePlacement,
    Vector2 DefaultSpawnPosition,
    bool IsPrivatePerPlayer)
{
    public int DefaultZoneIndex => IsPrivatePerPlayer ? 0 : 1;

    public Vector2 ClampPosition(Vector2 position)
    {
        return new Vector2(
            Math.Clamp(position.X, 0, Width),
            Math.Clamp(position.Y, 0, Height));
    }
}
