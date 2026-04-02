using System.Numerics;
using GameServer.Network.Interface;
using GameServer.Runtime;
using GameServer.World;
using GameShared.Messages;
using GameShared.Packets;
using GameShared.Logging;

namespace GameServer.Network.Handlers;

public sealed class TravelToMapHandler : IPacketHandler<TravelToMapPacket>
{
    private const float PortalValidationBufferServerUnits = 4f;

    private readonly CharacterRuntimeService _runtimeService;
    private readonly CharacterCultivationService _cultivationService;
    private readonly WorldInterestService _interestService;
    private readonly INetworkSender _server;
    private readonly MapCatalog _mapCatalog;
    private readonly MapManager _mapManager;

    public TravelToMapHandler(
        CharacterRuntimeService runtimeService,
        CharacterCultivationService cultivationService,
        WorldInterestService interestService,
        INetworkSender server,
        MapCatalog mapCatalog,
        MapManager mapManager)
    {
        _runtimeService = runtimeService;
        _cultivationService = cultivationService;
        _interestService = interestService;
        _server = server;
        _mapCatalog = mapCatalog;
        _mapManager = mapManager;
    }

    public Task HandleAsync(ConnectionSession session, TravelToMapPacket packet)
    {
        if (session.Player == null)
        {
            SendFailure(session, packet, MessageCode.CharacterNotFound, null, null);
            return Task.CompletedTask;
        }

        var player = session.Player;
        var currentState = player.RuntimeState.CaptureSnapshot().CurrentState.CurrentState;
        if (player.IsStunned(DateTime.UtcNow))
        {
            SendFailure(session, packet, MessageCode.CharacterCannotActWhileStunned, null, null);
            return Task.CompletedTask;
        }

        if (_cultivationService.IsCultivating(player))
        {
            SendFailure(session, packet, MessageCode.CharacterCannotMoveWhileCultivating, null, null);
            return Task.CompletedTask;
        }

        if (currentState == CharacterRuntimeStateCodes.Casting)
        {
            SendFailure(session, packet, MessageCode.CharacterCannotActWhileCasting, null, null);
            return Task.CompletedTask;
        }

        if (packet.PortalId.HasValue)
            return HandlePortalTravelAsync(session, player, packet);

        return HandleLegacyMapTravelAsync(session, player, packet);
    }

    private Task HandlePortalTravelAsync(ConnectionSession session, PlayerSession player, TravelToMapPacket packet)
    {
        var portalId = packet.PortalId!.Value;
        Logger.Info($"[PortalTravel] request conn={session.ConnectionId} player={player.CharacterData.Name} characterId={player.CharacterData.CharacterId} map={player.MapId} zone={player.ZoneIndex} portal={portalId} packetPos=({packet.CurrentPosX?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "<null>"},{packet.CurrentPosY?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "<null>"}) playerPos=({player.Position.X.ToString(System.Globalization.CultureInfo.InvariantCulture)},{player.Position.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)}).");
        if (!_mapCatalog.TryGetPortal(player.MapId, portalId, out var portal) || !portal.IsEnabled)
        {
            Logger.Info($"[PortalTravel] reject invalid portal conn={session.ConnectionId} player={player.CharacterData.Name} map={player.MapId} portal={portalId}.");
            SendFailure(session, packet, MessageCode.MapPortalInvalid, null, null);
            return Task.CompletedTask;
        }

        if (!_mapCatalog.TryGet(portal.TargetMapId, out var targetDefinition))
        {
            SendFailure(session, packet, MessageCode.MapIdInvalid, portal.TargetMapId, portal.TargetSpawnPointId);
            return Task.CompletedTask;
        }

        if (!targetDefinition.TryGetSpawnPoint(portal.TargetSpawnPointId, out var targetSpawnPoint))
        {
            SendFailure(session, packet, MessageCode.MapPortalInvalid, portal.TargetMapId, portal.TargetSpawnPointId);
            return Task.CompletedTask;
        }

        var validationPosition = ResolvePortalValidationPosition(player, packet);
        var maxDistance = MathF.Max(0f, portal.InteractionRadius) + PortalValidationBufferServerUnits;
        var distanceSquared = Vector2.DistanceSquared(validationPosition, portal.SourcePosition);
        if (distanceSquared > maxDistance * maxDistance)
        {
            Logger.Info($"[PortalTravel] reject out-of-range conn={session.ConnectionId} player={player.CharacterData.Name} portal={portal.Id} validationPos=({validationPosition.X.ToString(System.Globalization.CultureInfo.InvariantCulture)},{validationPosition.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)}) portalPos=({portal.SourcePosition.X.ToString(System.Globalization.CultureInfo.InvariantCulture)},{portal.SourcePosition.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)}) distance={MathF.Sqrt(distanceSquared).ToString(System.Globalization.CultureInfo.InvariantCulture)} maxDistance={maxDistance.ToString(System.Globalization.CultureInfo.InvariantCulture)} packetPos=({packet.CurrentPosX?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "<null>"},{packet.CurrentPosY?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "<null>"}).");
            SendFailure(session, packet, MessageCode.MapTravelNotAllowed, portal.TargetMapId, portal.TargetSpawnPointId);
            return Task.CompletedTask;
        }

        var targetZoneIndex = targetDefinition.IsPrivatePerPlayer
            ? 0
            : _mapManager.ResolveAutoJoinZone(targetDefinition);
        var entryPosition = targetDefinition.ResolveSpawnPosition(targetSpawnPoint.Id);
        var entryContext = new MapEntryContext(
            MapEntryReason.Portal,
            portal.Id,
            targetSpawnPoint.Id,
            entryPosition);

        _runtimeService.UpdatePosition(player, targetDefinition.MapId, targetZoneIndex, entryPosition);
        player.SetMapEntryContext(entryContext);
        _interestService.PublishWorldSnapshot(player);

        Logger.Info($"[PortalTravel] success conn={session.ConnectionId} player={player.CharacterData.Name} portal={portal.Id} targetMap={targetDefinition.MapId} spawn={targetSpawnPoint.Id} targetZone={targetZoneIndex}.");
        _server.Send(session.ConnectionId, new TravelToMapResultPacket
        {
            Success = true,
            Code = MessageCode.None,
            TargetMapId = targetDefinition.MapId,
            PortalId = portal.Id,
            TargetSpawnPointId = targetSpawnPoint.Id
        });
        return Task.CompletedTask;
    }

    private Vector2 ResolvePortalValidationPosition(PlayerSession player, TravelToMapPacket packet)
    {
        if (packet.CurrentPosX.HasValue && packet.CurrentPosY.HasValue && _mapCatalog.TryGet(player.MapId, out var currentMap))
        {
            return currentMap.ClampPosition(new Vector2(packet.CurrentPosX.Value, packet.CurrentPosY.Value));
        }

        return player.Position;
    }

    private Task HandleLegacyMapTravelAsync(ConnectionSession session, PlayerSession player, TravelToMapPacket packet)
    {
        if (!packet.TargetMapId.HasValue)
        {
            SendFailure(session, packet, MessageCode.MapIdInvalid, null, null);
            return Task.CompletedTask;
        }

        var targetMapId = packet.TargetMapId.Value;
        if (!_mapCatalog.TryGet(targetMapId, out var targetDefinition))
        {
            SendFailure(session, packet, MessageCode.MapIdInvalid, targetMapId, null);
            return Task.CompletedTask;
        }

        if (!_mapCatalog.CanTravel(player.MapId, targetMapId))
        {
            SendFailure(session, packet, MessageCode.MapTravelNotAllowed, targetMapId, null);
            return Task.CompletedTask;
        }

        var targetZoneIndex = targetDefinition.IsPrivatePerPlayer
            ? 0
            : _mapManager.ResolveAutoJoinZone(targetDefinition);
        var entryPosition = targetDefinition.DefaultSpawnPosition;
        var entryContext = new MapEntryContext(
            MapEntryReason.DefaultSpawn,
            PortalId: null,
            SpawnPointId: null,
            entryPosition);

        _runtimeService.UpdatePosition(player, targetMapId, targetZoneIndex, entryPosition);
        player.SetMapEntryContext(entryContext);
        _interestService.PublishWorldSnapshot(player);

        _server.Send(session.ConnectionId, new TravelToMapResultPacket
        {
            Success = true,
            Code = MessageCode.None,
            TargetMapId = targetMapId,
            PortalId = null,
            TargetSpawnPointId = null
        });
        return Task.CompletedTask;
    }

    private void SendFailure(
        ConnectionSession session,
        TravelToMapPacket packet,
        MessageCode code,
        int? resolvedTargetMapId,
        int? resolvedTargetSpawnPointId)
    {
        _server.Send(session.ConnectionId, new TravelToMapResultPacket
        {
            Success = false,
            Code = code,
            TargetMapId = resolvedTargetMapId ?? packet.TargetMapId,
            PortalId = packet.PortalId,
            TargetSpawnPointId = resolvedTargetSpawnPointId
        });
    }
}
