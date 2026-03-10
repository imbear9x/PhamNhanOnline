using GameServer.Network.Packets;

namespace GameServer.Network;

public interface INetworkSender
{
    void Send(int clientId, IPacket packet);
}