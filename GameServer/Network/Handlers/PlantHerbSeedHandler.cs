using GameServer.DTO;
using GameServer.Exceptions;
using GameServer.Network.Interface;
using GameServer.Repositories;
using GameServer.Services;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class PlantHerbSeedHandler : IPacketHandler<PlantHerbSeedPacket>
{
    private readonly HerbService _herbService;
    private readonly PlayerGardenPlotRepository _plots;
    private readonly INetworkSender _network;

    public PlantHerbSeedHandler(HerbService herbService, PlayerGardenPlotRepository plots, INetworkSender network)
    {
        _herbService = herbService;
        _plots = plots;
        _network = network;
    }

    public async Task HandleAsync(ConnectionSession session, PlantHerbSeedPacket packet)
    {
        if (session.Player is null)
        {
            _network.Send(session.ConnectionId, new PlantHerbSeedResultPacket { Success = false, Code = MessageCode.CharacterMustEnterWorld });
            return;
        }

        try
        {
            var playerId = session.Player.CharacterData.CharacterId;
            var herbId = await _herbService.PlantSeedAsync(playerId, packet.SeedPlayerItemId ?? 0, packet.CaveId ?? 0, packet.PlotIndex ?? 0);
            var plot = await _plots.GetByCaveAndPlotIndexAsync(packet.CaveId ?? 0, packet.PlotIndex ?? 0);
            var herb = await _herbService.GetHerbRuntimeStateAsync(herbId);
            var nextRemaining = await _herbService.GetNextStageRemainingSecondsAsync(herbId);
            _network.Send(session.ConnectionId, new PlantHerbSeedResultPacket
            {
                Success = true,
                Code = MessageCode.None,
                PlayerHerbId = herbId,
                Plot = plot is null ? null : plot.ToGardenPlotStateModel(null, herb, null, nextRemaining)
            });
        }
        catch (GameException ex)
        {
            _network.Send(session.ConnectionId, new PlantHerbSeedResultPacket
            {
                Success = false,
                Code = ex.Code,
                PlayerHerbId = null,
                Plot = null
            });
        }
    }
}
