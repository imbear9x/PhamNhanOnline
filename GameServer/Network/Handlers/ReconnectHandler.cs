using GameServer.Network.Interface;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class ReconnectHandler : IPacketHandler<ReconnectPacket>
{
    private readonly INetworkSender _server;

    public ReconnectHandler(INetworkSender server)
    {
        _server = server;
    }

    public Task HandleAsync(ConnectionSession session, ReconnectPacket packet)
    {
        if (_server.TryResumeSession(session, packet.ResumeToken!, out var accountId, out var errorCode))
        {
            _server.Send(session.ConnectionId, new ReconnectResultPacket
            {
                Success = true,
                Code = MessageCode.None,
                AccountId = accountId,
                ResumeToken = packet.ResumeToken
            });
            return Task.CompletedTask;
        }

        _server.Send(session.ConnectionId, new ReconnectResultPacket
        {
            Success = false,
            Code = errorCode,
            AccountId = Guid.Empty
        });
        return Task.CompletedTask;
    }
}
