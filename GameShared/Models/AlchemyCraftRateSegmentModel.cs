using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct AlchemyCraftRateSegmentModel
{
    public double SuccessRate;
    public int Count;
}
