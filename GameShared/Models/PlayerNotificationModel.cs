using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct PlayerNotificationModel
{
    public long NotificationId;
    public int NotificationType;
    public int SourceType;
    public long? SourceId;
    public string? Title;
    public string? Message;
    public ItemTemplateSummaryModel? DisplayItem;
    public List<NotificationItemModel>? Items;
    public long? CreatedUnixMs;
}
