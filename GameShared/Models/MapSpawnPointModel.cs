using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct MapSpawnPointModel
{
    public int Id;
    public string Code;
    public string Name;
    public int SpawnCategory;
    public float PosX;
    public float PosY;
    public float? FacingDegrees;
    public string Description;
}
