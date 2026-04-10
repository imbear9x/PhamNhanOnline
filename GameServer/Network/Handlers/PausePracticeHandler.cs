using GameServer.Network.Interface;
using GameServer.Services;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class PausePracticeHandler : IPacketHandler<PausePracticePacket>
{
    private readonly PracticeService _practiceService;
    private readonly INetworkSender _network;

    public PausePracticeHandler(PracticeService practiceService, INetworkSender network)
    {
        _practiceService = practiceService;
        _network = network;
    }

    public async Task HandleAsync(ConnectionSession session, PausePracticePacket packet)
    {
        var result = await _practiceService.PauseAsync(session, packet.PracticeSessionId);
        _network.Send(session.ConnectionId, new PausePracticeResultPacket
        {
            Success = result.Success,
            Code = result.Code,
            Session = result.Session is not null
                ? _practiceService.BuildSessionModel(result.Session, DateTime.UtcNow)
                : null
        });
    }
}
