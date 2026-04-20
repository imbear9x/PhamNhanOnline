using GameServer.Exceptions;
using GameServer.Network.Interface;
using GameServer.Services;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class LoginHandler : IPacketHandler<LoginPacket>
{
    private readonly AccountActionService _accountActionService;
    private readonly INetworkSender _server;

    public LoginHandler(AccountActionService accountActionService, INetworkSender server)
    {
        _accountActionService = accountActionService;
        _server = server;
    }

    public async Task HandleAsync(ConnectionSession session, LoginPacket packet)
    {
        try
        {
            var result = await _accountActionService.LoginAsync(packet.Username!, packet.Password!);
            if (!result.Success || result.Login is null)
            {
                _server.Send(session.ConnectionId, new LoginResultPacket
                {
                    Success = false,
                    Code = result.Code,
                    AccountId = Guid.Empty
                });
                return;
            }

            session.PlayerId = result.Login.Account.AccountId;
            session.IsAuthenticated = true;
            var resumeToken = _server.IssueResumeToken(session, result.Login.Account.AccountId);

            var response = new LoginResultPacket
            {
                Success = true,
                Code = MessageCode.None,
                AccountId = result.Login.Account.AccountId,
                ResumeToken = resumeToken
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
