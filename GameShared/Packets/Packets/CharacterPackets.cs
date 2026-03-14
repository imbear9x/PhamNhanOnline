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
