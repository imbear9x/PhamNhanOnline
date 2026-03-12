using GameServer.Exceptions;
using GameServer.Network.Interface;
using GameServer.Services;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class RegisterHandler : IPacketHandler<RegisterPacket>
{
    private readonly AccountService _accountService;
    private readonly INetworkSender _server;

    public RegisterHandler(AccountService accountService, INetworkSender server)
    {
        _accountService = accountService;
        _server         = server;
    }

    public async Task HandleAsync(ConnectionSession session, RegisterPacket packet)
    {
        try
        {
            // Email is accepted at the network layer; AccountService currently uses username/password.
            await _accountService.RegisterWithPasswordAsync(packet.Username!, packet.Password!);

            var response = new RegisterResultPacket
            {
                Success = true,
                Code = MessageCode.None
            };

            _server.Send(session.ConnectionId, response);
        }
        catch (GameException ex)
        {
            var response = new RegisterResultPacket
            {
                Success = false,
                Code = ex.Code
            };

            _server.Send(session.ConnectionId, response);
        }
        catch (Exception)
        {
            var response = new RegisterResultPacket
            {
                Success = false,
                Code = MessageCode.UnknownError
            };

            _server.Send(session.ConnectionId, response);
            throw;
        }
    }
}
