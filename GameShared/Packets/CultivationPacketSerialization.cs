using System.IO;

namespace GameShared.Packets;

// Temporary manual bindings for cultivation packets while the packet generator
// registry/output is not emitting these newer packet types yet.
public partial class StartCultivationPacket
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

public partial class StartCultivationResultPacket
{
    private ulong _mask;

    public void Serialize(BinaryWriter writer)
    {
        ulong mask = 0;
        if (!global::System.Collections.Generic.EqualityComparer<bool?>.Default.Equals(Success, default!)) mask |= 1UL << 0;
        if (!global::System.Collections.Generic.EqualityComparer<global::GameShared.Messages.MessageCode?>.Default.Equals(Code, default!)) mask |= 1UL << 1;
        if (!global::System.Collections.Generic.EqualityComparer<global::GameShared.Models.CharacterCurrentStateModel?>.Default.Equals(CurrentState, default!)) mask |= 1UL << 2;

        writer.Write(mask);

        if ((mask & (1UL << 0)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, Success.Value);
        if ((mask & (1UL << 1)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, (int)Code.Value);
        if ((mask & (1UL << 2)) != 0)
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
            CurrentState = (global::GameShared.Models.CharacterCurrentStateModel?)global::GameShared.Packets.PacketModelSerializer.Read<global::GameShared.Models.CharacterCurrentStateModel>(reader);
    }
}

public partial class StopCultivationPacket
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

public partial class StopCultivationResultPacket
{
    private ulong _mask;

    public void Serialize(BinaryWriter writer)
    {
        ulong mask = 0;
        if (!global::System.Collections.Generic.EqualityComparer<bool?>.Default.Equals(Success, default!)) mask |= 1UL << 0;
        if (!global::System.Collections.Generic.EqualityComparer<global::GameShared.Messages.MessageCode?>.Default.Equals(Code, default!)) mask |= 1UL << 1;
        if (!global::System.Collections.Generic.EqualityComparer<global::GameShared.Models.CharacterCurrentStateModel?>.Default.Equals(CurrentState, default!)) mask |= 1UL << 2;

        writer.Write(mask);

        if ((mask & (1UL << 0)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, Success.Value);
        if ((mask & (1UL << 1)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, (int)Code.Value);
        if ((mask & (1UL << 2)) != 0)
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
            CurrentState = (global::GameShared.Models.CharacterCurrentStateModel?)global::GameShared.Packets.PacketModelSerializer.Read<global::GameShared.Models.CharacterCurrentStateModel>(reader);
    }
}

public partial class BreakthroughPacket
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

public partial class BreakthroughResultPacket
{
    private ulong _mask;

    public void Serialize(BinaryWriter writer)
    {
        ulong mask = 0;
        if (!global::System.Collections.Generic.EqualityComparer<bool?>.Default.Equals(Success, default!)) mask |= 1UL << 0;
        if (!global::System.Collections.Generic.EqualityComparer<global::GameShared.Messages.MessageCode?>.Default.Equals(Code, default!)) mask |= 1UL << 1;
        if (!global::System.Collections.Generic.EqualityComparer<global::GameShared.Models.CharacterBaseStatsModel?>.Default.Equals(BaseStats, default!)) mask |= 1UL << 2;
        if (!global::System.Collections.Generic.EqualityComparer<global::GameShared.Models.CharacterCurrentStateModel?>.Default.Equals(CurrentState, default!)) mask |= 1UL << 3;

        writer.Write(mask);

        if ((mask & (1UL << 0)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, Success.Value);
        if ((mask & (1UL << 1)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, (int)Code.Value);
        if ((mask & (1UL << 2)) != 0)
            global::GameShared.Packets.PacketModelSerializer.Write(writer, BaseStats.Value);
        if ((mask & (1UL << 3)) != 0)
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
            BaseStats = (global::GameShared.Models.CharacterBaseStatsModel?)global::GameShared.Packets.PacketModelSerializer.Read<global::GameShared.Models.CharacterBaseStatsModel>(reader);
        if ((_mask & (1UL << 3)) != 0)
            CurrentState = (global::GameShared.Models.CharacterCurrentStateModel?)global::GameShared.Packets.PacketModelSerializer.Read<global::GameShared.Models.CharacterCurrentStateModel>(reader);
    }
}

public partial class AllocatePotentialPacket
{
    private ulong _mask;

    public void Serialize(BinaryWriter writer)
    {
        ulong mask = 0;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(TargetStat, default!)) mask |= 1UL << 0;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(RequestedPotentialAmount, default!)) mask |= 1UL << 1;

        writer.Write(mask);

        if ((mask & (1UL << 0)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, TargetStat.Value);
        if ((mask & (1UL << 1)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, RequestedPotentialAmount.Value);
    }

    public void Deserialize(BinaryReader reader)
    {
        _mask = reader.ReadUInt64();

        if ((_mask & (1UL << 0)) != 0)
            TargetStat = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 1)) != 0)
            RequestedPotentialAmount = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
    }
}

public partial class AllocatePotentialResultPacket
{
    private ulong _mask;

    public void Serialize(BinaryWriter writer)
    {
        ulong mask = 0;
        if (!global::System.Collections.Generic.EqualityComparer<bool?>.Default.Equals(Success, default!)) mask |= 1UL << 0;
        if (!global::System.Collections.Generic.EqualityComparer<global::GameShared.Messages.MessageCode?>.Default.Equals(Code, default!)) mask |= 1UL << 1;
        if (!global::System.Collections.Generic.EqualityComparer<global::GameShared.Models.CharacterBaseStatsModel?>.Default.Equals(BaseStats, default!)) mask |= 1UL << 2;
        if (!global::System.Collections.Generic.EqualityComparer<global::GameShared.Models.CharacterCurrentStateModel?>.Default.Equals(CurrentState, default!)) mask |= 1UL << 3;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(RequestedPotentialAmount, default!)) mask |= 1UL << 4;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(SpentPotentialAmount, default!)) mask |= 1UL << 5;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(AppliedUpgradeCount, default!)) mask |= 1UL << 6;

        writer.Write(mask);

        if ((mask & (1UL << 0)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, Success.Value);
        if ((mask & (1UL << 1)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, (int)Code.Value);
        if ((mask & (1UL << 2)) != 0)
            global::GameShared.Packets.PacketModelSerializer.Write(writer, BaseStats.Value);
        if ((mask & (1UL << 3)) != 0)
            global::GameShared.Packets.PacketModelSerializer.Write(writer, CurrentState.Value);
        if ((mask & (1UL << 4)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, RequestedPotentialAmount.Value);
        if ((mask & (1UL << 5)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, SpentPotentialAmount.Value);
        if ((mask & (1UL << 6)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, AppliedUpgradeCount.Value);
    }

    public void Deserialize(BinaryReader reader)
    {
        _mask = reader.ReadUInt64();

        if ((_mask & (1UL << 0)) != 0)
            Success = (bool?)global::GameShared.Packets.PacketReader.ReadBool(reader);
        if ((_mask & (1UL << 1)) != 0)
            Code = (global::GameShared.Messages.MessageCode?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 2)) != 0)
            BaseStats = (global::GameShared.Models.CharacterBaseStatsModel?)global::GameShared.Packets.PacketModelSerializer.Read<global::GameShared.Models.CharacterBaseStatsModel>(reader);
        if ((_mask & (1UL << 3)) != 0)
            CurrentState = (global::GameShared.Models.CharacterCurrentStateModel?)global::GameShared.Packets.PacketModelSerializer.Read<global::GameShared.Models.CharacterCurrentStateModel>(reader);
        if ((_mask & (1UL << 4)) != 0)
            RequestedPotentialAmount = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 5)) != 0)
            SpentPotentialAmount = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 6)) != 0)
            AppliedUpgradeCount = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
    }
}

public partial class CultivationRewardsGrantedPacket
{
    private ulong _mask;

    public void Serialize(BinaryWriter writer)
    {
        ulong mask = 0;
        if (!global::System.Collections.Generic.EqualityComparer<global::System.Guid?>.Default.Equals(CharacterId, default!)) mask |= 1UL << 0;
        if (!global::System.Collections.Generic.EqualityComparer<long?>.Default.Equals(CultivationGranted, default!)) mask |= 1UL << 1;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(UnallocatedPotentialGranted, default!)) mask |= 1UL << 2;
        if (!global::System.Collections.Generic.EqualityComparer<bool?>.Default.Equals(ReachedRealmCap, default!)) mask |= 1UL << 3;
        if (!global::System.Collections.Generic.EqualityComparer<bool?>.Default.Equals(IsOfflineSettlement, default!)) mask |= 1UL << 4;
        if (!global::System.Collections.Generic.EqualityComparer<long?>.Default.Equals(RewardedFromUnixMs, default!)) mask |= 1UL << 5;
        if (!global::System.Collections.Generic.EqualityComparer<long?>.Default.Equals(RewardedToUnixMs, default!)) mask |= 1UL << 6;

        writer.Write(mask);

        if ((mask & (1UL << 0)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, CharacterId.Value);
        if ((mask & (1UL << 1)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, CultivationGranted.Value);
        if ((mask & (1UL << 2)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, UnallocatedPotentialGranted.Value);
        if ((mask & (1UL << 3)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, ReachedRealmCap.Value);
        if ((mask & (1UL << 4)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, IsOfflineSettlement.Value);
        if ((mask & (1UL << 5)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, RewardedFromUnixMs.Value);
        if ((mask & (1UL << 6)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, RewardedToUnixMs.Value);
    }

    public void Deserialize(BinaryReader reader)
    {
        _mask = reader.ReadUInt64();

        if ((_mask & (1UL << 0)) != 0)
            CharacterId = (global::System.Guid?)global::GameShared.Packets.PacketReader.ReadGuid(reader);
        if ((_mask & (1UL << 1)) != 0)
            CultivationGranted = (long?)global::GameShared.Packets.PacketReader.ReadLong(reader);
        if ((_mask & (1UL << 2)) != 0)
            UnallocatedPotentialGranted = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 3)) != 0)
            ReachedRealmCap = (bool?)global::GameShared.Packets.PacketReader.ReadBool(reader);
        if ((_mask & (1UL << 4)) != 0)
            IsOfflineSettlement = (bool?)global::GameShared.Packets.PacketReader.ReadBool(reader);
        if ((_mask & (1UL << 5)) != 0)
            RewardedFromUnixMs = (long?)global::GameShared.Packets.PacketReader.ReadLong(reader);
        if ((_mask & (1UL << 6)) != 0)
            RewardedToUnixMs = (long?)global::GameShared.Packets.PacketReader.ReadLong(reader);
    }
}

public partial class GetOwnedMartialArtsPacket
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

public partial class GetOwnedMartialArtsResultPacket
{
    private ulong _mask;

    public void Serialize(BinaryWriter writer)
    {
        ulong mask = 0;
        if (!global::System.Collections.Generic.EqualityComparer<bool?>.Default.Equals(Success, default!)) mask |= 1UL << 0;
        if (!global::System.Collections.Generic.EqualityComparer<global::GameShared.Messages.MessageCode?>.Default.Equals(Code, default!)) mask |= 1UL << 1;
        if (!global::System.Collections.Generic.EqualityComparer<global::System.Collections.Generic.List<global::GameShared.Models.PlayerMartialArtModel>?>.Default.Equals(MartialArts, default!)) mask |= 1UL << 2;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(ActiveMartialArtId, default!)) mask |= 1UL << 3;

        writer.Write(mask);

        if ((mask & (1UL << 0)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, Success.Value);
        if ((mask & (1UL << 1)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, (int)Code.Value);
        if ((mask & (1UL << 2)) != 0)
            global::GameShared.Packets.PacketModelSerializer.WriteList(writer, MartialArts);
        if ((mask & (1UL << 3)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, ActiveMartialArtId.Value);
    }

    public void Deserialize(BinaryReader reader)
    {
        _mask = reader.ReadUInt64();

        if ((_mask & (1UL << 0)) != 0)
            Success = (bool?)global::GameShared.Packets.PacketReader.ReadBool(reader);
        if ((_mask & (1UL << 1)) != 0)
            Code = (global::GameShared.Messages.MessageCode?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 2)) != 0)
            MartialArts = global::GameShared.Packets.PacketModelSerializer.ReadList<global::GameShared.Models.PlayerMartialArtModel>(reader);
        if ((_mask & (1UL << 3)) != 0)
            ActiveMartialArtId = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
    }
}

public partial class UseMartialArtBookPacket
{
    private ulong _mask;

    public void Serialize(BinaryWriter writer)
    {
        ulong mask = 0;
        if (!global::System.Collections.Generic.EqualityComparer<long?>.Default.Equals(PlayerItemId, default!)) mask |= 1UL << 0;

        writer.Write(mask);

        if ((mask & (1UL << 0)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, PlayerItemId.Value);
    }

    public void Deserialize(BinaryReader reader)
    {
        _mask = reader.ReadUInt64();

        if ((_mask & (1UL << 0)) != 0)
            PlayerItemId = (long?)global::GameShared.Packets.PacketReader.ReadLong(reader);
    }
}

public partial class UseMartialArtBookResultPacket
{
    private ulong _mask;

    public void Serialize(BinaryWriter writer)
    {
        ulong mask = 0;
        if (!global::System.Collections.Generic.EqualityComparer<bool?>.Default.Equals(Success, default!)) mask |= 1UL << 0;
        if (!global::System.Collections.Generic.EqualityComparer<global::GameShared.Messages.MessageCode?>.Default.Equals(Code, default!)) mask |= 1UL << 1;
        if (!global::System.Collections.Generic.EqualityComparer<global::GameShared.Models.CharacterBaseStatsModel?>.Default.Equals(BaseStats, default!)) mask |= 1UL << 2;
        if (!global::System.Collections.Generic.EqualityComparer<global::GameShared.Models.PlayerMartialArtModel?>.Default.Equals(LearnedMartialArt, default!)) mask |= 1UL << 3;

        writer.Write(mask);

        if ((mask & (1UL << 0)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, Success.Value);
        if ((mask & (1UL << 1)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, (int)Code.Value);
        if ((mask & (1UL << 2)) != 0)
            global::GameShared.Packets.PacketModelSerializer.Write(writer, BaseStats.Value);
        if ((mask & (1UL << 3)) != 0)
            global::GameShared.Packets.PacketModelSerializer.Write(writer, LearnedMartialArt.Value);
    }

    public void Deserialize(BinaryReader reader)
    {
        _mask = reader.ReadUInt64();

        if ((_mask & (1UL << 0)) != 0)
            Success = (bool?)global::GameShared.Packets.PacketReader.ReadBool(reader);
        if ((_mask & (1UL << 1)) != 0)
            Code = (global::GameShared.Messages.MessageCode?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 2)) != 0)
            BaseStats = (global::GameShared.Models.CharacterBaseStatsModel?)global::GameShared.Packets.PacketModelSerializer.Read<global::GameShared.Models.CharacterBaseStatsModel>(reader);
        if ((_mask & (1UL << 3)) != 0)
            LearnedMartialArt = (global::GameShared.Models.PlayerMartialArtModel?)global::GameShared.Packets.PacketModelSerializer.Read<global::GameShared.Models.PlayerMartialArtModel>(reader);
    }
}

public partial class SetActiveMartialArtPacket
{
    private ulong _mask;

    public void Serialize(BinaryWriter writer)
    {
        ulong mask = 0;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(MartialArtId, default!)) mask |= 1UL << 0;

        writer.Write(mask);

        if ((mask & (1UL << 0)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, MartialArtId.Value);
    }

    public void Deserialize(BinaryReader reader)
    {
        _mask = reader.ReadUInt64();

        if ((_mask & (1UL << 0)) != 0)
            MartialArtId = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
    }
}

public partial class SetActiveMartialArtResultPacket
{
    private ulong _mask;

    public void Serialize(BinaryWriter writer)
    {
        ulong mask = 0;
        if (!global::System.Collections.Generic.EqualityComparer<bool?>.Default.Equals(Success, default!)) mask |= 1UL << 0;
        if (!global::System.Collections.Generic.EqualityComparer<global::GameShared.Messages.MessageCode?>.Default.Equals(Code, default!)) mask |= 1UL << 1;
        if (!global::System.Collections.Generic.EqualityComparer<global::GameShared.Models.CharacterBaseStatsModel?>.Default.Equals(BaseStats, default!)) mask |= 1UL << 2;

        writer.Write(mask);

        if ((mask & (1UL << 0)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, Success.Value);
        if ((mask & (1UL << 1)) != 0)
            global::GameShared.Packets.PacketWriter.Write(writer, (int)Code.Value);
        if ((mask & (1UL << 2)) != 0)
            global::GameShared.Packets.PacketModelSerializer.Write(writer, BaseStats.Value);
    }

    public void Deserialize(BinaryReader reader)
    {
        _mask = reader.ReadUInt64();

        if ((_mask & (1UL << 0)) != 0)
            Success = (bool?)global::GameShared.Packets.PacketReader.ReadBool(reader);
        if ((_mask & (1UL << 1)) != 0)
            Code = (global::GameShared.Messages.MessageCode?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 2)) != 0)
            BaseStats = (global::GameShared.Models.CharacterBaseStatsModel?)global::GameShared.Packets.PacketModelSerializer.Read<global::GameShared.Models.CharacterBaseStatsModel>(reader);
    }
}
