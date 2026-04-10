using GameServer.Network.Interface;
using GameServer.Services;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class AcknowledgePracticeResultHandler : IPacketHandler<AcknowledgePracticeResultPacket>
{
    private readonly PracticeService _practiceService;
    private readonly INetworkSender _network;

    public AcknowledgePracticeResultHandler(PracticeService practiceService, INetworkSender network)
    {
        _practiceService = practiceService;
        _network = network;
    }

    public async Task HandleAsync(ConnectionSession session, AcknowledgePracticeResultPacket packet)
    {
        var result = await _practiceService.AcknowledgeResultAsync(session, packet.PracticeSessionId);
        _network.Send(session.ConnectionId, new AcknowledgePracticeResultResultPacket
        {
            Success = result.Success,
            Code = result.Code
        });
    }
}
