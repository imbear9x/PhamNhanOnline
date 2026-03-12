using GameServer.Exceptions;
using GameServer.Network.Interface;
using GameServer.Services;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class ChangePasswordHandler : IPacketHandler<ChangePasswordPacket>
{
    private readonly AccountService _accountService;
    private readonly INetworkSender _server;

    public ChangePasswordHandler(AccountService accountService, INetworkSender server)
    {
        _accountService = accountService;
        _server = server;
    }

    public async Task HandleAsync(ConnectionSession session, ChangePasswordPacket packet)
    {
        try
        {
            await _accountService.ChangePasswordAsync(session.PlayerId, packet.Password!, packet.NewPassword!);

            _server.Send(session.ConnectionId, new ChangePasswordResultPacket
            {
                Success = true,
                Code = MessageCode.None
            });
        }
        catch (GameException ex)
        {
            _server.Send(session.ConnectionId, new ChangePasswordResultPacket
            {
                Success = false,
                Code = ex.Code
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
