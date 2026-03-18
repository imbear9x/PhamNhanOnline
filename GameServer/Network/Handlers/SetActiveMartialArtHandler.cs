using GameServer.DTO;
using GameServer.Exceptions;
using GameServer.Network.Interface;
using GameServer.Runtime;
using GameServer.Services;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class SetActiveMartialArtHandler : IPacketHandler<SetActiveMartialArtPacket>
{
    private readonly MartialArtService _martialArtService;
    private readonly CharacterRuntimeNotifier _notifier;
    private readonly INetworkSender _network;

    public SetActiveMartialArtHandler(
        MartialArtService martialArtService,
        CharacterRuntimeNotifier notifier,
        INetworkSender network)
    {
        _martialArtService = martialArtService;
        _notifier = notifier;
        _network = network;
    }

    public async Task HandleAsync(ConnectionSession session, SetActiveMartialArtPacket packet)
    {
        if (session.Player is null)
        {
            _network.Send(session.ConnectionId, new SetActiveMartialArtResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterMustEnterWorld
            });
            return;
        }

        try
        {
            var updatedBaseStats = await _martialArtService.SetActiveMartialArtAsync(
                session.Player.CharacterData.CharacterId,
                packet.MartialArtId!.Value);

            session.Player.RuntimeState.UpdateBaseStats(_ => updatedBaseStats);
            _notifier.NotifyBaseStatsChanged(session.Player, updatedBaseStats);

            _network.Send(session.ConnectionId, new SetActiveMartialArtResultPacket
            {
                Success = true,
                Code = MessageCode.None,
                BaseStats = updatedBaseStats.ToModel()
            });
        }
        catch (GameException ex)
        {
            _network.Send(session.ConnectionId, new SetActiveMartialArtResultPacket
            {
                Success = false,
                Code = ex.Code
            });
        }
    }
}
