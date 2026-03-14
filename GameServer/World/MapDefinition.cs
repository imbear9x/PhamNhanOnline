using System.Numerics;

namespace GameServer.World;

public sealed record MapDefinition(
    int MapId,
    string Name,
    MapTemplate Template)
{
    public float Width => Template.Width;
    public float Height => Template.Height;
    public float CellSize => Template.CellSize;
    public float InterestRadius => Template.InterestRadius;
    public int MaxPlayersPerInstance => Template.MaxPlayersPerInstance;
    public Vector2 DefaultSpawnPosition => Template.DefaultSpawnPosition;

    public Vector2 ClampPosition(Vector2 position) => Template.ClampPosition(position);
}
