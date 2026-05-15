using GameServer.DTO;
using GameServer.Exceptions;
using GameServer.Network.Interface;
using GameServer.Repositories;
using GameServer.Services;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class PlantExistingHerbHandler : IPacketHandler<PlantExistingHerbPacket>
{
    private readonly HerbService _herbService;
    private readonly PlayerGardenPlotRepository _plots;
    private readonly INetworkSender _network;

    public PlantExistingHerbHandler(HerbService herbService, PlayerGardenPlotRepository plots, INetworkSender network)
    {
        _herbService = herbService;
        _plots = plots;
        _network = network;
    }

    public async Task HandleAsync(ConnectionSession session, PlantExistingHerbPacket packet)
    {
        if (session.Player is null)
        {
            _network.Send(session.ConnectionId, new PlantExistingHerbResultPacket { Success = false, Code = MessageCode.CharacterMustEnterWorld });
            return;
        }

        try
        {
            var playerId = session.Player.CharacterData.CharacterId;
            await _herbService.PlantExistingHerbAsync(playerId, packet.PlayerHerbId ?? 0, packet.CaveId ?? 0, packet.PlotIndex ?? 0);
            var plot = await _plots.GetByCaveAndPlotIndexAsync(packet.CaveId ?? 0, packet.PlotIndex ?? 0);
            var herb = plot?.CurrentPlayerHerbId is { } herbId ? await _herbService.GetHerbRuntimeStateAsync(herbId) : null;
            var nextRemaining = plot?.CurrentPlayerHerbId is { } plantedHerbId
                ? await _herbService.GetNextStageRemainingSecondsAsync(plantedHerbId)
                : 0;
            _network.Send(session.ConnectionId, new PlantExistingHerbResultPacket
            {
                Success = true,
                Code = MessageCode.None,
                Plot = plot is null ? null : plot.ToGardenPlotStateModel(null, herb, null, nextRemaining)
            });
        }
        catch (GameException ex)
        {
            _network.Send(session.ConnectionId, new PlantExistingHerbResultPacket
            {
                Success = false,
                Code = ex.Code,
                Plot = null
            });
        }
    }
}
