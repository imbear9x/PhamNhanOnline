using GameShared.Attributes;
using GameShared.Messages;

namespace GameShared.Packets;

[Packet(70)]
[PacketTransport(PacketTransportMode.ReliableOrdered, PacketTrafficClass.Business)]
public partial class SessionTerminationPacket : IPacket
{
    public MessageCode? Code { get; set; }
    public string? Message { get; set; }
}
