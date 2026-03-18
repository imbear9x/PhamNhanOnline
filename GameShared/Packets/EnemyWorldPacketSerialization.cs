using System.IO;

namespace GameShared.Packets;

public partial class WorldRuntimeSnapshotPacket
{
    private ulong _mask;

    public void Serialize(BinaryWriter writer)
    {
        ulong mask = 0;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(MapId, default!)) mask |= 1UL << 0;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(InstanceId, default!)) mask |= 1UL << 1;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(ZoneIndex, default!)) mask |= 1UL << 2;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(RuntimeKind, default!)) mask |= 1UL << 3;
        if (!global::System.Collections.Generic.EqualityComparer<long?>.Default.Equals(ExpiresAtUnixMs, default!)) mask |= 1UL << 4;
        if (!global::System.Collections.Generic.EqualityComparer<long?>.Default.Equals(CompletedAtUnixMs, default!)) mask |= 1UL << 5;
        if (!global::System.Collections.Generic.EqualityComparer<global::System.Collections.Generic.List<global::GameShared.Models.EnemyRuntimeModel>?>.Default.Equals(Enemies, default!)) mask |= 1UL << 6;
        if (!global::System.Collections.Generic.EqualityComparer<global::System.Collections.Generic.List<global::GameShared.Models.GroundRewardModel>?>.Default.Equals(GroundRewards, default!)) mask |= 1UL << 7;

        writer.Write(mask);

        if ((mask & (1UL << 0)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, MapId.Value);
        if ((mask & (1UL << 1)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, InstanceId.Value);
        if ((mask & (1UL << 2)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, ZoneIndex.Value);
        if ((mask & (1UL << 3)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, RuntimeKind.Value);
        if ((mask & (1UL << 4)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, ExpiresAtUnixMs.Value);
        if ((mask & (1UL << 5)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, CompletedAtUnixMs.Value);
        if ((mask & (1UL << 6)) != 0) global::GameShared.Packets.PacketModelSerializer.WriteList(writer, Enemies);
        if ((mask & (1UL << 7)) != 0) global::GameShared.Packets.PacketModelSerializer.WriteList(writer, GroundRewards);
    }

    public void Deserialize(BinaryReader reader)
    {
        _mask = reader.ReadUInt64();

        if ((_mask & (1UL << 0)) != 0) MapId = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 1)) != 0) InstanceId = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 2)) != 0) ZoneIndex = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 3)) != 0) RuntimeKind = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 4)) != 0) ExpiresAtUnixMs = (long?)global::GameShared.Packets.PacketReader.ReadLong(reader);
        if ((_mask & (1UL << 5)) != 0) CompletedAtUnixMs = (long?)global::GameShared.Packets.PacketReader.ReadLong(reader);
        if ((_mask & (1UL << 6)) != 0) Enemies = global::GameShared.Packets.PacketModelSerializer.ReadList<global::GameShared.Models.EnemyRuntimeModel>(reader);
        if ((_mask & (1UL << 7)) != 0) GroundRewards = global::GameShared.Packets.PacketModelSerializer.ReadList<global::GameShared.Models.GroundRewardModel>(reader);
    }
}

public partial class EnemySpawnedPacket
{
    private ulong _mask;

    public void Serialize(BinaryWriter writer)
    {
        ulong mask = 0;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(MapId, default!)) mask |= 1UL << 0;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(InstanceId, default!)) mask |= 1UL << 1;
        if (!global::System.Collections.Generic.EqualityComparer<global::GameShared.Models.EnemyRuntimeModel?>.Default.Equals(Enemy, default!)) mask |= 1UL << 2;

        writer.Write(mask);

        if ((mask & (1UL << 0)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, MapId.Value);
        if ((mask & (1UL << 1)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, InstanceId.Value);
        if ((mask & (1UL << 2)) != 0) global::GameShared.Packets.PacketModelSerializer.Write(writer, Enemy.Value);
    }

    public void Deserialize(BinaryReader reader)
    {
        _mask = reader.ReadUInt64();

        if ((_mask & (1UL << 0)) != 0) MapId = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 1)) != 0) InstanceId = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 2)) != 0) Enemy = (global::GameShared.Models.EnemyRuntimeModel?)global::GameShared.Packets.PacketModelSerializer.Read<global::GameShared.Models.EnemyRuntimeModel>(reader);
    }
}

public partial class EnemyDespawnedPacket
{
    private ulong _mask;

    public void Serialize(BinaryWriter writer)
    {
        ulong mask = 0;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(MapId, default!)) mask |= 1UL << 0;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(InstanceId, default!)) mask |= 1UL << 1;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(EnemyRuntimeId, default!)) mask |= 1UL << 2;

        writer.Write(mask);

        if ((mask & (1UL << 0)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, MapId.Value);
        if ((mask & (1UL << 1)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, InstanceId.Value);
        if ((mask & (1UL << 2)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, EnemyRuntimeId.Value);
    }

    public void Deserialize(BinaryReader reader)
    {
        _mask = reader.ReadUInt64();

        if ((_mask & (1UL << 0)) != 0) MapId = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 1)) != 0) InstanceId = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 2)) != 0) EnemyRuntimeId = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
    }
}

public partial class EnemyHpChangedPacket
{
    private ulong _mask;

    public void Serialize(BinaryWriter writer)
    {
        ulong mask = 0;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(MapId, default!)) mask |= 1UL << 0;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(InstanceId, default!)) mask |= 1UL << 1;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(EnemyRuntimeId, default!)) mask |= 1UL << 2;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(CurrentHp, default!)) mask |= 1UL << 3;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(MaxHp, default!)) mask |= 1UL << 4;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(RuntimeState, default!)) mask |= 1UL << 5;

        writer.Write(mask);

        if ((mask & (1UL << 0)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, MapId.Value);
        if ((mask & (1UL << 1)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, InstanceId.Value);
        if ((mask & (1UL << 2)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, EnemyRuntimeId.Value);
        if ((mask & (1UL << 3)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, CurrentHp.Value);
        if ((mask & (1UL << 4)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, MaxHp.Value);
        if ((mask & (1UL << 5)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, RuntimeState.Value);
    }

    public void Deserialize(BinaryReader reader)
    {
        _mask = reader.ReadUInt64();

        if ((_mask & (1UL << 0)) != 0) MapId = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 1)) != 0) InstanceId = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 2)) != 0) EnemyRuntimeId = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 3)) != 0) CurrentHp = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 4)) != 0) MaxHp = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 5)) != 0) RuntimeState = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
    }
}

public partial class GroundRewardSpawnedPacket
{
    private ulong _mask;

    public void Serialize(BinaryWriter writer)
    {
        ulong mask = 0;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(MapId, default!)) mask |= 1UL << 0;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(InstanceId, default!)) mask |= 1UL << 1;
        if (!global::System.Collections.Generic.EqualityComparer<global::GameShared.Models.GroundRewardModel?>.Default.Equals(Reward, default!)) mask |= 1UL << 2;

        writer.Write(mask);

        if ((mask & (1UL << 0)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, MapId.Value);
        if ((mask & (1UL << 1)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, InstanceId.Value);
        if ((mask & (1UL << 2)) != 0) global::GameShared.Packets.PacketModelSerializer.Write(writer, Reward.Value);
    }

    public void Deserialize(BinaryReader reader)
    {
        _mask = reader.ReadUInt64();

        if ((_mask & (1UL << 0)) != 0) MapId = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 1)) != 0) InstanceId = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 2)) != 0) Reward = (global::GameShared.Models.GroundRewardModel?)global::GameShared.Packets.PacketModelSerializer.Read<global::GameShared.Models.GroundRewardModel>(reader);
    }
}

public partial class GroundRewardDespawnedPacket
{
    private ulong _mask;

    public void Serialize(BinaryWriter writer)
    {
        ulong mask = 0;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(MapId, default!)) mask |= 1UL << 0;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(InstanceId, default!)) mask |= 1UL << 1;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(RewardId, default!)) mask |= 1UL << 2;

        writer.Write(mask);

        if ((mask & (1UL << 0)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, MapId.Value);
        if ((mask & (1UL << 1)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, InstanceId.Value);
        if ((mask & (1UL << 2)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, RewardId.Value);
    }

    public void Deserialize(BinaryReader reader)
    {
        _mask = reader.ReadUInt64();

        if ((_mask & (1UL << 0)) != 0) MapId = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 1)) != 0) InstanceId = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 2)) != 0) RewardId = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
    }
}

public partial class AttackEnemyPacket
{
    private ulong _mask;

    public void Serialize(BinaryWriter writer)
    {
        ulong mask = 0;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(EnemyRuntimeId, default!)) mask |= 1UL << 0;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(MartialArtSkillId, default!)) mask |= 1UL << 1;

        writer.Write(mask);

        if ((mask & (1UL << 0)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, EnemyRuntimeId.Value);
        if ((mask & (1UL << 1)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, MartialArtSkillId.Value);
    }

    public void Deserialize(BinaryReader reader)
    {
        _mask = reader.ReadUInt64();

        if ((_mask & (1UL << 0)) != 0) EnemyRuntimeId = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 1)) != 0) MartialArtSkillId = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
    }
}

public partial class AttackEnemyResultPacket
{
    private ulong _mask;

    public void Serialize(BinaryWriter writer)
    {
        ulong mask = 0;
        if (!global::System.Collections.Generic.EqualityComparer<bool?>.Default.Equals(Success, default!)) mask |= 1UL << 0;
        if (!global::System.Collections.Generic.EqualityComparer<global::GameShared.Messages.MessageCode?>.Default.Equals(Code, default!)) mask |= 1UL << 1;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(EnemyRuntimeId, default!)) mask |= 1UL << 2;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(DamageApplied, default!)) mask |= 1UL << 3;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(RemainingHp, default!)) mask |= 1UL << 4;
        if (!global::System.Collections.Generic.EqualityComparer<bool?>.Default.Equals(IsKilled, default!)) mask |= 1UL << 5;

        writer.Write(mask);

        if ((mask & (1UL << 0)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, Success.Value);
        if ((mask & (1UL << 1)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, (int)Code.Value);
        if ((mask & (1UL << 2)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, EnemyRuntimeId.Value);
        if ((mask & (1UL << 3)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, DamageApplied.Value);
        if ((mask & (1UL << 4)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, RemainingHp.Value);
        if ((mask & (1UL << 5)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, IsKilled.Value);
    }

    public void Deserialize(BinaryReader reader)
    {
        _mask = reader.ReadUInt64();

        if ((_mask & (1UL << 0)) != 0) Success = (bool?)global::GameShared.Packets.PacketReader.ReadBool(reader);
        if ((_mask & (1UL << 1)) != 0) Code = (global::GameShared.Messages.MessageCode?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 2)) != 0) EnemyRuntimeId = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 3)) != 0) DamageApplied = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 4)) != 0) RemainingHp = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 5)) != 0) IsKilled = (bool?)global::GameShared.Packets.PacketReader.ReadBool(reader);
    }
}

public partial class PickupGroundRewardPacket
{
    private ulong _mask;

    public void Serialize(BinaryWriter writer)
    {
        ulong mask = 0;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(RewardId, default!)) mask |= 1UL << 0;

        writer.Write(mask);

        if ((mask & (1UL << 0)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, RewardId.Value);
    }

    public void Deserialize(BinaryReader reader)
    {
        _mask = reader.ReadUInt64();

        if ((_mask & (1UL << 0)) != 0) RewardId = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
    }
}

public partial class PickupGroundRewardResultPacket
{
    private ulong _mask;

    public void Serialize(BinaryWriter writer)
    {
        ulong mask = 0;
        if (!global::System.Collections.Generic.EqualityComparer<bool?>.Default.Equals(Success, default!)) mask |= 1UL << 0;
        if (!global::System.Collections.Generic.EqualityComparer<global::GameShared.Messages.MessageCode?>.Default.Equals(Code, default!)) mask |= 1UL << 1;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(RewardId, default!)) mask |= 1UL << 2;
        if (!global::System.Collections.Generic.EqualityComparer<global::System.Collections.Generic.List<global::GameShared.Models.GroundRewardItemModel>?>.Default.Equals(GrantedItems, default!)) mask |= 1UL << 3;

        writer.Write(mask);

        if ((mask & (1UL << 0)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, Success.Value);
        if ((mask & (1UL << 1)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, (int)Code.Value);
        if ((mask & (1UL << 2)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, RewardId.Value);
        if ((mask & (1UL << 3)) != 0) global::GameShared.Packets.PacketModelSerializer.WriteList(writer, GrantedItems);
    }

    public void Deserialize(BinaryReader reader)
    {
        _mask = reader.ReadUInt64();

        if ((_mask & (1UL << 0)) != 0) Success = (bool?)global::GameShared.Packets.PacketReader.ReadBool(reader);
        if ((_mask & (1UL << 1)) != 0) Code = (global::GameShared.Messages.MessageCode?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 2)) != 0) RewardId = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 3)) != 0) GrantedItems = global::GameShared.Packets.PacketModelSerializer.ReadList<global::GameShared.Models.GroundRewardItemModel>(reader);
    }
}

public partial class MapInstanceClosedPacket
{
    private ulong _mask;

    public void Serialize(BinaryWriter writer)
    {
        ulong mask = 0;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(ClosedMapId, default!)) mask |= 1UL << 0;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(ClosedInstanceId, default!)) mask |= 1UL << 1;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(Reason, default!)) mask |= 1UL << 2;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(RedirectMapId, default!)) mask |= 1UL << 3;
        if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(RedirectZoneIndex, default!)) mask |= 1UL << 4;
        if (!global::System.Collections.Generic.EqualityComparer<float?>.Default.Equals(RedirectPosX, default!)) mask |= 1UL << 5;
        if (!global::System.Collections.Generic.EqualityComparer<float?>.Default.Equals(RedirectPosY, default!)) mask |= 1UL << 6;

        writer.Write(mask);

        if ((mask & (1UL << 0)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, ClosedMapId.Value);
        if ((mask & (1UL << 1)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, ClosedInstanceId.Value);
        if ((mask & (1UL << 2)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, Reason.Value);
        if ((mask & (1UL << 3)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, RedirectMapId.Value);
        if ((mask & (1UL << 4)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, RedirectZoneIndex.Value);
        if ((mask & (1UL << 5)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, RedirectPosX.Value);
        if ((mask & (1UL << 6)) != 0) global::GameShared.Packets.PacketWriter.Write(writer, RedirectPosY.Value);
    }

    public void Deserialize(BinaryReader reader)
    {
        _mask = reader.ReadUInt64();

        if ((_mask & (1UL << 0)) != 0) ClosedMapId = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 1)) != 0) ClosedInstanceId = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 2)) != 0) Reason = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 3)) != 0) RedirectMapId = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 4)) != 0) RedirectZoneIndex = (int?)global::GameShared.Packets.PacketReader.ReadInt(reader);
        if ((_mask & (1UL << 5)) != 0) RedirectPosX = (float?)global::GameShared.Packets.PacketReader.ReadFloat(reader);
        if ((_mask & (1UL << 6)) != 0) RedirectPosY = (float?)global::GameShared.Packets.PacketReader.ReadFloat(reader);
    }
}
