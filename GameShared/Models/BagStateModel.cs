using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct BagStateModel
{
    public int Grade;
    public int UsedSlots;
    public int TotalSlots;
    public string? DisplayName;
}