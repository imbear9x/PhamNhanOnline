using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct GroundRewardItemModel
{
    public int ItemTemplateId;
    public string Code;
    public string Name;
    public int ItemType;
    public int Rarity;
    public int Quantity;
    public bool IsBound;
    public string? Icon;
    public string? BackgroundIcon;
}
