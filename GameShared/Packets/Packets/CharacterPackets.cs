using System.ComponentModel.DataAnnotations;
using GameShared.Attributes;
using GameShared.Messages;
using GameShared.Models;

namespace GameShared.Packets;

[Packet]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 1000)]
public partial class CreateCharacterPacket : IPacket
{
    [ValidationCode(MessageCode.CharacterNameInvalid)]
    [Required]
    [StringLength(20, MinimumLength = 3)]
    public string? Name { get; set; }

    [ValidationCode(MessageCode.CharacterServerInvalid)]
    [Required]
    [Range(1, int.MaxValue)]
    public int? ServerId { get; set; }

    [ValidationCode(MessageCode.CharacterModelInvalid)]
    [Required]
    [Range(1, int.MaxValue)]
    public int? ModelId { get; set; }
}

[Packet]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class CreateCharacterResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public CharacterModel? Character { get; set; }
    public CharacterBaseStatsModel? BaseStats { get; set; }
    public CharacterCurrentStateModel? CurrentState { get; set; }
}

[Packet]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 200)]
public partial class GetCharacterListPacket : IPacket
{
}

[Packet]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class GetCharacterListResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public List<CharacterModel>? Characters { get; set; }
}

[Packet]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 200)]
public partial class GetCharacterDataPacket : IPacket
{
    [ValidationCode(MessageCode.CharacterIdInvalid)]
    [Required]
    public Guid? CharacterId { get; set; }
}

[Packet]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class GetCharacterDataResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public CharacterModel? Character { get; set; }
    public CharacterBaseStatsModel? BaseStats { get; set; }
    public CharacterCurrentStateModel? CurrentState { get; set; }
}

[Packet]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 200)]
public partial class EnterWorldPacket : IPacket
{
    [ValidationCode(MessageCode.CharacterIdInvalid)]
    [Required]
    public Guid? CharacterId { get; set; }
}

[Packet]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class EnterWorldResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public CharacterModel? Character { get; set; }
    public CharacterBaseStatsModel? BaseStats { get; set; }
    public CharacterCurrentStateModel? CurrentState { get; set; }
}

[Packet]
[PacketTransport(PacketTransportMode.ReliableOrdered, PacketTrafficClass.StateSync)]
public partial class CharacterBaseStatsChangedPacket : IPacket
{
    public CharacterBaseStatsModel? BaseStats { get; set; }
}

[Packet]
[PacketTransport(PacketTransportMode.ReliableSequenced, PacketTrafficClass.StateSync)]
public partial class CharacterCurrentStateChangedPacket : IPacket
{
    public CharacterCurrentStateModel? CurrentState { get; set; }
}

[Packet]
[PacketTransport(PacketTransportMode.ReliableOrdered, PacketTrafficClass.StateSync)]
public partial class CharacterStateTransitionPacket : IPacket
{
    public Guid? CharacterId { get; set; }
    public int? Reason { get; set; }
}

[Packet]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 500)]
public partial class StartCultivationPacket : IPacket
{
}

[Packet]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class StartCultivationResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public CharacterCurrentStateModel? CurrentState { get; set; }
}

[Packet]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 500)]
public partial class StopCultivationPacket : IPacket
{
}

[Packet]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class StopCultivationResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public CharacterCurrentStateModel? CurrentState { get; set; }
}

[Packet]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 500)]
public partial class BreakthroughPacket : IPacket
{
}

[Packet]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class BreakthroughResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public CharacterBaseStatsModel? BaseStats { get; set; }
    public CharacterCurrentStateModel? CurrentState { get; set; }
}

[Packet]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 200)]
public partial class AllocatePotentialPacket : IPacket
{
    [ValidationCode(MessageCode.PotentialTargetInvalid)]
    [Range(1, int.MaxValue)]
    public int? TargetStat { get; set; }

    [ValidationCode(MessageCode.PotentialAllocationInvalid)]
    [Range(1, int.MaxValue)]
    public int? RequestedPotentialAmount { get; set; }
}

[Packet]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class AllocatePotentialResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public CharacterBaseStatsModel? BaseStats { get; set; }
    public CharacterCurrentStateModel? CurrentState { get; set; }
    public int? RequestedPotentialAmount { get; set; }
    public int? SpentPotentialAmount { get; set; }
    public int? AppliedUpgradeCount { get; set; }
}

[Packet]
[PacketTransport(PacketTransportMode.ReliableOrdered, PacketTrafficClass.StateSync)]
public partial class CultivationRewardsGrantedPacket : IPacket
{
    public Guid? CharacterId { get; set; }
    public long? CultivationGranted { get; set; }
    public int? UnallocatedPotentialGranted { get; set; }
    public bool? ReachedRealmCap { get; set; }
    public bool? IsOfflineSettlement { get; set; }
    public long? RewardedFromUnixMs { get; set; }
    public long? RewardedToUnixMs { get; set; }
}

[Packet]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 200)]
public partial class GetOwnedMartialArtsPacket : IPacket
{
}

[Packet]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class GetOwnedMartialArtsResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public List<PlayerMartialArtModel>? MartialArts { get; set; }
    public int? ActiveMartialArtId { get; set; }
}

[Packet]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 300)]
public partial class UseMartialArtBookPacket : IPacket
{
    [ValidationCode(MessageCode.MartialArtBookItemInvalid)]
    [Required]
    [Range(1, long.MaxValue)]
    public long? PlayerItemId { get; set; }
}

[Packet]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class UseMartialArtBookResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public CharacterBaseStatsModel? BaseStats { get; set; }
    public PlayerMartialArtModel? LearnedMartialArt { get; set; }
}

[Packet]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 300)]
public partial class SetActiveMartialArtPacket : IPacket
{
    [ValidationCode(MessageCode.ActiveMartialArtInvalid)]
    [Required]
    [Range(1, int.MaxValue)]
    public int? MartialArtId { get; set; }
}

[Packet]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class SetActiveMartialArtResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public CharacterBaseStatsModel? BaseStats { get; set; }
}

[Packet]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 200)]
public partial class GetInventoryPacket : IPacket
{
}

[Packet]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class GetInventoryResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public List<InventoryItemModel>? Items { get; set; }
}
