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
    private readonly MartialArtActionService _martialArtActionService;
    private readonly INetworkSender _network;

    public SetActiveMartialArtHandler(
        MartialArtActionService martialArtActionService,
        INetworkSender network)
    {
        _martialArtActionService = martialArtActionService;
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
            var execution = await _martialArtActionService.SetActiveMartialArtAsync(
                session,
                packet.MartialArtId!.Value);

            _network.Send(session.ConnectionId, new SetActiveMartialArtResultPacket
            {
                Success = true,
                Code = MessageCode.None,
                BaseStats = execution.BaseStats.ToModel(),
                CultivationPreview = execution.CultivationPreview?.ToModel()
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
