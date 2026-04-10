using GameServer.DTO;
using GameServer.Network.Interface;
using GameServer.World;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class GetMapZonesHandler : IPacketHandler<GetMapZonesPacket>
{
    private readonly INetworkSender _server;
    private readonly MapCatalog _mapCatalog;
    private readonly MapManager _mapManager;

    public GetMapZonesHandler(
        INetworkSender server,
        MapCatalog mapCatalog,
        MapManager mapManager)
    {
        _server = server;
        _mapCatalog = mapCatalog;
        _mapManager = mapManager;
    }

    public Task HandleAsync(ConnectionSession session, GetMapZonesPacket packet)
    {
        if (session.Player is null || !packet.MapId.HasValue)
        {
            _server.Send(session.ConnectionId, new GetMapZonesResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterMustEnterWorld,
                MapId = packet.MapId
            });
            return Task.CompletedTask;
        }

        if (!_mapCatalog.TryGet(packet.MapId.Value, out var definition))
        {
            _server.Send(session.ConnectionId, new GetMapZonesResultPacket
            {
                Success = false,
                Code = MessageCode.MapIdInvalid,
                MapId = packet.MapId
            });
            return Task.CompletedTask;
        }

        if (definition.IsPrivatePerPlayer || definition.MaxPublicZoneCount <= 0)
        {
            _server.Send(session.ConnectionId, new GetMapZonesResultPacket
            {
                Success = false,
                Code = MessageCode.MapZoneSelectionNotSupported,
                MapId = definition.MapId
            });
            return Task.CompletedTask;
        }

        var playerCounts = _mapManager.GetActivePlayerCountsByZone(definition.MapId);
        var zones = _mapCatalog.GetZoneSlots(definition.MapId)
            .Select(zone =>
            {
                playerCounts.TryGetValue(zone.ZoneIndex, out var currentPlayerCount);
                return zone.ToSummaryModel(currentPlayerCount, definition.MaxPlayersPerZone);
            })
            .ToList();

        _server.Send(session.ConnectionId, new GetMapZonesResultPacket
        {
            Success = true,
            Code = MessageCode.None,
            MapId = definition.MapId,
            CurrentZoneIndex = ResolveCurrentZoneIndex(session.Player, definition.MapId),
            MaxZoneCount = definition.MaxPublicZoneCount,
            SupportsCavePlacement = definition.SupportsCavePlacement,
            Zones = zones
        });
        return Task.CompletedTask;
    }
    private int? ResolveCurrentZoneIndex(PlayerSession player, int requestedMapId)
    {
        if (player.MapId != requestedMapId)
            return null;

        if (_mapManager.TryGetInstance(player.MapId, player.InstanceId, out var instance) &&
            instance.ContainsPlayer(player.PlayerId))
        {
            return instance.ZoneIndex;
        }

        if (_mapManager.TryGetInstanceContainingPlayer(player.MapId, player.PlayerId, out var fallbackInstance))
            return fallbackInstance.ZoneIndex;

        return null;
    }
}
