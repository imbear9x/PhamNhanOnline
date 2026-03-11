using GameServer.Network.Interface;
using GameServer.Services;
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
            var a = packet.HasEmail;
            // Email is accepted at the network layer; AccountService currently uses username/password.
            await _accountService.RegisterWithPasswordAsync(packet.Username, packet.Password);

            var response = new RegisterResultPacket
            {
                Success = true,
                Error   = string.Empty
            };

            _server.Send(session.ConnectionId, response);
        }
        catch (Exception ex)
        {
            var response = new RegisterResultPacket
            {
                Success = false,
                Error   = ex.Message
            };

            _server.Send(session.ConnectionId, response);
        }
    }
}
