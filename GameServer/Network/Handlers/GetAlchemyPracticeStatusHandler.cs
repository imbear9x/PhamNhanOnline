using GameServer.Network.Interface;
using GameServer.Services;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class GetAlchemyPracticeStatusHandler : IPacketHandler<GetAlchemyPracticeStatusPacket>
{
    private readonly AlchemyPracticeService _alchemyPracticeService;
    private readonly INetworkSender _network;

    public GetAlchemyPracticeStatusHandler(
        AlchemyPracticeService alchemyPracticeService,
        INetworkSender network)
    {
        _alchemyPracticeService = alchemyPracticeService;
        _network = network;
    }

    public async Task HandleAsync(ConnectionSession session, GetAlchemyPracticeStatusPacket packet)
    {
        if (session.Player is null)
        {
            _network.Send(session.ConnectionId, new GetAlchemyPracticeStatusResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterMustEnterWorld
            });
            return;
        }

        var status = await _alchemyPracticeService.GetStatusAsync(session.Player.CharacterData.CharacterId);
        _network.Send(session.ConnectionId, new GetAlchemyPracticeStatusResultPacket
        {
            Success = true,
            Code = MessageCode.None,
            Status = status
        });
    }
}
