using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct MapDefinitionModel
{
    public int MapId;
    public string Name;
    public int MapType;
    public string ClientMapKey;
    public List<int> AdjacentMapIds;
    public float Width;
    public float Height;
    public float CellSize;
    public float DefaultSpawnX;
    public float DefaultSpawnY;
    public int MaxPublicZoneCount;
    public int MaxPlayersPerZone;
    public bool SupportsCavePlacement;
    public bool IsPrivatePerPlayer;
    public List<MapSpawnPointModel> SpawnPoints;
    public List<MapPortalModel> Portals;
}
