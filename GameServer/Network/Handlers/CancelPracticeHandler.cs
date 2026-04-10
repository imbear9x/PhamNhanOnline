using GameServer.Network.Interface;
using GameServer.Services;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class CancelPracticeHandler : IPacketHandler<CancelPracticePacket>
{
    private readonly PracticeService _practiceService;
    private readonly INetworkSender _network;

    public CancelPracticeHandler(PracticeService practiceService, INetworkSender network)
    {
        _practiceService = practiceService;
        _network = network;
    }

    public async Task HandleAsync(ConnectionSession session, CancelPracticePacket packet)
    {
        var result = await _practiceService.CancelAsync(session, packet.PracticeSessionId);
        _network.Send(session.ConnectionId, new CancelPracticeResultPacket
        {
            Success = result.Success,
            Code = result.Code
        });
    }
}
