using GameServer.Network.Interface;
using GameServer.Services;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class AcknowledgePlayerNotificationHandler : IPacketHandler<AcknowledgePlayerNotificationPacket>
{
    private readonly PlayerNotificationService _notificationService;
    private readonly INetworkSender _network;

    public AcknowledgePlayerNotificationHandler(
        PlayerNotificationService notificationService,
        INetworkSender network)
    {
        _notificationService = notificationService;
        _network = network;
    }

    public async Task HandleAsync(ConnectionSession session, AcknowledgePlayerNotificationPacket packet)
    {
        var result = await _notificationService.AcknowledgeAsync(session, packet.NotificationId);
        _network.Send(session.ConnectionId, new AcknowledgePlayerNotificationResultPacket
        {
            Success = result.Success,
            Code = result.Code,
            NotificationId = result.NotificationId
        });
    }
}
