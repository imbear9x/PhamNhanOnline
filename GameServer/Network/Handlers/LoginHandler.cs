using GameServer.Network.Interface;
using GameServer.Services;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class LoginHandler : IPacketHandler<LoginPacket>
{
    private readonly AccountService _accountService;
    private readonly INetworkSender _server;

    public LoginHandler(AccountService accountService, INetworkSender server)
    {
        _accountService = accountService;
        _server = server;
    }

    public async Task HandleAsync(ConnectionSession session, LoginPacket packet)
    {
        try
        {
            var result = await _accountService.LoginWithPasswordAsync(packet.Username, packet.Password);

            session.PlayerId = result.Account.AccountId;

            var response = new LoginResultPacket
            {
                Success = true,
                Error = string.Empty,
                AccountId = result.Account.AccountId
            };

            _server.Send(session.ConnectionId, response);
        }
        catch (Exception ex)
        {
            var response = new LoginResultPacket
            {
                Success = false,
                Error = ex.Message,
                AccountId = Guid.Empty
            };

            _server.Send(session.ConnectionId, response);
        }
    }
}
