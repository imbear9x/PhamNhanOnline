using GameServer.Config;
using GameServer.Network.Interface;
using GameServer.Runtime;
using GameServer.World;
using GameShared.Messages;
using GameShared.Packets;
using GameShared.Logging;

namespace GameServer.Network.Handlers;

public sealed class TravelToMapHandler : IPacketHandler<TravelToMapPacket>
{
    private readonly CharacterRuntimeService _runtimeService;
    private readonly GameConfigValues _gameConfig;
    private readonly WorldInterestService _interestService;
    private readonly INetworkSender _server;
    private readonly MapCatalog _mapCatalog;
    private readonly MapManager _mapManager;
    private readonly WorldInteractionGate _interactionGate;

    public TravelToMapHandler(
        CharacterRuntimeService runtimeService,
        GameConfigValues gameConfig,
        WorldInterestService interestService,
        INetworkSender server,
        MapCatalog mapCatalog,
        MapManager mapManager,
        WorldInteractionGate interactionGate)
    {
        _runtimeService = runtimeService;
        _gameConfig = gameConfig;
        _interestService = interestService;
        _server = server;
        _mapCatalog = mapCatalog;
        _mapManager = mapManager;
        _interactionGate = interactionGate;
    }

    public Task HandleAsync(ConnectionSession session, TravelToMapPacket packet)
    {
        if (session.Player == null)
        {
            SendFailure(session, packet, MessageCode.CharacterNotFound, null, null);
            return Task.CompletedTask;
        }

        var player = session.Player;
        var startGateResult = _interactionGate.CheckPlayerCanStartAction(
            player,
            WorldInteractionActionKind.PortalTravel,
            "TravelToMap",
            DateTime.UtcNow);
        if (!startGateResult.Success)
        {
            if (startGateResult.SuppressFailure)
                return Task.CompletedTask;

            SendFailure(session, packet, startGateResult.Code, null, null);
            return Task.CompletedTask;
        }

        if (packet.PortalId.HasValue)
            return HandlePortalTravelAsync(session, player, packet);

        return HandleLegacyMapTravelAsync(session, player, packet);
    }

    private async Task HandlePortalTravelAsync(ConnectionSession session, PlayerSession player, TravelToMapPacket packet)
    {
        var portalId = packet.PortalId!.Value;
        Logger.Info($"[PortalTravel] request conn={session.ConnectionId} player={player.CharacterData.Name} characterId={player.CharacterData.CharacterId} map={player.MapId} zone={player.ZoneIndex} portal={portalId} packetPos=({packet.CurrentPosX?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "<null>"},{packet.CurrentPosY?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "<null>"}) playerPos=({player.Position.X.ToString(System.Globalization.CultureInfo.InvariantCulture)},{player.Position.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)}).");
        if (!_mapCatalog.TryGetPortal(player.MapId, portalId, out var portal) || !portal.IsEnabled)
        {
            Logger.Info($"[PortalTravel] reject invalid portal conn={session.ConnectionId} player={player.CharacterData.Name} map={player.MapId} portal={portalId}.");
            SendFailure(session, packet, MessageCode.MapPortalInvalid, null, null);
            return;
        }

        if (!_mapCatalog.TryGet(portal.TargetMapId, out var targetDefinition))
        {
            SendFailure(session, packet, MessageCode.MapIdInvalid, portal.TargetMapId, portal.TargetSpawnPointId);
            return;
        }

        if (!targetDefinition.TryGetSpawnPoint(portal.TargetSpawnPointId, out var targetSpawnPoint))
        {
            SendFailure(session, packet, MessageCode.MapPortalInvalid, portal.TargetMapId, portal.TargetSpawnPointId);
            return;
        }

        if (!_mapManager.TryGetInstance(player.MapId, player.InstanceId, out var instance))
        {
            SendFailure(session, packet, MessageCode.CharacterNotInWorldInstance, portal.TargetMapId, portal.TargetSpawnPointId);
            return;
        }

        var maxDistance = MathF.Max(0f, portal.InteractionRadius) + MathF.Max(0f, _gameConfig.WorldPortalValidationBufferServerUnits);
        var gateResult = await _interactionGate.PrepareAsync(new WorldInteractionGateRequest(
            player,
            instance,
            WorldTargetRef.Portal(portal.Id),
            maxDistance,
            WorldInteractionActionKind.PortalTravel,
            "PortalTravel",
            MessageCode.MapTravelNotAllowed));
        if (!gateResult.Success)
        {
            if (gateResult.SuppressFailure)
                return;

            Logger.Info(
                $"[PortalTravel] reject by interaction gate conn={session.ConnectionId} " +
                $"player={player.CharacterData.Name} portal={portal.Id} code={gateResult.Code}.");
            SendFailure(session, packet, gateResult.Code, portal.TargetMapId, portal.TargetSpawnPointId);
            return;
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
