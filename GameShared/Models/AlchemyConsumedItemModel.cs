using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct AlchemyConsumedItemModel
{
    public long PlayerItemId;
    public ItemTemplateSummaryModel Item;
    public int Quantity;
}
