using GameServer.DTO;
using GameServer.Network.Interface;
using GameServer.Time;
using GameShared.Packets;

namespace GameServer.World;

public sealed class WorldInterestService
{
    private readonly WorldManager _worldManager;
    private readonly INetworkSender _network;
    private readonly GameTimeService _gameTimeService;

    public WorldInterestService(
        WorldManager worldManager,
        INetworkSender network,
        GameTimeService gameTimeService)
    {
        _worldManager = worldManager;
        _network = network;
        _gameTimeService = gameTimeService;
        _worldManager.PlayerRemoving += HandlePlayerRemoving;
    }

    public MapDefinition EnsurePlayerInWorld(
        PlayerSession player,
        int? requestedZoneIndex = null,
        bool autoSelectPublicZone = false)
    {
        var definition = _worldManager.MapManager.ResolveDefinitionOrDefault(player.MapId == 0 ? null : player.MapId);
        var targetPosition = ResolveEntryPosition(player, definition);

        var instance = _worldManager.MapManager.JoinInstance(definition, player, requestedZoneIndex, autoSelectPublicZone);

        if (NeedsRuntimeStateSync(player, definition, instance.ZoneIndex, targetPosition))
        {
            var snapshot = player.RuntimeState.UpdateCurrentState(current => current with
            {
                CurrentMapId = definition.MapId,
                CurrentZoneIndex = instance.ZoneIndex,
                CurrentPosX = targetPosition.X,
                CurrentPosY = targetPosition.Y
            });

            player.SynchronizeFromCurrentState(snapshot.CurrentState);
        }

        instance.UpdatePlayerPosition(player);
        return definition;
    }

    public void PublishWorldSnapshot(PlayerSession player)
    {
        if (!_worldManager.MapManager.TryGetInstance(player.MapId, player.InstanceId, out var instance))
            return;

        _network.Send(player.ConnectionId, new MapJoinedPacket
        {
            Map = instance.Definition.ToModel(),
            ZoneIndex = instance.ZoneIndex
        });

        RefreshVisibility(player);
    }

    public void HandlePositionUpdated(
        PlayerSession player,
        CharacterCurrentStateDto previousState,
        CharacterCurrentStateDto currentState)
    {
        var previousMapId = previousState.CurrentMapId ?? 0;
        var currentMapId = currentState.CurrentMapId ?? 0;
        var previousVisibleIds = new HashSet<Guid>(player.GetVisibleCharacterIdsSnapshot());

        if (previousMapId != 0 && player.InstanceId != 0 &&
            (previousMapId != currentMapId || previousState.CurrentZoneIndex != currentState.CurrentZoneIndex))
        {
            LeaveCurrentWorld(player, previousMapId, player.InstanceId, previousState.CurrentZoneIndex);
            player.InstanceId = 0;
        }

        EnsurePlayerInWorld(player, currentState.CurrentZoneIndex, autoSelectPublicZone: false);
        RefreshVisibility(player);
        PublishMoveToExistingObservers(player, currentState, previousVisibleIds);
    }

    public void NotifyCurrentStateChanged(PlayerSession player, CharacterCurrentStateDto currentState)
    {
        foreach (var observer in GetVisiblePlayers(player))
        {
            _network.Send(observer.ConnectionId, new ObservedCharacterCurrentStateChangedPacket
            {
                CurrentState = currentState.ToModel(_gameTimeService.GetCurrentSnapshot()),
                ZoneIndex = player.ZoneIndex
            });
        }
    }

    private void HandlePlayerRemoving(PlayerSession player)
    {
        if (player.MapId == 0 || player.InstanceId == 0)
        {
            player.ClearVisibleCharacters();
            return;
        }

        LeaveCurrentWorld(player, player.MapId, player.InstanceId, player.ZoneIndex);
    }

    private void LeaveCurrentWorld(PlayerSession player, int mapId, int instanceId, int zoneIndex)
    {
        foreach (var visibleCharacterId in player.GetVisibleCharacterIdsSnapshot())
        {
            if (_worldManager.TryGetPlayer(visibleCharacterId, out var other))
            {
                if (other.RemoveVisibleCharacter(player.PlayerId))
                {
                    _network.Send(other.ConnectionId, new ObservedCharacterDespawnedPacket
                    {
                        CharacterId = player.CharacterData.CharacterId,
                        MapId = mapId,
                        ZoneIndex = zoneIndex
                    });
                }

                _network.Send(player.ConnectionId, new ObservedCharacterDespawnedPacket
                {
                    CharacterId = other.CharacterData.CharacterId,
                    MapId = mapId,
                    ZoneIndex = zoneIndex
                });
            }
        }

        player.ClearVisibleCharacters();
        _worldManager.MapManager.RemovePlayer(mapId, instanceId, player);
    }

    private void RefreshVisibility(PlayerSession subject)
    {
        if (!_worldManager.MapManager.TryGetInstance(subject.MapId, subject.InstanceId, out var instance))
            return;

        var nearbyPlayers = instance
            .GetPlayersSnapshot(subject.PlayerId)
            .Where(player => player.IsConnected)
            .ToArray();

        var nextVisibleIds = nearbyPlayers
            .Select(player => player.PlayerId)
            .ToHashSet();
        var currentVisibleIds = subject.GetVisibleCharacterIdsSnapshot().ToHashSet();

        foreach (var removedId in currentVisibleIds.Except(nextVisibleIds))
        {
            if (_worldManager.TryGetPlayer(removedId, out var removedPlayer))
            {
                _network.Send(subject.ConnectionId, new ObservedCharacterDespawnedPacket
                {
                    CharacterId = removedPlayer.CharacterData.CharacterId,
                    MapId = removedPlayer.MapId,
                    ZoneIndex = removedPlayer.ZoneIndex
                });

                if (removedPlayer.RemoveVisibleCharacter(subject.PlayerId))
                {
                    _network.Send(removedPlayer.ConnectionId, new ObservedCharacterDespawnedPacket
                    {
                        CharacterId = subject.CharacterData.CharacterId,
                        MapId = subject.MapId,
                        ZoneIndex = subject.ZoneIndex
                    });
                }
            }

            subject.RemoveVisibleCharacter(removedId);
        }

        foreach (var visiblePlayer in nearbyPlayers)
        {
            if (subject.AddVisibleCharacter(visiblePlayer.PlayerId))
            {
                _network.Send(subject.ConnectionId, new ObservedCharacterSpawnedPacket
                {
                    Character = visiblePlayer.ToObservedCharacterModel(_gameTimeService.GetCurrentSnapshot())
                });
            }

            if (visiblePlayer.AddVisibleCharacter(subject.PlayerId))
            {
                _network.Send(visiblePlayer.ConnectionId, new ObservedCharacterSpawnedPacket
                {
                    Character = subject.ToObservedCharacterModel(_gameTimeService.GetCurrentSnapshot())
                });
            }
        }
    }

    private void PublishMoveToExistingObservers(
        PlayerSession player,
        CharacterCurrentStateDto currentState,
        HashSet<Guid> previousVisibleIds)
    {
        var currentVisibleIds = player.GetVisibleCharacterIdsSnapshot().ToHashSet();
        foreach (var observerId in previousVisibleIds.Intersect(currentVisibleIds))
        {
            if (!_worldManager.TryGetPlayer(observerId, out var observer))
                continue;

            if (observer.MapId != player.MapId || observer.InstanceId != player.InstanceId)
                continue;

            _network.Send(observer.ConnectionId, new ObservedCharacterMovedPacket
            {
                CharacterId = player.CharacterData.CharacterId,
                MapId = player.MapId,
                ZoneIndex = player.ZoneIndex,
                CurrentPosX = currentState.CurrentPosX,
                CurrentPosY = currentState.CurrentPosY
            });
        }
    }

    private IReadOnlyCollection<PlayerSession> GetVisiblePlayers(PlayerSession player)
    {
        var result = new List<PlayerSession>();
        foreach (var visibleId in player.GetVisibleCharacterIdsSnapshot())
        {
            if (_worldManager.TryGetPlayer(visibleId, out var visiblePlayer) &&
                visiblePlayer.MapId == player.MapId &&
                visiblePlayer.InstanceId == player.InstanceId)
            {
                result.Add(visiblePlayer);
            }
        }

        return result;
    }

    private static bool NeedsRuntimeStateSync(PlayerSession player, MapDefinition definition, int zoneIndex, System.Numerics.Vector2 targetPosition)
    {
        return player.MapId != definition.MapId ||
               player.ZoneIndex != zoneIndex ||
               System.Numerics.Vector2.DistanceSquared(player.Position, targetPosition) > 0.0001f;
    }

    private static System.Numerics.Vector2 ResolveEntryPosition(PlayerSession player, MapDefinition definition)
    {
        if (player.MapId == definition.MapId)
            return definition.ClampPosition(player.Position);

        return definition.DefaultSpawnPosition;
    }
}
