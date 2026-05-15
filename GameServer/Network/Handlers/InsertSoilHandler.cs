using GameServer.DTO;
using GameServer.Exceptions;
using GameServer.Network.Interface;
using GameServer.Repositories;
using GameServer.Services;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class InsertSoilHandler : IPacketHandler<InsertSoilPacket>
{
    private readonly HerbService _herbService;
    private readonly PlayerGardenPlotRepository _plots;
    private readonly PlayerSoilRepository _soils;
    private readonly INetworkSender _network;

    public InsertSoilHandler(HerbService herbService, PlayerGardenPlotRepository plots, PlayerSoilRepository soils, INetworkSender network)
    {
        _herbService = herbService;
        _plots = plots;
        _soils = soils;
        _network = network;
    }

    public async Task HandleAsync(ConnectionSession session, InsertSoilPacket packet)
    {
        if (session.Player is null)
        {
            _network.Send(session.ConnectionId, new InsertSoilResultPacket { Success = false, Code = MessageCode.CharacterMustEnterWorld });
            return;
        }

        try
        {
            var playerId = session.Player.CharacterData.CharacterId;
            await _herbService.InsertSoilAsync(playerId, packet.SoilPlayerItemId ?? 0, packet.CaveId ?? 0, packet.PlotIndex ?? 0);
            var plot = await _plots.GetByCaveAndPlotIndexAsync(packet.CaveId ?? 0, packet.PlotIndex ?? 0);
            var soil = plot?.CurrentSoilPlayerItemId is { } soilPlayerItemId ? await _soils.GetByPlayerItemIdAsync(soilPlayerItemId) : null;
            _network.Send(session.ConnectionId, new InsertSoilResultPacket
            {
                Success = true,
                Code = MessageCode.None,
                Plot = plot is null ? null : plot.ToGardenPlotStateModel(soil, null, soil?.State, 0)
            });
        }
        catch (GameException ex)
        {
            _network.Send(session.ConnectionId, new InsertSoilResultPacket
            {
                Success = false,
                Code = ex.Code,
                Plot = null
            });
        }
    }
}
