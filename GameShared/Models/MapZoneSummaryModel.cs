using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct MapZoneSummaryModel
{
    public int ZoneIndex;
    public int CurrentPlayerCount;
    public int MaxPlayerCount;
    public bool IsActive;
}
