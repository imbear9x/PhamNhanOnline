using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Interface;

public interface INetworkSender
{
    void Send(int clientId, IPacket packet);
    string IssueResumeToken(ConnectionSession session, Guid accountId);
    bool TryResumeSession(ConnectionSession session, string resumeToken, out Guid accountId, out MessageCode errorCode);
}
