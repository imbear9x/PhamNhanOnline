using GameServer.Exceptions;
using GameServer.Network.Interface;
using GameServer.Services;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class ChangePasswordHandler : IPacketHandler<ChangePasswordPacket>
{
    private readonly AccountActionService _accountActionService;
    private readonly INetworkSender _server;

    public ChangePasswordHandler(AccountActionService accountActionService, INetworkSender server)
    {
        _accountActionService = accountActionService;
        _server = server;
    }

    public async Task HandleAsync(ConnectionSession session, ChangePasswordPacket packet)
    {
        try
        {
            var result = await _accountActionService.ChangePasswordAsync(
                session.PlayerId,
                packet.Password!,
                packet.NewPassword!);

            _server.Send(session.ConnectionId, new ChangePasswordResultPacket
            {
                Success = result.Success,
                Code = result.Code
            });
        }
        catch (Exception)
        {
            _server.Send(session.ConnectionId, new ChangePasswordResultPacket
            {
                Success = false,
                Code = MessageCode.UnknownError
            });
            throw;
        }
    }
}
