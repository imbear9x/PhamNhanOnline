using GameServer.Network.Interface;
using GameServer.Services;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class ResumePracticeHandler : IPacketHandler<ResumePracticePacket>
{
    private readonly PracticeService _practiceService;
    private readonly INetworkSender _network;

    public ResumePracticeHandler(PracticeService practiceService, INetworkSender network)
    {
        _practiceService = practiceService;
        _network = network;
    }

    public async Task HandleAsync(ConnectionSession session, ResumePracticePacket packet)
    {
        var result = await _practiceService.ResumeAsync(session, packet.PracticeSessionId);
        _network.Send(session.ConnectionId, new ResumePracticeResultPacket
        {
            Success = result.Success,
            Code = result.Code,
            Session = result.Session is not null
                ? _practiceService.BuildSessionModel(result.Session, DateTime.UtcNow)
                : null
        });
    }
}
