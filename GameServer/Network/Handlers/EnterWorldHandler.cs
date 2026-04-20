using GameServer.DTO;
using GameServer.Network.Interface;
using GameServer.Runtime;
using GameServer.Services;
using GameServer.Time;
using GameServer.World;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class EnterWorldHandler : IPacketHandler<EnterWorldPacket>
{
    private readonly WorldEntryService _worldEntryService;
    private readonly CharacterLifecycleService _lifecycleService;
    private readonly PlayerNotificationService _notificationService;
    private readonly WorldInterestService _interestService;
    private readonly INetworkSender _server;

    public EnterWorldHandler(
        WorldEntryService worldEntryService,
        CharacterLifecycleService lifecycleService,
        PlayerNotificationService notificationService,
        WorldInterestService interestService,
        INetworkSender server)
    {
        _worldEntryService = worldEntryService;
        _lifecycleService = lifecycleService;
        _notificationService = notificationService;
        _interestService = interestService;
        _server = server;
    }

    public async Task HandleAsync(ConnectionSession session, EnterWorldPacket packet)
    {
        try
        {
            var result = await _worldEntryService.EnterAsync(session, packet.CharacterId!.Value);
            if (!result.Success || result.Player is null || result.Character is null || result.BaseStats is null || result.CurrentState is null)
            {
                _server.Send(session.ConnectionId, new EnterWorldResultPacket
                {
                    Success = false,
                    Code = result.Code
                });
                return;
            }

            _server.Send(session.ConnectionId, new EnterWorldResultPacket
            {
                Success = true,
                Code = result.Code,
                Character = result.Character,
                BaseStats = result.BaseStats,
                CurrentState = result.CurrentState
            });

            _interestService.PublishWorldSnapshot(result.Player);

            if (result.RewardPacket is not null)
                _server.Send(session.ConnectionId, result.RewardPacket);

            await _notificationService.PushUnreadAsync(session);

            if (result.NotifyLifespanExpired)
                _lifecycleService.NotifyLifespanExpired(session.ConnectionId, result.Player.CharacterData.CharacterId);
        }
        catch (Exception)
        {
            _server.Send(session.ConnectionId, new EnterWorldResultPacket
            {
                Success = false,
                Code = MessageCode.UnknownError
            });
            throw;
        }
    }
}
