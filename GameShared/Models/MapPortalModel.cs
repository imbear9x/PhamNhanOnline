using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct MapPortalModel
{
    public int Id;
    public string Code;
    public string Name;
    public float SourceX;
    public float SourceY;
    public float InteractionRadius;
    public int InteractionMode;
    public int TargetMapId;
    public string TargetMapName;
    public int TargetSpawnPointId;
    public bool IsEnabled;
    public int OrderIndex;
    public string Description;
}
