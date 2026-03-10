namespace GameServer.Network.Packets;

public interface IPacket
{
    PacketType PacketType { get; }
}

