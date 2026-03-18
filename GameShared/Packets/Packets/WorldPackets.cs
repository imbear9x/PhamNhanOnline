using System.ComponentModel.DataAnnotations;
using GameShared.Attributes;
using GameShared.Messages;
using GameShared.Models;

namespace GameShared.Packets;

[Packet]
[PacketTransport(PacketTransportMode.ReliableOrdered, PacketTrafficClass.StateSync)]
public partial class MapJoinedPacket : IPacket
{
    public MapDefinitionModel? Map { get; set; }
    public int? ZoneIndex { get; set; }
}

[Packet]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 200)]
public partial class TravelToMapPacket : IPacket
{
    [ValidationCode(MessageCode.MapIdInvalid)]
    [Required]
    [Range(1, int.MaxValue)]
    public int? TargetMapId { get; set; }
}

[Packet]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class TravelToMapResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public int? TargetMapId { get; set; }
}

[Packet]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 200)]
public partial class GetMapZonesPacket : IPacket
{
    [ValidationCode(MessageCode.MapIdInvalid)]
    [Required]
    [Range(1, int.MaxValue)]
    public int? MapId { get; set; }
}

[Packet]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class GetMapZonesResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public int? MapId { get; set; }
    public int? CurrentZoneIndex { get; set; }
    public int? MaxZoneCount { get; set; }
    public bool? SupportsCavePlacement { get; set; }
    public List<MapZoneSummaryModel>? Zones { get; set; }
}

[Packet]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 200)]
public partial class SwitchMapZonePacket : IPacket
{
    [ValidationCode(MessageCode.MapIdInvalid)]
    [Required]
    [Range(1, int.MaxValue)]
    public int? MapId { get; set; }

    [ValidationCode(MessageCode.MapZoneIndexInvalid)]
    [Required]
    [Range(1, int.MaxValue)]
    public int? TargetZoneIndex { get; set; }
}

[Packet]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class SwitchMapZoneResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public int? MapId { get; set; }
    public int? ZoneIndex { get; set; }
    public MapZoneDetailModel? Zone { get; set; }
}

[Packet]
[RequireAuth]
[PacketTransport(PacketTransportMode.UnreliableSequenced, PacketTrafficClass.RealtimeState, MinIntervalMs = 40)]
public partial class CharacterPositionSyncPacket : IPacket
{
    public float? CurrentPosX { get; set; }
    public float? CurrentPosY { get; set; }
}

[Packet]
[PacketTransport(PacketTransportMode.ReliableOrdered, PacketTrafficClass.StateSync)]
public partial class ObservedCharacterSpawnedPacket : IPacket
{
    public ObservedCharacterModel? Character { get; set; }
}

[Packet]
[PacketTransport(PacketTransportMode.ReliableOrdered, PacketTrafficClass.StateSync)]
public partial class ObservedCharacterDespawnedPacket : IPacket
{
    public Guid? CharacterId { get; set; }
    public int? MapId { get; set; }
    public int? ZoneIndex { get; set; }
}

[Packet]
[PacketTransport(PacketTransportMode.UnreliableSequenced, PacketTrafficClass.RealtimeState)]
public partial class ObservedCharacterMovedPacket : IPacket
{
    public Guid? CharacterId { get; set; }
    public int? MapId { get; set; }
    public int? ZoneIndex { get; set; }
    public float? CurrentPosX { get; set; }
    public float? CurrentPosY { get; set; }
}

[Packet]
[PacketTransport(PacketTransportMode.ReliableSequenced, PacketTrafficClass.StateSync)]
public partial class ObservedCharacterCurrentStateChangedPacket : IPacket
{
    public CharacterCurrentStateModel? CurrentState { get; set; }
    public int? ZoneIndex { get; set; }
}

[Packet]
[PacketTransport(PacketTransportMode.ReliableOrdered, PacketTrafficClass.StateSync)]
public partial class WorldRuntimeSnapshotPacket : IPacket
{
    public int? MapId { get; set; }
    public int? InstanceId { get; set; }
    public int? ZoneIndex { get; set; }
    public int? RuntimeKind { get; set; }
    public long? ExpiresAtUnixMs { get; set; }
    public long? CompletedAtUnixMs { get; set; }
    public List<EnemyRuntimeModel>? Enemies { get; set; }
    public List<GroundRewardModel>? GroundRewards { get; set; }
}

[Packet]
[PacketTransport(PacketTransportMode.ReliableOrdered, PacketTrafficClass.StateSync)]
public partial class EnemySpawnedPacket : IPacket
{
    public int? MapId { get; set; }
    public int? InstanceId { get; set; }
    public EnemyRuntimeModel? Enemy { get; set; }
}

[Packet]
[PacketTransport(PacketTransportMode.ReliableOrdered, PacketTrafficClass.StateSync)]
public partial class EnemyDespawnedPacket : IPacket
{
    public int? MapId { get; set; }
    public int? InstanceId { get; set; }
    public int? EnemyRuntimeId { get; set; }
}

[Packet]
[PacketTransport(PacketTransportMode.ReliableSequenced, PacketTrafficClass.StateSync)]
public partial class EnemyHpChangedPacket : IPacket
{
    public int? MapId { get; set; }
    public int? InstanceId { get; set; }
    public int? EnemyRuntimeId { get; set; }
    public int? CurrentHp { get; set; }
    public int? MaxHp { get; set; }
    public int? RuntimeState { get; set; }
}

[Packet]
[PacketTransport(PacketTransportMode.ReliableOrdered, PacketTrafficClass.StateSync)]
public partial class GroundRewardSpawnedPacket : IPacket
{
    public int? MapId { get; set; }
    public int? InstanceId { get; set; }
    public GroundRewardModel? Reward { get; set; }
}

[Packet]
[PacketTransport(PacketTransportMode.ReliableOrdered, PacketTrafficClass.StateSync)]
public partial class GroundRewardDespawnedPacket : IPacket
{
    public int? MapId { get; set; }
    public int? InstanceId { get; set; }
    public int? RewardId { get; set; }
}

[Packet]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, PacketTrafficClass.Business, MinIntervalMs = 80)]
public partial class AttackEnemyPacket : IPacket
{
    [ValidationCode(MessageCode.EnemyRuntimeIdInvalid)]
    [Required]
    [Range(1, int.MaxValue)]
    public int? EnemyRuntimeId { get; set; }

    [Range(1, int.MaxValue)]
    public int? MartialArtSkillId { get; set; }
}

[Packet]
[PacketTransport(PacketTransportMode.ReliableOrdered, PacketTrafficClass.Business)]
public partial class AttackEnemyResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public int? EnemyRuntimeId { get; set; }
    public int? DamageApplied { get; set; }
    public int? RemainingHp { get; set; }
    public bool? IsKilled { get; set; }
}

[Packet]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, PacketTrafficClass.Business, MinIntervalMs = 80)]
public partial class PickupGroundRewardPacket : IPacket
{
    [ValidationCode(MessageCode.GroundRewardIdInvalid)]
    [Required]
    [Range(1, int.MaxValue)]
    public int? RewardId { get; set; }
}

[Packet]
[PacketTransport(PacketTransportMode.ReliableOrdered, PacketTrafficClass.Business)]
public partial class PickupGroundRewardResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public int? RewardId { get; set; }
    public List<GroundRewardItemModel>? GrantedItems { get; set; }
}

[Packet]
[PacketTransport(PacketTransportMode.ReliableOrdered, PacketTrafficClass.StateSync)]
public partial class MapInstanceClosedPacket : IPacket
{
    public int? ClosedMapId { get; set; }
    public int? ClosedInstanceId { get; set; }
    public int? Reason { get; set; }
    public int? RedirectMapId { get; set; }
    public int? RedirectZoneIndex { get; set; }
    public float? RedirectPosX { get; set; }
    public float? RedirectPosY { get; set; }
}
