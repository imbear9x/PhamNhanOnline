using GameShared.Attributes;
using GameShared.Models;

namespace GameShared.Packets;

[Packet]
[PacketTransport(PacketTransportMode.ReliableOrdered, PacketTrafficClass.StateSync)]
public partial class MapJoinedPacket : IPacket
{
    public MapDefinitionModel? Map { get; set; }
    public int? InstanceId { get; set; }
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
    public int? InstanceId { get; set; }
}

[Packet]
[PacketTransport(PacketTransportMode.UnreliableSequenced, PacketTrafficClass.RealtimeState)]
public partial class ObservedCharacterMovedPacket : IPacket
{
    public Guid? CharacterId { get; set; }
    public int? MapId { get; set; }
    public int? InstanceId { get; set; }
    public float? CurrentPosX { get; set; }
    public float? CurrentPosY { get; set; }
}

[Packet]
[PacketTransport(PacketTransportMode.ReliableSequenced, PacketTrafficClass.StateSync)]
public partial class ObservedCharacterCurrentStateChangedPacket : IPacket
{
    public CharacterCurrentStateModel? CurrentState { get; set; }
    public int? InstanceId { get; set; }
}
