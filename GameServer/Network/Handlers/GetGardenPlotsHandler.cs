using GameServer.DTO;
using GameServer.Exceptions;
using GameServer.Network.Interface;
using GameServer.Repositories;
using GameServer.Runtime;
using GameServer.Services;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class GetGardenPlotsHandler : IPacketHandler<GetGardenPlotsPacket>
{
    private readonly HerbService _herbService;
    private readonly PlayerGardenPlotRepository _plots;
    private readonly PlayerSoilRepository _soils;
    private readonly INetworkSender _network;

    public GetGardenPlotsHandler(HerbService herbService, PlayerGardenPlotRepository plots, PlayerSoilRepository soils, INetworkSender network)
    {
        _herbService = herbService;
        _plots = plots;
        _soils = soils;
        _network = network;
    }

    public async Task HandleAsync(ConnectionSession session, GetGardenPlotsPacket packet)
    {
        if (session.Player is null)
        {
            _network.Send(session.ConnectionId, new GetGardenPlotsResultPacket { Success = false, Code = MessageCode.CharacterMustEnterWorld, CaveId = packet.CaveId });
            return;
        }

        try
        {
            var playerId = session.Player.CharacterData.CharacterId;
            var caveId = packet.CaveId ?? 0;
            var plotEntities = await _herbService.GetGardenPlotsAsync(playerId, caveId);
            var models = new List<GameShared.Models.GardenPlotStateModel>(plotEntities.Count);
            foreach (var plot in plotEntities)
            {
                var herbState = plot.CurrentPlayerHerbId.HasValue
                    ? await _herbService.GetHerbRuntimeStateAsync(plot.CurrentPlayerHerbId.Value)
                    : null;
                var soil = plot.CurrentSoilPlayerItemId.HasValue
                    ? await _soils.GetByPlayerItemIdAsync(plot.CurrentSoilPlayerItemId.Value)
                    : null;
                var nextRemaining = plot.CurrentPlayerHerbId.HasValue
                    ? await _herbService.GetNextStageRemainingSecondsAsync(plot.CurrentPlayerHerbId.Value)
                    : 0;
                models.Add(plot.ToGardenPlotStateModel(soil, herbState, soil?.State, nextRemaining));
            }

            _network.Send(session.ConnectionId, new GetGardenPlotsResultPacket
            {
                Success = true,
                Code = MessageCode.None,
                CaveId = caveId,
                Plots = models
            });
        }
        catch (GameException ex)
        {
            _network.Send(session.ConnectionId, new GetGardenPlotsResultPacket
            {
                Success = false,
                Code = ex.Code,
                CaveId = packet.CaveId,
                Plots = new List<GameShared.Models.GardenPlotStateModel>()
            });
        }
    }
}
