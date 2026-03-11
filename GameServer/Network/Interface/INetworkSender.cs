
using GameShared.Packets;

namespace GameServer.Network.Interface;

public interface INetworkSender
{
    void Send(int clientId, IPacket packet);
}