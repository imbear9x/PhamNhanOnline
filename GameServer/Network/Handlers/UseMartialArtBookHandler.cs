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
    private readonly CharacterCultivationService _cultivationService;
    private readonly CharacterRuntimeNotifier _notifier;
    private readonly INetworkSender _network;

    public UseMartialArtBookHandler(
        MartialArtService martialArtService,
        CharacterCultivationService cultivationService,
        CharacterRuntimeNotifier notifier,
        INetworkSender network)
    {
        _martialArtService = martialArtService;
        _cultivationService = cultivationService;
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
            var cultivationPreview = await _cultivationService.BuildCultivationPreviewAsync(result.BaseStats);

            _network.Send(session.ConnectionId, new UseMartialArtBookResultPacket
            {
                Success = true,
                Code = MessageCode.None,
                BaseStats = result.BaseStats.ToModel(),
                LearnedMartialArt = result.LearnedMartialArt.ToModel(),
                CultivationPreview = cultivationPreview?.ToModel()
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
