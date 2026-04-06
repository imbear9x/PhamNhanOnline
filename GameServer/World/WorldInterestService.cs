using GameServer.DTO;
using GameServer.Network.Interface;
using GameServer.Time;
using GameShared.Logging;
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
        bool autoSelectPublicZone = false,
        MapEntryContext? requestedEntry = null)
    {
        var definition = _worldManager.MapManager.ResolveDefinitionOrDefault(player.MapId == 0 ? null : player.MapId);
        var resolvedEntry = ResolveEntryContext(player, definition, requestedEntry);
        var targetPosition = resolvedEntry.Position;

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
        player.SetMapEntryContext(resolvedEntry);
        return definition;
    }

    public void PublishWorldSnapshot(PlayerSession player)
    {
        if (!_worldManager.MapManager.TryGetInstance(player.MapId, player.InstanceId, out var instance))
        {
            Logger.Error(
                $"PublishWorldSnapshot failed to resolve instance for player {player.PlayerId}. " +
                $"CharacterId={player.CharacterData.CharacterId}, MapId={player.MapId}, InstanceId={player.InstanceId}, ZoneIndex={player.ZoneIndex}");
            return;
        }

        _network.Send(player.ConnectionId, new MapJoinedPacket
        {
            Map = instance.Definition.ToModel(),
            ZoneIndex = instance.ZoneIndex,
            EntryReason = (int)player.LastMapEntryContext.Reason,
            EntryPortalId = player.LastMapEntryContext.PortalId,
            EntrySpawnPointId = player.LastMapEntryContext.SpawnPointId
        });

        _network.Send(player.ConnectionId, new WorldRuntimeSnapshotPacket
        {
            MapId = instance.MapId,
            InstanceId = instance.InstanceId,
            ZoneIndex = instance.ZoneIndex,
            RuntimeKind = (int)instance.RuntimeKind,
            ExpiresAtUnixMs = ToUnixMs(instance.ExpiresAtUtc),
            CompletedAtUnixMs = ToUnixMs(instance.CompletedAtUtc),
            Enemies = instance.GetEnemiesSnapshot().Select(x => x.ToModel()).ToList(),
            GroundRewards = instance.GetGroundRewardsSnapshot().Select(x => x.ToModel()).ToList()
        });

        // The client rebuilds its observed-entity list after a fresh world snapshot,
        // so force this session to resend the currently visible players as spawn packets.
        player.ClearVisibleCharacters();
        RefreshVisibility(player);
        ResendVisiblePlayersToSubject(player);
    }

    public void NotifyEnemySpawned(MapInstance instance, MonsterEntity enemy)
    {
        BroadcastToInstancePlayers(instance, new EnemySpawnedPacket
        {
            MapId = instance.MapId,
            InstanceId = instance.InstanceId,
            Enemy = enemy.ToModel()
        });
    }

    public void NotifyEnemyDespawned(MapInstance instance, int enemyRuntimeId)
    {
        BroadcastToInstancePlayers(instance, new EnemyDespawnedPacket
        {
            MapId = instance.MapId,
            InstanceId = instance.InstanceId,
            EnemyRuntimeId = enemyRuntimeId
        });
    }

    public void NotifyEnemyHpChanged(MapInstance instance, EnemyHpChangedRuntimeEvent hpChanged)
    {
        BroadcastToInstancePlayers(instance, new EnemyHpChangedPacket
        {
            MapId = instance.MapId,
            InstanceId = instance.InstanceId,
            EnemyRuntimeId = hpChanged.EnemyRuntimeId,
            CurrentHp = hpChanged.CurrentHp,
            MaxHp = hpChanged.MaxHp,
            RuntimeState = (int)hpChanged.RuntimeState
        });
    }

    public void NotifySkillCastStarted(MapInstance instance, PendingSkillExecution execution)
    {
        BroadcastToInstancePlayers(instance, new SkillCastStartedPacket
        {
            MapId = instance.MapId,
            InstanceId = instance.InstanceId,
            Caster = execution.Caster.ToModel(),
            CasterCharacterId = execution.CasterCharacterId,
            Target = execution.Target?.ToModel(),
            SkillExecutionId = execution.ExecutionId,
            SkillSlotIndex = execution.SkillSlotIndex,
            PlayerSkillId = execution.PlayerSkillId,
            SkillId = execution.SkillId,
            SkillCode = execution.SkillCode,
            SkillGroupCode = execution.SkillGroupCode,
            CastTimeMs = execution.CastTimeMs,
            TravelTimeMs = execution.TravelTimeMs,
            CastStartedUnixMs = ToUnixMs(execution.CastStartedAtUtc),
            CastCompletedUnixMs = ToUnixMs(execution.CastCompletedAtUtc),
            ImpactUnixMs = ToUnixMs(execution.ImpactAtUtc)
        });
    }

    public void NotifySkillImpactResolved(MapInstance instance, SkillImpactResolvedRuntimeEvent impact)
    {
        BroadcastToInstancePlayers(instance, new SkillImpactResolvedPacket
        {
            MapId = instance.MapId,
            InstanceId = instance.InstanceId,
            Caster = impact.Caster.ToModel(),
            CasterCharacterId = impact.CasterCharacterId,
            Target = impact.Target?.ToModel(),
            SkillExecutionId = impact.ExecutionId,
            SkillSlotIndex = impact.SkillSlotIndex,
            PlayerSkillId = impact.PlayerSkillId,
            SkillId = impact.SkillId,
            SkillCode = impact.SkillCode,
            SkillGroupCode = impact.SkillGroupCode,
            Success = impact.Applied,
            Code = impact.Code,
            DamageApplied = impact.DamageApplied,
            RemainingHp = impact.RemainingHp,
            IsKilled = impact.IsKilled,
            ResolvedAtUnixMs = ToUnixMs(impact.ResolvedAtUtc)
        });
    }

    public void NotifyGroundRewardSpawned(MapInstance instance, GroundRewardEntity reward)
    {
        BroadcastToInstancePlayers(instance, new GroundRewardSpawnedPacket
        {
            MapId = instance.MapId,
            InstanceId = instance.InstanceId,
            Reward = reward.ToModel()
        });
    }

    public void NotifyGroundRewardDespawned(MapInstance instance, int rewardId)
    {
        BroadcastToInstancePlayers(instance, new GroundRewardDespawnedPacket
        {
            MapId = instance.MapId,
            InstanceId = instance.InstanceId,
            RewardId = rewardId
        });
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
        var snapshot = player.RuntimeState.CaptureSnapshot();
        var maxHp = snapshot.BaseStats.GetEffectiveHp();
        var maxMp = snapshot.BaseStats.GetEffectiveMp();

        foreach (var observer in GetVisiblePlayers(player))
        {
            _network.Send(observer.ConnectionId, new ObservedCharacterCurrentStateChangedPacket
            {
                CurrentState = currentState.ToModel(_gameTimeService.GetCurrentSnapshot()),
                MaxHp = maxHp,
                MaxMp = maxMp,
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
            if (_worldManager.TryGetPlayerByCharacterId(visibleCharacterId, out var other))
            {
                if (other.RemoveVisibleCharacter(player.CharacterData.CharacterId))
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
            .Select(player => player.CharacterData.CharacterId)
            .ToHashSet();
        var currentVisibleIds = subject.GetVisibleCharacterIdsSnapshot().ToHashSet();

        foreach (var removedId in currentVisibleIds.Except(nextVisibleIds))
        {
            if (_worldManager.TryGetPlayerByCharacterId(removedId, out var removedPlayer))
            {
                _network.Send(subject.ConnectionId, new ObservedCharacterDespawnedPacket
                {
                    CharacterId = removedPlayer.CharacterData.CharacterId,
                    MapId = removedPlayer.MapId,
                    ZoneIndex = removedPlayer.ZoneIndex
                });

                if (removedPlayer.RemoveVisibleCharacter(subject.CharacterData.CharacterId))
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
            if (subject.AddVisibleCharacter(visiblePlayer.CharacterData.CharacterId))
            {
                _network.Send(subject.ConnectionId, new ObservedCharacterSpawnedPacket
                {
                    Character = visiblePlayer.ToObservedCharacterModel(_gameTimeService.GetCurrentSnapshot())
                });
            }

            if (visiblePlayer.AddVisibleCharacter(subject.CharacterData.CharacterId))
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
            if (!_worldManager.TryGetPlayerByCharacterId(observerId, out var observer))
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
            if (_worldManager.TryGetPlayerByCharacterId(visibleId, out var visiblePlayer) &&
                visiblePlayer.MapId == player.MapId &&
                visiblePlayer.InstanceId == player.InstanceId)
            {
                result.Add(visiblePlayer);
            }
        }

        return result;
    }

    private void ResendVisiblePlayersToSubject(PlayerSession subject)
    {
        var visiblePlayers = GetVisiblePlayers(subject);
        foreach (var visiblePlayer in visiblePlayers)
        {
            _network.Send(subject.ConnectionId, new ObservedCharacterSpawnedPacket
            {
                Character = visiblePlayer.ToObservedCharacterModel(_gameTimeService.GetCurrentSnapshot())
            });
        }
    }

    private static bool NeedsRuntimeStateSync(PlayerSession player, MapDefinition definition, int zoneIndex, System.Numerics.Vector2 targetPosition)
    {
        return player.MapId != definition.MapId ||
               player.ZoneIndex != zoneIndex ||
               System.Numerics.Vector2.DistanceSquared(player.Position, targetPosition) > 0.0001f;
    }

    private void BroadcastToInstancePlayers(MapInstance instance, IPacket packet)
    {
        foreach (var player in instance.GetPlayersSnapshot())
        {
            if (!player.IsConnected)
                continue;

            _network.Send(player.ConnectionId, packet);
        }
    }

    private static MapEntryContext ResolveEntryContext(
        PlayerSession player,
        MapDefinition definition,
        MapEntryContext? requestedEntry)
    {
        if (requestedEntry is not null)
        {
            return requestedEntry with
            {
                Position = definition.ClampPosition(requestedEntry.Position)
            };
        }

        if (player.MapId == definition.MapId)
        {
            return new MapEntryContext(
                MapEntryReason.SavedPosition,
                PortalId: null,
                SpawnPointId: null,
                definition.ClampPosition(player.Position));
        }

        return new MapEntryContext(
            MapEntryReason.DefaultSpawn,
            PortalId: null,
            SpawnPointId: null,
            definition.DefaultSpawnPosition);
    }

    private static long? ToUnixMs(DateTime? value)
    {
        if (!value.HasValue)
            return null;

        var utc = value.Value.Kind == DateTimeKind.Utc
            ? value.Value
            : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
        return new DateTimeOffset(utc).ToUnixTimeMilliseconds();
    }
}
