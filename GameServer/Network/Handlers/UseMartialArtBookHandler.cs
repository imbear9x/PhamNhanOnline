using GameServer.DTO;
using GameServer.Exceptions;
using GameServer.Network.Interface;
using GameServer.Runtime;
using GameServer.Services;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class UseMartialArtBookHandler : IPacketHandler<UseMartialArtBookPacket>
{
    private readonly MartialArtService _martialArtService;
    private readonly CharacterRuntimeNotifier _notifier;
    private readonly INetworkSender _network;

    public UseMartialArtBookHandler(
        MartialArtService martialArtService,
        CharacterRuntimeNotifier notifier,
        INetworkSender network)
    {
        _martialArtService = martialArtService;
        _notifier = notifier;
        _network = network;
    }

    public async Task HandleAsync(ConnectionSession session, UseMartialArtBookPacket packet)
    {
        if (session.Player is null)
        {
            _network.Send(session.ConnectionId, new UseMartialArtBookResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterMustEnterWorld
            });
            return;
        }

        try
        {
            var result = await _martialArtService.UseMartialArtBookAsync(session.Player.CharacterData.CharacterId, packet.PlayerItemId!.Value);
            session.Player.RuntimeState.UpdateBaseStats(_ => result.BaseStats);
            _notifier.NotifyBaseStatsChanged(session.Player, result.BaseStats);

            _network.Send(session.ConnectionId, new UseMartialArtBookResultPacket
            {
                Success = true,
                Code = MessageCode.None,
                BaseStats = result.BaseStats.ToModel(),
                LearnedMartialArt = result.LearnedMartialArt.ToModel()
            });
        }
        catch (GameException ex)
        {
            _network.Send(session.ConnectionId, new UseMartialArtBookResultPacket
            {
                Success = false,
                Code = ex.Code
            });
        }
    }
}
