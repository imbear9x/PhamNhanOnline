using GameServer.Exceptions;
using GameServer.Network.Interface;
using GameServer.Services;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class RegisterHandler : IPacketHandler<RegisterPacket>
{
    private readonly AccountActionService _accountActionService;
    private readonly INetworkSender _server;

    public RegisterHandler(AccountActionService accountActionService, INetworkSender server)
    {
        _accountActionService = accountActionService;
        _server         = server;
    }

    public async Task HandleAsync(ConnectionSession session, RegisterPacket packet)
    {
        try
        {
            var result = await _accountActionService.RegisterAsync(packet.Username!, packet.Password!);
            _server.Send(session.ConnectionId, new RegisterResultPacket
            {
                Success = result.Success,
                Code = result.Code
            });
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
