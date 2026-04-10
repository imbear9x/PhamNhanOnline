using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct NotificationItemModel
{
    public ItemTemplateSummaryModel Item;
    public int Quantity;
}
