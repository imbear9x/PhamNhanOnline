using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct MapZoneDetailModel
{
    public int ZoneIndex;
    public int SpiritualEnergyTemplateId;
    public string SpiritualEnergyCode;
    public string SpiritualEnergyName;
    public decimal SpiritualEnergyPerMinute;
}
