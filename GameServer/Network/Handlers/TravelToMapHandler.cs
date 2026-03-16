using GameServer.Network.Interface;
using GameServer.Runtime;
using GameServer.World;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class TravelToMapHandler : IPacketHandler<TravelToMapPacket>
{
    private readonly CharacterRuntimeService _runtimeService;
    private readonly WorldInterestService _interestService;
    private readonly INetworkSender _server;
    private readonly MapCatalog _mapCatalog;

    public TravelToMapHandler(
        CharacterRuntimeService runtimeService,
        WorldInterestService interestService,
        INetworkSender server,
        MapCatalog mapCatalog)
    {
        _runtimeService = runtimeService;
        _interestService = interestService;
        _server = server;
        _mapCatalog = mapCatalog;
    }

    public Task HandleAsync(ConnectionSession session, TravelToMapPacket packet)
    {
        if (session.Player == null || !packet.TargetMapId.HasValue)
        {
            _server.Send(session.ConnectionId, new TravelToMapResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterNotFound,
                TargetMapId = packet.TargetMapId
            });
            return Task.CompletedTask;
        }

        var player = session.Player;
        var targetMapId = packet.TargetMapId.Value;
        if (!_mapCatalog.TryGet(targetMapId, out var targetDefinition))
        {
            _server.Send(session.ConnectionId, new TravelToMapResultPacket
            {
                Success = false,
                Code = MessageCode.MapIdInvalid,
                TargetMapId = targetMapId
            });
            return Task.CompletedTask;
        }

        if (!_mapCatalog.CanTravel(player.MapId, targetMapId))
        {
            _server.Send(session.ConnectionId, new TravelToMapResultPacket
            {
                Success = false,
                Code = MessageCode.MapTravelNotAllowed,
                TargetMapId = targetMapId
            });
            return Task.CompletedTask;
        }

        _runtimeService.UpdatePosition(player, targetMapId, targetDefinition.DefaultZoneIndex, targetDefinition.DefaultSpawnPosition);
        _interestService.PublishWorldSnapshot(player);
        _server.Send(session.ConnectionId, new TravelToMapResultPacket
        {
            Success = true,
            Code = MessageCode.None,
            TargetMapId = targetMapId
        });
        return Task.CompletedTask;
    }
}
