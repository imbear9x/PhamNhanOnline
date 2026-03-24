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

public partial class EquipInventoryItemPacket
{
    private ulong _mask;

    public void Serialize(BinaryWriter writer)
    {
        ulong mask = 0;
        if (!global::System.Collections.Generic.EqualityComparer<long?>.Default.Equals(PlayerItemId, default!)) mask |= 1UL << 0;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(Slot, default!)) mask |= 1UL << 1;

        writer.Write(mask);

        if ((mask & (1UL << 0)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, PlayerItemId.Value);
        if ((mask & (1UL << 1)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, Slot.Value);
    }

    public void Deserialize(BinaryReader reader)
    {
        _mask = reader.ReadUInt64();

        if ((_mask & (1UL << 0)) != 0)
            PlayerItemId = (long?)global::GameShared.Packets.PacketReader.ReadLong(reader);
        if ((_mask & (1UL << 1)) != 0)
            Slot = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
    }
}

public partial class EquipInventoryItemResultPacket
{
    private ulong _mask;

    public void Serialize(BinaryWriter writer)
    {
        ulong mask = 0;
        if (!global::System.Collections.Generic.EqualityComparer<bool?>.Default.Equals(Success, default!)) mask |= 1UL << 0;
        if (!global::System.Collections.Generic.EqualityComparer<global::GameShared.Messages.MessageCode?>.Default.Equals(Code, default!)) mask |= 1UL << 1;
        if (!global::System.Collections.Generic.EqualityComparer<global::System.Collections.Generic.List<global::GameShared.Models.InventoryItemModel>?>.Default.Equals(Items, default!)) mask |= 1UL << 2;
        if (!global::System.Collections.Generic.EqualityComparer<global::GameShared.Models.CharacterBaseStatsModel?>.Default.Equals(BaseStats, default!)) mask |= 1UL << 3;
        if (!global::System.Collections.Generic.EqualityComparer<global::GameShared.Models.CharacterCurrentStateModel?>.Default.Equals(CurrentState, default!)) mask |= 1UL << 4;

        writer.Write(mask);

        if ((mask & (1UL << 0)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, Success.Value);
        if ((mask & (1UL << 1)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, (int)Code.Value);
        if ((mask & (1UL << 2)) != 0)
            global::GameShared.Packets.PacketModelSerializer.WriteList(writer, Items);
        if ((mask & (1UL << 3)) != 0)
            global::GameShared.Packets.PacketModelSerializer.Write(writer, BaseStats.Value);
        if ((mask & (1UL << 4)) != 0)
            global::GameShared.Packets.PacketModelSerializer.Write(writer, CurrentState.Value);
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
        if ((_mask & (1UL << 3)) != 0)
            BaseStats = global::GameShared.Packets.PacketModelSerializer.Read<global::GameShared.Models.CharacterBaseStatsModel>(reader);
        if ((_mask & (1UL << 4)) != 0)
            CurrentState = global::GameShared.Packets.PacketModelSerializer.Read<global::GameShared.Models.CharacterCurrentStateModel>(reader);
    }
}

public partial class UnequipInventoryItemPacket
{
    private ulong _mask;

    public void Serialize(BinaryWriter writer)
    {
        ulong mask = 0;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(Slot, default!)) mask |= 1UL << 0;

        writer.Write(mask);

        if ((mask & (1UL << 0)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, Slot.Value);
    }

    public void Deserialize(BinaryReader reader)
    {
        _mask = reader.ReadUInt64();

        if ((_mask & (1UL << 0)) != 0)
            Slot = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
    }
}

public partial class UnequipInventoryItemResultPacket
{
    private ulong _mask;

    public void Serialize(BinaryWriter writer)
    {
        ulong mask = 0;
        if (!global::System.Collections.Generic.EqualityComparer<bool?>.Default.Equals(Success, default!)) mask |= 1UL << 0;
        if (!global::System.Collections.Generic.EqualityComparer<global::GameShared.Messages.MessageCode?>.Default.Equals(Code, default!)) mask |= 1UL << 1;
        if (!global::System.Collections.Generic.EqualityComparer<global::System.Collections.Generic.List<global::GameShared.Models.InventoryItemModel>?>.Default.Equals(Items, default!)) mask |= 1UL << 2;
        if (!global::System.Collections.Generic.EqualityComparer<global::GameShared.Models.CharacterBaseStatsModel?>.Default.Equals(BaseStats, default!)) mask |= 1UL << 3;
        if (!global::System.Collections.Generic.EqualityComparer<global::GameShared.Models.CharacterCurrentStateModel?>.Default.Equals(CurrentState, default!)) mask |= 1UL << 4;

        writer.Write(mask);

        if ((mask & (1UL << 0)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, Success.Value);
        if ((mask & (1UL << 1)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, (int)Code.Value);
        if ((mask & (1UL << 2)) != 0)
            global::GameShared.Packets.PacketModelSerializer.WriteList(writer, Items);
        if ((mask & (1UL << 3)) != 0)
            global::GameShared.Packets.PacketModelSerializer.Write(writer, BaseStats.Value);
        if ((mask & (1UL << 4)) != 0)
            global::GameShared.Packets.PacketModelSerializer.Write(writer, CurrentState.Value);
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
        if ((_mask & (1UL << 3)) != 0)
            BaseStats = global::GameShared.Packets.PacketModelSerializer.Read<global::GameShared.Models.CharacterBaseStatsModel>(reader);
        if ((_mask & (1UL << 4)) != 0)
            CurrentState = global::GameShared.Packets.PacketModelSerializer.Read<global::GameShared.Models.CharacterCurrentStateModel>(reader);
    }
}
