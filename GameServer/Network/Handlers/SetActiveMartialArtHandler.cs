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
    private readonly CharacterFinalStatService _characterFinalStatService;
    private readonly CharacterCultivationService _cultivationService;
    private readonly INetworkSender _network;

    public SetActiveMartialArtHandler(
        MartialArtService martialArtService,
        CharacterFinalStatService characterFinalStatService,
        CharacterCultivationService cultivationService,
        INetworkSender network)
    {
        _martialArtService = martialArtService;
        _characterFinalStatService = characterFinalStatService;
        _cultivationService = cultivationService;
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
            var requestedMartialArtId = packet.MartialArtId!.Value;
            var currentSnapshot = session.Player.RuntimeState.CaptureSnapshot();
            if (currentSnapshot.CurrentState.CurrentState == CharacterRuntimeStateCodes.Cultivating ||
                currentSnapshot.CurrentState.CurrentState == CharacterRuntimeStateCodes.Practicing)
            {
                _network.Send(session.ConnectionId, new SetActiveMartialArtResultPacket
                {
                    Success = false,
                    Code = MessageCode.PracticeAlreadyActive
                });
                return;
            }

            var updatedBaseStats = await _martialArtService.SetActiveMartialArtAsync(
                session.Player.CharacterData.CharacterId,
                requestedMartialArtId);

            session.Player.RuntimeState.UpdateBaseStats(_ => updatedBaseStats);
            var runtimeSnapshot = await _characterFinalStatService.ApplyAuthoritativeFinalStatsAsync(session.Player);
            var cultivationPreview = await _cultivationService.BuildCultivationPreviewAsync(runtimeSnapshot.BaseStats);

            _network.Send(session.ConnectionId, new SetActiveMartialArtResultPacket
            {
                Success = true,
                Code = MessageCode.None,
                BaseStats = runtimeSnapshot.BaseStats.ToModel(),
                CultivationPreview = cultivationPreview?.ToModel()
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
