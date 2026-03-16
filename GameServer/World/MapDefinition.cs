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
    public float InterestRadius => Template.InterestRadius;
    public int MaxPublicZoneCount => Template.MaxPublicZoneCount;
    public int MaxPlayersPerZone => Template.MaxPlayersPerZone;
    public bool SupportsCavePlacement => Template.SupportsCavePlacement;
    public Vector2 DefaultSpawnPosition => Template.DefaultSpawnPosition;
    public bool IsPrivatePerPlayer => Template.IsPrivatePerPlayer;
    public int DefaultZoneIndex => Template.DefaultZoneIndex;

    public Vector2 ClampPosition(Vector2 position) => Template.ClampPosition(position);

    public bool CanTravelTo(int otherMapId) => AdjacentMapIds.Contains(otherMapId);
}
