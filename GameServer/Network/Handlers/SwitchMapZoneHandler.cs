using GameServer.DTO;
using GameServer.Network.Interface;
using GameServer.Runtime;
using GameServer.World;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class SwitchMapZoneHandler : IPacketHandler<SwitchMapZonePacket>
{
    private readonly CharacterRuntimeService _runtimeService;
    private readonly CharacterCultivationService _cultivationService;
    private readonly WorldInterestService _interestService;
    private readonly MapManager _mapManager;
    private readonly INetworkSender _server;
    private readonly MapCatalog _mapCatalog;

    public SwitchMapZoneHandler(
        CharacterRuntimeService runtimeService,
        CharacterCultivationService cultivationService,
        WorldInterestService interestService,
        MapManager mapManager,
        INetworkSender server,
        MapCatalog mapCatalog)
    {
        _runtimeService = runtimeService;
        _cultivationService = cultivationService;
        _interestService = interestService;
        _mapManager = mapManager;
        _server = server;
        _mapCatalog = mapCatalog;
    }

    public Task HandleAsync(ConnectionSession session, SwitchMapZonePacket packet)
    {
        if (session.Player is null || !packet.MapId.HasValue || !packet.TargetZoneIndex.HasValue)
        {
            _server.Send(session.ConnectionId, new SwitchMapZoneResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterMustEnterWorld,
                MapId = packet.MapId,
                ZoneIndex = packet.TargetZoneIndex
            });
            return Task.CompletedTask;
        }

        var player = session.Player;
        var currentState = player.RuntimeState.CaptureSnapshot().CurrentState.CurrentState;
        if (player.IsStunned(DateTime.UtcNow))
        {
            _server.Send(session.ConnectionId, new SwitchMapZoneResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterCannotActWhileStunned,
                MapId = packet.MapId,
                ZoneIndex = packet.TargetZoneIndex
            });
            return Task.CompletedTask;
        }

        if (_cultivationService.IsCultivating(player))
        {
            _server.Send(session.ConnectionId, new SwitchMapZoneResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterCannotMoveWhileCultivating,
                MapId = packet.MapId,
                ZoneIndex = packet.TargetZoneIndex
            });
            return Task.CompletedTask;
        }

        if (currentState == CharacterRuntimeStateCodes.Casting)
        {
            _server.Send(session.ConnectionId, new SwitchMapZoneResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterCannotActWhileCasting,
                MapId = packet.MapId,
                ZoneIndex = packet.TargetZoneIndex
            });
            return Task.CompletedTask;
        }

        if (!_mapCatalog.TryGet(packet.MapId.Value, out var definition))
        {
            _server.Send(session.ConnectionId, new SwitchMapZoneResultPacket
            {
                Success = false,
                Code = MessageCode.MapIdInvalid,
                MapId = packet.MapId,
                ZoneIndex = packet.TargetZoneIndex
            });
            return Task.CompletedTask;
        }

        if (player.MapId != definition.MapId || definition.IsPrivatePerPlayer || !definition.SupportsCavePlacement || definition.MaxPublicZoneCount <= 0)
        {
            _server.Send(session.ConnectionId, new SwitchMapZoneResultPacket
            {
                Success = false,
                Code = MessageCode.MapZoneSelectionNotSupported,
                MapId = definition.MapId,
                ZoneIndex = packet.TargetZoneIndex
            });
            return Task.CompletedTask;
        }

        var targetZoneIndex = packet.TargetZoneIndex.Value;
        if (!_mapCatalog.TryGetZoneSlot(definition.MapId, targetZoneIndex, out _))
        {
            _server.Send(session.ConnectionId, new SwitchMapZoneResultPacket
            {
                Success = false,
                Code = MessageCode.MapZoneIndexInvalid,
                MapId = definition.MapId,
                ZoneIndex = targetZoneIndex
            });
            return Task.CompletedTask;
        }

        var playerCounts = _mapManager.GetActivePlayerCountsByZone(definition.MapId);
        playerCounts.TryGetValue(targetZoneIndex, out var currentPlayerCount);
        if (player.ZoneIndex != targetZoneIndex && currentPlayerCount >= definition.MaxPlayersPerZone)
        {
            _server.Send(session.ConnectionId, new SwitchMapZoneResultPacket
            {
                Success = false,
                Code = MessageCode.MapZoneFull,
                MapId = definition.MapId,
                ZoneIndex = targetZoneIndex
            });
            return Task.CompletedTask;
        }

        var entryContext = new MapEntryContext(
            MapEntryReason.DefaultSpawn,
            PortalId: null,
            SpawnPointId: null,
            definition.DefaultSpawnPosition);
        _runtimeService.UpdatePosition(player, definition.MapId, targetZoneIndex, definition.DefaultSpawnPosition);
        player.SetMapEntryContext(entryContext);
        _interestService.PublishWorldSnapshot(player);

        _mapCatalog.TryGetZoneSlot(definition.MapId, targetZoneIndex, out var zoneSlot);
        _server.Send(session.ConnectionId, new SwitchMapZoneResultPacket
        {
            Success = true,
            Code = MessageCode.None,
            MapId = definition.MapId,
            ZoneIndex = targetZoneIndex,
            Zone = zoneSlot?.ToDetailModel()
        });
        return Task.CompletedTask;
    }
}
