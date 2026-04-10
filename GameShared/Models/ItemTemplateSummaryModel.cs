using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct ItemTemplateSummaryModel
{
    public int ItemTemplateId;
    public string? Code;
    public string? Name;
    public int ItemType;
    public int Rarity;
    public string? Icon;
    public string? BackgroundIcon;
    public string? Description;
    public int MaxStack;
    public bool IsStackable;
}
