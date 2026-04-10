using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct PracticeRewardItemModel
{
    public ItemTemplateSummaryModel Item;
    public int Quantity;
}
