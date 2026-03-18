using System.IO;

namespace GameShared.Packets;

public partial class GetInventoryPacket
{
    public void Serialize(BinaryWriter writer)
    {
        writer.Write(0UL);
    }

    public void Deserialize(BinaryReader reader)
    {
        _ = reader.ReadUInt64();
    }
}

public partial class GetInventoryResultPacket
{
    private ulong _mask;

    public void Serialize(BinaryWriter writer)
    {
        ulong mask = 0;
        if (!global::System.Collections.Generic.EqualityComparer<bool?>.Default.Equals(Success, default!)) mask |= 1UL << 0;
        if (!global::System.Collections.Generic.EqualityComparer<global::GameShared.Messages.MessageCode?>.Default.Equals(Code, default!)) mask |= 1UL << 1;
        if (!global::System.Collections.Generic.EqualityComparer<global::System.Collections.Generic.List<global::GameShared.Models.InventoryItemModel>?>.Default.Equals(Items, default!)) mask |= 1UL << 2;

        writer.Write(mask);

        if ((mask & (1UL << 0)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, Success.Value);
        if ((mask & (1UL << 1)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, (int)Code.Value);
        if ((mask & (1UL << 2)) != 0)
            global::GameShared.Packets.PacketModelSerializer.WriteList(writer, Items);
    }

    public void Deserialize(BinaryReader reader)
    {
        _mask = reader.ReadUInt64();

        if ((_mask & (1UL << 0)) != 0)
            Success = (bool?)global::GameShared.Packets.PacketReader.ReadBool(reader);
        if ((_mask & (1UL << 1)) != 0)
            Code = (global::GameShared.Messages.MessageCode?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 2)) != 0)
            Items = global::GameShared.Packets.PacketModelSerializer.ReadList<global::GameShared.Models.InventoryItemModel>(reader);
    }
}
