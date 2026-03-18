using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct GroundRewardItemModel
{
    public int ItemTemplateId;
    public int Quantity;
    public bool IsBound;
}
