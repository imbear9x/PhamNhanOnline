using System.Numerics;

namespace GameServer.World;

public sealed record MapTemplate(
    string TemplateCode,
    float Width,
    float Height,
    float CellSize,
    float InterestRadius,
    int MaxPlayersPerInstance,
    Vector2 DefaultSpawnPosition)
{
    public Vector2 ClampPosition(Vector2 position)
    {
        return new Vector2(
            Math.Clamp(position.X, 0, Width),
            Math.Clamp(position.Y, 0, Height));
    }
}
