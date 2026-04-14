using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct AlchemyOptionalInputSelectionModel
{
    public int InputId;
    public int Quantity;
}
