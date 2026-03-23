using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct InventoryItemModel
{
    public long PlayerItemId;
    public int ItemTemplateId;
    public string Code;
    public string Name;
    public int ItemType;
    public int Rarity;
    public int Quantity;
    public bool IsBound;
    public int MaxStack;
    public bool IsTradeable;
    public bool IsDroppable;
    public bool IsDestroyable;
    public string? Icon;
    public string? BackgroundIcon;
    public string? Description;
    public bool IsEquipped;
    public int? EquippedSlot;
    public int EnhanceLevel;
    public int? Durability;
}
