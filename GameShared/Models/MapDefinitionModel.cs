using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct MapDefinitionModel
{
    public int MapId;
    public string Name;
    public float Width;
    public float Height;
    public float CellSize;
    public float InterestRadius;
    public float DefaultSpawnX;
    public float DefaultSpawnY;
    public int MaxPlayersPerInstance;
}
