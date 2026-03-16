using System.IO;

namespace GameShared.Packets;

public partial class GetMapZonesPacket
{
    private ulong _mask;

    public void Serialize(BinaryWriter writer)
    {
        ulong mask = 0;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(MapId, default!)) mask |= 1UL << 0;

        writer.Write(mask);

        if ((mask & (1UL << 0)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, MapId.Value);
    }

    public void Deserialize(BinaryReader reader)
    {
        _mask = reader.ReadUInt64();

        if ((_mask & (1UL << 0)) != 0)
            MapId = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
    }
}

public partial class GetMapZonesResultPacket
{
    private ulong _mask;

    public void Serialize(BinaryWriter writer)
    {
        ulong mask = 0;
        if (!global::System.Collections.Generic.EqualityComparer<bool?>.Default.Equals(Success, default!)) mask |= 1UL << 0;
        if (!global::System.Collections.Generic.EqualityComparer<global::GameShared.Messages.MessageCode?>.Default.Equals(Code, default!)) mask |= 1UL << 1;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(MapId, default!)) mask |= 1UL << 2;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(CurrentZoneIndex, default!)) mask |= 1UL << 3;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(MaxZoneCount, default!)) mask |= 1UL << 4;
        if (!global::System.Collections.Generic.EqualityComparer<bool?>.Default.Equals(SupportsCavePlacement, default!)) mask |= 1UL << 5;
        if (!global::System.Collections.Generic.EqualityComparer<global::System.Collections.Generic.List<global::GameShared.Models.MapZoneSummaryModel>?>.Default.Equals(Zones, default!)) mask |= 1UL << 6;

        writer.Write(mask);

        if ((mask & (1UL << 0)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, Success.Value);
        if ((mask & (1UL << 1)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, (int)Code.Value);
        if ((mask & (1UL << 2)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, MapId.Value);
        if ((mask & (1UL << 3)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, CurrentZoneIndex.Value);
        if ((mask & (1UL << 4)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, MaxZoneCount.Value);
        if ((mask & (1UL << 5)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, SupportsCavePlacement.Value);
        if ((mask & (1UL << 6)) != 0)
            global::GameShared.Packets.PacketModelSerializer.WriteList(writer, Zones);
    }

    public void Deserialize(BinaryReader reader)
    {
        _mask = reader.ReadUInt64();

        if ((_mask & (1UL << 0)) != 0)
            Success = (bool?)global::GameShared.Packets.PacketReader.ReadBool(reader);
        if ((_mask & (1UL << 1)) != 0)
            Code = (global::GameShared.Messages.MessageCode?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 2)) != 0)
            MapId = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 3)) != 0)
            CurrentZoneIndex = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 4)) != 0)
            MaxZoneCount = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 5)) != 0)
            SupportsCavePlacement = (bool?)global::GameShared.Packets.PacketReader.ReadBool(reader);
        if ((_mask & (1UL << 6)) != 0)
            Zones = global::GameShared.Packets.PacketModelSerializer.ReadList<global::GameShared.Models.MapZoneSummaryModel>(reader);
    }
}

public partial class SwitchMapZonePacket
{
    private ulong _mask;

    public void Serialize(BinaryWriter writer)
    {
        ulong mask = 0;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(MapId, default!)) mask |= 1UL << 0;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(TargetZoneIndex, default!)) mask |= 1UL << 1;

        writer.Write(mask);

        if ((mask & (1UL << 0)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, MapId.Value);
        if ((mask & (1UL << 1)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, TargetZoneIndex.Value);
    }

    public void Deserialize(BinaryReader reader)
    {
        _mask = reader.ReadUInt64();

        if ((_mask & (1UL << 0)) != 0)
            MapId = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 1)) != 0)
            TargetZoneIndex = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
    }
}

public partial class SwitchMapZoneResultPacket
{
    private ulong _mask;

    public void Serialize(BinaryWriter writer)
    {
        ulong mask = 0;
        if (!global::System.Collections.Generic.EqualityComparer<bool?>.Default.Equals(Success, default!)) mask |= 1UL << 0;
        if (!global::System.Collections.Generic.EqualityComparer<global::GameShared.Messages.MessageCode?>.Default.Equals(Code, default!)) mask |= 1UL << 1;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(MapId, default!)) mask |= 1UL << 2;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(ZoneIndex, default!)) mask |= 1UL << 3;
        if (!global::System.Collections.Generic.EqualityComparer<global::GameShared.Models.MapZoneDetailModel?>.Default.Equals(Zone, default!)) mask |= 1UL << 4;

        writer.Write(mask);

        if ((mask & (1UL << 0)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, Success.Value);
        if ((mask & (1UL << 1)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, (int)Code.Value);
        if ((mask & (1UL << 2)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, MapId.Value);
        if ((mask & (1UL << 3)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, ZoneIndex.Value);
        if ((mask & (1UL << 4)) != 0)
            global::GameShared.Packets.PacketModelSerializer.Write(writer, Zone.Value);
    }

    public void Deserialize(BinaryReader reader)
    {
        _mask = reader.ReadUInt64();

        if ((_mask & (1UL << 0)) != 0)
            Success = (bool?)global::GameShared.Packets.PacketReader.ReadBool(reader);
        if ((_mask & (1UL << 1)) != 0)
            Code = (global::GameShared.Messages.MessageCode?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 2)) != 0)
            MapId = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 3)) != 0)
            ZoneIndex = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 4)) != 0)
            Zone = global::GameShared.Packets.PacketModelSerializer.Read<global::GameShared.Models.MapZoneDetailModel>(reader);
    }
}
