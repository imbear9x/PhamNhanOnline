using GameServer.Exceptions;
using GameServer.Network.Interface;
using GameServer.Services;
using GameShared.Messages;
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
            var result = await _accountService.LoginWithPasswordAsync(packet.Username!, packet.Password!);

            session.PlayerId = result.Account.AccountId;
            session.IsAuthenticated = true;
            var resumeToken = _server.IssueResumeToken(session, result.Account.AccountId);

            var response = new LoginResultPacket
            {
                Success = true,
                Code = MessageCode.None,
                AccountId = result.Account.AccountId,
                ResumeToken = resumeToken
            };

            _server.Send(session.ConnectionId, response);
        }
        catch (GameException ex)
        {
            var response = new LoginResultPacket
            {
                Success = false,
                Code = ex.Code,
                AccountId = Guid.Empty
            };

            _server.Send(session.ConnectionId, response);
        }
        catch (Exception)
        {
            var response = new LoginResultPacket
            {
                Success = false,
                Code = MessageCode.UnknownError,
                AccountId = Guid.Empty
            };

            _server.Send(session.ConnectionId, response);
            throw;
        }
    }
}
