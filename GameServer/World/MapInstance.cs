using System.Numerics;
using GameServer.Randomness;
using GameServer.Runtime;
using GameShared.Messages;

namespace GameServer.World;

public sealed class MapInstance
{
    private static readonly TimeSpan EnemyOutOfCombatResetDelay = TimeSpan.FromSeconds(10);

    private readonly object _sync = new();
    private readonly Dictionary<(int X, int Y), HashSet<Guid>> _playersByCell = new();
    private readonly Dictionary<Guid, (int X, int Y)> _playerCells = new();
    private readonly Dictionary<Guid, PlayerSession> _playersById = new();
    private readonly Dictionary<int, SpawnGroupRuntimeState> _spawnStateByGroupId = new();
    private readonly Queue<EnemyDeathRuntimeEvent> _pendingDeaths = new();
    private readonly Queue<EnemySpawnRuntimeEvent> _pendingEnemySpawns = new();
    private readonly Queue<EnemyDespawnRuntimeEvent> _pendingEnemyDespawns = new();
    private readonly Queue<EnemyHpChangedRuntimeEvent> _pendingEnemyHpChanges = new();
    private readonly Queue<PlayerDamageRuntimeEvent> _pendingPlayerDamages = new();
    private readonly Queue<GroundRewardSpawnRuntimeEvent> _pendingGroundRewardSpawns = new();
    private readonly Queue<GroundRewardDespawnRuntimeEvent> _pendingGroundRewardDespawns = new();
    private readonly IRandomNumberProvider _random;
    private int _nextMonsterId = 1;
    private int _nextGroundRewardId = 1;
    private DateTime? _completedAtUtc;

    public int InstanceId { get; }
    public int ZoneIndex { get; }
    public Guid? OwnerCharacterId { get; }
    public bool IsPrivate => OwnerCharacterId.HasValue && RuntimeKind == MapRuntimeKind.PrivateHome;
    public MapDefinition Definition { get; }
    public int MapId => Definition.MapId;
    public DateTime CreatedAtUtc { get; }
    public DateTime? EmptySinceUtc { get; private set; }
    public MapRuntimeKind RuntimeKind { get; }
    public MapInstanceConfigDefinition? InstanceConfig { get; }
    public DateTime? ExpiresAtUtc { get; }
    public DateTime? CompletedAtUtc => _completedAtUtc;

    public List<PlayerSession> Players { get; } = new();
    public List<MonsterEntity> Monsters { get; } = new();
    public List<GroundRewardEntity> GroundRewards { get; } = new();

    public MapInstance(
        int instanceId,
        int zoneIndex,
        MapDefinition definition,
        Guid? ownerCharacterId,
        MapRuntimeKind runtimeKind,
        MapInstanceConfigDefinition? instanceConfig,
        IEnumerable<MapEnemySpawnGroupDefinition> spawnGroups,
        EnemyDefinitionCatalog enemyDefinitions,
        IRandomNumberProvider random,
        DateTime utcNow)
    {
        InstanceId = instanceId;
        ZoneIndex = zoneIndex;
        Definition = definition;
        OwnerCharacterId = ownerCharacterId;
        RuntimeKind = runtimeKind;
        InstanceConfig = instanceConfig;
        _random = random;
        CreatedAtUtc = utcNow;
        ExpiresAtUtc = instanceConfig?.DurationSeconds is > 0
            ? utcNow.AddSeconds(instanceConfig.DurationSeconds.Value)
            : null;

        foreach (var group in spawnGroups)
        {
            var state = new SpawnGroupRuntimeState(group, utcNow.AddSeconds(group.InitialSpawnDelaySeconds));
            foreach (var entry in group.Entries)
            {
                if (!enemyDefinitions.TryGetEnemy(entry.EnemyTemplateId, out var enemyDefinition))
                    throw new InvalidOperationException($"Spawn group {group.Id} references missing enemy template {entry.EnemyTemplateId}.");

                state.EnemyDefinitions[entry.EnemyTemplateId] = enemyDefinition;
            }

            _spawnStateByGroupId[group.Id] = state;
        }
    }

    public int PlayerCount
    {
        get
        {
            lock (_sync)
            {
                return Players.Count;
            }
        }
    }

    public bool AddPlayer(PlayerSession player)
    {
        lock (_sync)
        {
            if (RuntimeKind == MapRuntimeKind.PrivateHome && OwnerCharacterId != player.CharacterData.CharacterId)
                return false;

            var maxPlayers = RuntimeKind == MapRuntimeKind.PrivateHome ? 1 : Definition.MaxPlayersPerZone;
            if (Players.Count >= maxPlayers && !_playersById.ContainsKey(player.PlayerId))
                return false;

            if (!_playersById.ContainsKey(player.PlayerId))
            {
                Players.Add(player);
                _playersById[player.PlayerId] = player;
                player.MapId = MapId;
                player.InstanceId = InstanceId;
                player.ZoneIndex = ZoneIndex;
            }

            EmptySinceUtc = null;
            UpdatePlayerCellUnsafe(player);
            return true;
        }
    }

    public void RemovePlayer(PlayerSession player)
    {
        lock (_sync)
        {
            Players.Remove(player);
            _playersById.Remove(player.PlayerId);
            RemovePlayerCellUnsafe(player.PlayerId);
            if (player.MapId == MapId && player.InstanceId == InstanceId)
                player.InstanceId = 0;

            if (Players.Count == 0)
                EmptySinceUtc = DateTime.UtcNow;
        }
    }

    public void UpdatePlayerPosition(PlayerSession player)
    {
        lock (_sync)
        {
            if (!_playersById.ContainsKey(player.PlayerId))
                return;

            UpdatePlayerCellUnsafe(player);
        }
    }

    public IReadOnlyCollection<PlayerSession> GetNearbyPlayers(Vector2 position, float radius, Guid? excludePlayerId = null)
    {
        lock (_sync)
        {
            var radiusSquared = radius * radius;
            var baseCell = GetCell(position);
            var cellRadius = Math.Max(1, (int)MathF.Ceiling(radius / Definition.CellSize));
            var result = new List<PlayerSession>();
            var seen = new HashSet<Guid>();

            for (var dx = -cellRadius; dx <= cellRadius; dx++)
            {
                for (var dy = -cellRadius; dy <= cellRadius; dy++)
                {
                    var cell = (baseCell.X + dx, baseCell.Y + dy);
                    if (!_playersByCell.TryGetValue(cell, out var playerIds))
                        continue;

                    foreach (var playerId in playerIds)
                    {
                        if (excludePlayerId.HasValue && playerId == excludePlayerId.Value)
                            continue;

                        if (!seen.Add(playerId))
                            continue;

                        if (!_playersById.TryGetValue(playerId, out var player))
                            continue;

                        if (Vector2.DistanceSquared(player.Position, position) > radiusSquared)
                            continue;

                        result.Add(player);
                    }
                }
            }

            return result;
        }
    }

    public IReadOnlyCollection<PlayerSession> GetPlayersSnapshot(Guid? excludePlayerId = null)
    {
        lock (_sync)
        {
            if (!excludePlayerId.HasValue)
                return Players.ToArray();

            return Players.Where(player => player.PlayerId != excludePlayerId.Value).ToArray();
        }
    }

    public IReadOnlyCollection<MonsterEntity> GetEnemiesSnapshot()
    {
        lock (_sync)
        {
            return Monsters.ToArray();
        }
    }

    public IReadOnlyCollection<GroundRewardEntity> GetGroundRewardsSnapshot()
    {
        lock (_sync)
        {
            return GroundRewards.ToArray();
        }
    }

    public EnemyDamageApplicationResult ApplyEnemyDamage(PlayerSession attacker, int enemyRuntimeId, int damage, DateTime utcNow)
    {
        lock (_sync)
        {
            var enemy = Monsters.FirstOrDefault(x => x.Id == enemyRuntimeId);
            if (enemy is null)
                return new EnemyDamageApplicationResult(false, false, 0, MessageCode.EnemyNotFound);

            var result = enemy.ApplyDamage(attacker.PlayerId, damage, utcNow);
            if (result.Applied)
            {
                _pendingEnemyHpChanges.Enqueue(new EnemyHpChangedRuntimeEvent(
                    enemy.Id,
                    enemy.Hp,
                    enemy.MaxHp,
                    enemy.State));
            }

            if (!result.IsKilled)
                return result;

            if (_spawnStateByGroupId.TryGetValue(enemy.SpawnGroupId, out var spawnState))
            {
                spawnState.AliveEnemyIds.Remove(enemy.Id);
                if (spawnState.Group.SpawnMode == EnemySpawnMode.Timer && spawnState.Group.RespawnSeconds > 0)
                    spawnState.NextSpawnAtUtc = utcNow.AddSeconds(spawnState.Group.RespawnSeconds);
            }

            _pendingDeaths.Enqueue(new EnemyDeathRuntimeEvent(
                enemy.Definition,
                enemy.Position,
                enemy.LastHitPlayerId,
                enemy.CaptureContributionsSnapshot(),
                utcNow));

            return result;
        }
    }

    public void AddGroundReward(GroundRewardEntity reward)
    {
        lock (_sync)
        {
            GroundRewards.Add(reward);
            _pendingGroundRewardSpawns.Enqueue(new GroundRewardSpawnRuntimeEvent(reward));
        }
    }

    public int AllocateGroundRewardId()
    {
        lock (_sync)
        {
            return _nextGroundRewardId++;
        }
    }

    public IReadOnlyCollection<EnemyDeathRuntimeEvent> DequeuePendingDeaths()
    {
        lock (_sync)
        {
            if (_pendingDeaths.Count == 0)
                return Array.Empty<EnemyDeathRuntimeEvent>();

            var result = new List<EnemyDeathRuntimeEvent>(_pendingDeaths.Count);
            while (_pendingDeaths.Count > 0)
                result.Add(_pendingDeaths.Dequeue());

            return result;
        }
    }

    public IReadOnlyCollection<EnemySpawnRuntimeEvent> DequeuePendingEnemySpawns()
    {
        lock (_sync)
        {
            return DrainQueueUnsafe(_pendingEnemySpawns);
        }
    }

    public IReadOnlyCollection<EnemyDespawnRuntimeEvent> DequeuePendingEnemyDespawns()
    {
        lock (_sync)
        {
            return DrainQueueUnsafe(_pendingEnemyDespawns);
        }
    }

    public IReadOnlyCollection<EnemyHpChangedRuntimeEvent> DequeuePendingEnemyHpChanges()
    {
        lock (_sync)
        {
            return DrainQueueUnsafe(_pendingEnemyHpChanges);
        }
    }

    public IReadOnlyCollection<GroundRewardSpawnRuntimeEvent> DequeuePendingGroundRewardSpawns()
    {
        lock (_sync)
        {
            return DrainQueueUnsafe(_pendingGroundRewardSpawns);
        }
    }

    public IReadOnlyCollection<GroundRewardDespawnRuntimeEvent> DequeuePendingGroundRewardDespawns()
    {
        lock (_sync)
        {
            return DrainQueueUnsafe(_pendingGroundRewardDespawns);
        }
    }

    public IReadOnlyCollection<PlayerDamageRuntimeEvent> DequeuePendingPlayerDamages()
    {
        lock (_sync)
        {
            return DrainQueueUnsafe(_pendingPlayerDamages);
        }
    }

    public bool TryClaimGroundReward(
        Guid pickerCharacterId,
        int rewardId,
        DateTime utcNow,
        out GroundRewardEntity reward,
        out MessageCode failureCode)
    {
        lock (_sync)
        {
            reward = null!;
            failureCode = MessageCode.None;

            var resolvedReward = GroundRewards.FirstOrDefault(x => x.Id == rewardId);
            if (resolvedReward is null)
            {
                failureCode = MessageCode.GroundRewardNotFound;
                return false;
            }

            resolvedReward.Update(utcNow);
            if (resolvedReward.IsDestroyed)
            {
                GroundRewards.Remove(resolvedReward);
                _pendingGroundRewardDespawns.Enqueue(new GroundRewardDespawnRuntimeEvent(resolvedReward.Id));
                failureCode = MessageCode.GroundRewardExpired;
                return false;
            }

            if (resolvedReward.OwnerCharacterId.HasValue &&
                resolvedReward.OwnerCharacterId.Value != pickerCharacterId)
            {
                failureCode = MessageCode.GroundRewardNotOwnedYet;
                return false;
            }

            GroundRewards.Remove(resolvedReward);
            _pendingGroundRewardDespawns.Enqueue(new GroundRewardDespawnRuntimeEvent(resolvedReward.Id));
            reward = resolvedReward;
            return true;
        }
    }

    public void Update(DateTime utcNow)
    {
        lock (_sync)
        {
            UpdateEnemyStatesUnsafe(utcNow);
            UpdateSpawnGroupsUnsafe(utcNow);
            UpdateGroundRewardsUnsafe(utcNow);
            UpdateCompletionStateUnsafe(utcNow);
        }
    }

    public bool ShouldDestroy(DateTime utcNow)
    {
        lock (_sync)
        {
            if (ExpiresAtUtc.HasValue && utcNow >= ExpiresAtUtc.Value)
                return true;

            if (_completedAtUtc.HasValue && InstanceConfig?.CompleteDestroyDelaySeconds is > 0)
                return utcNow >= _completedAtUtc.Value.AddSeconds(InstanceConfig.CompleteDestroyDelaySeconds.Value);

            if (RuntimeKind == MapRuntimeKind.SoloFarmInstance &&
                Players.Count == 0 &&
                EmptySinceUtc.HasValue &&
                InstanceConfig?.IdleDestroySeconds is > 0)
            {
                return utcNow >= EmptySinceUtc.Value.AddSeconds(InstanceConfig.IdleDestroySeconds.Value);
            }

            return false;
        }
    }

    private void UpdateEnemyStatesUnsafe(DateTime utcNow)
    {
        foreach (var monster in Monsters)
        {
            if (!monster.IsAlive)
                continue;

            if (monster.HasCombatTarget())
            {
                if (!monster.CombatTargetPlayerId.HasValue ||
                    !_playersById.TryGetValue(monster.CombatTargetPlayerId.Value, out var targetPlayer) ||
                    !targetPlayer.IsConnected ||
                    targetPlayer.InstanceId != InstanceId)
                {
                    monster.ReturnToPatrol();
                    _pendingEnemyHpChanges.Enqueue(new EnemyHpChangedRuntimeEvent(
                        monster.Id,
                        monster.Hp,
                        monster.MaxHp,
                        monster.State));
                    continue;
                }

                var combatRadiusSquared = monster.Definition.CombatRadius * monster.Definition.CombatRadius;
                if (Vector2.DistanceSquared(monster.Position, targetPlayer.Position) > combatRadiusSquared)
                {
                    monster.ReturnToPatrol();
                    _pendingEnemyHpChanges.Enqueue(new EnemyHpChangedRuntimeEvent(
                        monster.Id,
                        monster.Hp,
                        monster.MaxHp,
                        monster.State));
                    continue;
                }

                if (monster.TryConsumeAttackWindow(utcNow))
                {
                    _pendingPlayerDamages.Enqueue(new PlayerDamageRuntimeEvent(
                        targetPlayer.PlayerId,
                        monster.Id,
                        Math.Max(1, monster.Definition.BaseAttack)));
                }
            }

            if (!monster.LastDamagedAtUtc.HasValue)
                continue;

            if (utcNow - monster.LastDamagedAtUtc.Value < EnemyOutOfCombatResetDelay)
                continue;

            if (monster.Definition.Kind == EnemyKind.Boss)
            {
                monster.ReturnToPatrol();
                _pendingEnemyHpChanges.Enqueue(new EnemyHpChangedRuntimeEvent(
                    monster.Id,
                    monster.Hp,
                    monster.MaxHp,
                    monster.State));
                continue;
            }

            monster.RestoreFullHealth(utcNow);
            _pendingEnemyHpChanges.Enqueue(new EnemyHpChangedRuntimeEvent(
                monster.Id,
                monster.Hp,
                monster.MaxHp,
                monster.State));
        }
    }

    private void UpdateSpawnGroupsUnsafe(DateTime utcNow)
    {
        foreach (var state in _spawnStateByGroupId.Values)
        {
            if (state.Group.SpawnMode != EnemySpawnMode.Timer)
                continue;

            if (state.NextSpawnAtUtc.HasValue && utcNow < state.NextSpawnAtUtc.Value)
                continue;

            if (!state.InitialFillDone)
            {
                while (state.AliveEnemyIds.Count < state.Group.MaxAlive)
                    SpawnOneEnemyUnsafe(state, utcNow);

                state.InitialFillDone = true;
                state.NextSpawnAtUtc = null;
                continue;
            }

            if (state.AliveEnemyIds.Count >= state.Group.MaxAlive)
            {
                state.NextSpawnAtUtc = null;
                continue;
            }

            SpawnOneEnemyUnsafe(state, utcNow);
            state.NextSpawnAtUtc = state.AliveEnemyIds.Count < state.Group.MaxAlive && state.Group.RespawnSeconds > 0
                ? utcNow.AddSeconds(state.Group.RespawnSeconds)
                : null;
        }
    }

    private void SpawnOneEnemyUnsafe(SpawnGroupRuntimeState state, DateTime utcNow)
    {
        var enemyTemplateId = ResolveWeightedEnemyTemplateId(state.Group.Entries);
        var definition = state.EnemyDefinitions[enemyTemplateId];
        var position = ResolveSpawnPosition(state.Group);
        var enemy = new MonsterEntity(_nextMonsterId++, state.Group.Id, definition, position, utcNow);
        Monsters.Add(enemy);
        state.AliveEnemyIds.Add(enemy.Id);
        _pendingEnemySpawns.Enqueue(new EnemySpawnRuntimeEvent(enemy));
    }

    private int ResolveWeightedEnemyTemplateId(IReadOnlyList<MapEnemySpawnEntryDefinition> entries)
    {
        if (entries.Count == 0)
            throw new InvalidOperationException($"Spawn group in instance {InstanceId} does not contain any entries.");

        var totalWeight = entries.Sum(x => Math.Max(1, x.Weight));
        var roll = _random.NextInt(totalWeight);
        var cursor = 0;
        foreach (var entry in entries)
        {
            cursor += Math.Max(1, entry.Weight);
            if (roll < cursor)
                return entry.EnemyTemplateId;
        }

        return entries[^1].EnemyTemplateId;
    }

    private Vector2 ResolveSpawnPosition(MapEnemySpawnGroupDefinition group)
    {
        if (group.SpawnRadius <= 0f)
            return Definition.ClampPosition(group.CenterPosition);

        var angle = (_random.NextInt(3600) / 10f) * (MathF.PI / 180f);
        var distance = group.SpawnRadius * MathF.Sqrt(_random.NextInt(10_000) / 10_000f);
        var position = new Vector2(
            group.CenterPosition.X + MathF.Cos(angle) * distance,
            group.CenterPosition.Y + MathF.Sin(angle) * distance);
        return Definition.ClampPosition(position);
    }

    private void UpdateGroundRewardsUnsafe(DateTime utcNow)
    {
        for (var index = GroundRewards.Count - 1; index >= 0; index--)
        {
            var reward = GroundRewards[index];
            reward.Update(utcNow);
            if (reward.IsDestroyed)
            {
                GroundRewards.RemoveAt(index);
                _pendingGroundRewardDespawns.Enqueue(new GroundRewardDespawnRuntimeEvent(reward.Id));
            }
        }

        for (var index = Monsters.Count - 1; index >= 0; index--)
        {
            var monster = Monsters[index];
            if (!monster.IsAlive && monster.DiedAtUtc.HasValue && utcNow >= monster.DiedAtUtc.Value.AddSeconds(2))
            {
                Monsters.RemoveAt(index);
                _pendingEnemyDespawns.Enqueue(new EnemyDespawnRuntimeEvent(monster.Id));
            }
        }
    }

    private void UpdateCompletionStateUnsafe(DateTime utcNow)
    {
        if (_completedAtUtc.HasValue || InstanceConfig?.CompletionRule != InstanceCompletionRule.KillBoss)
            return;

        if (Monsters.Any(x => x.IsAlive && x.Definition.Kind == EnemyKind.Boss))
            return;

        if (_spawnStateByGroupId.Values.Any(x => x.Group.IsBossSpawn && !x.InitialFillDone))
            return;

        _completedAtUtc = utcNow;
    }

    private (int X, int Y) GetCell(Vector2 position)
    {
        var clamped = Definition.ClampPosition(position);
        var x = (int)MathF.Floor(clamped.X / Definition.CellSize);
        var y = (int)MathF.Floor(clamped.Y / Definition.CellSize);
        return (x, y);
    }

    private void UpdatePlayerCellUnsafe(PlayerSession player)
    {
        var newCell = GetCell(player.Position);
        if (_playerCells.TryGetValue(player.PlayerId, out var existingCell) && existingCell == newCell)
            return;

        RemovePlayerCellUnsafe(player.PlayerId);

        if (!_playersByCell.TryGetValue(newCell, out var players))
        {
            players = new HashSet<Guid>();
            _playersByCell[newCell] = players;
        }

        players.Add(player.PlayerId);
        _playerCells[player.PlayerId] = newCell;
    }

    private void RemovePlayerCellUnsafe(Guid playerId)
    {
        if (!_playerCells.TryGetValue(playerId, out var cell))
            return;

        if (_playersByCell.TryGetValue(cell, out var players))
        {
            players.Remove(playerId);
            if (players.Count == 0)
                _playersByCell.Remove(cell);
        }

        _playerCells.Remove(playerId);
    }

    private static IReadOnlyCollection<T> DrainQueueUnsafe<T>(Queue<T> queue)
    {
        if (queue.Count == 0)
            return Array.Empty<T>();

        var result = new List<T>(queue.Count);
        while (queue.Count > 0)
            result.Add(queue.Dequeue());

        return result;
    }

    private sealed class SpawnGroupRuntimeState
    {
        public SpawnGroupRuntimeState(MapEnemySpawnGroupDefinition group, DateTime? nextSpawnAtUtc)
        {
            Group = group;
            NextSpawnAtUtc = nextSpawnAtUtc;
            EnemyDefinitions = new Dictionary<int, EnemyDefinition>();
        }

        public MapEnemySpawnGroupDefinition Group { get; }
        public HashSet<int> AliveEnemyIds { get; } = [];
        public Dictionary<int, EnemyDefinition> EnemyDefinitions { get; }
        public bool InitialFillDone { get; set; }
        public DateTime? NextSpawnAtUtc { get; set; }
    }
}

public sealed record EnemyDeathRuntimeEvent(
    EnemyDefinition Definition,
    Vector2 Position,
    Guid? LastHitPlayerId,
    IReadOnlyList<RewardTargetSnapshot> Targets,
    DateTime KilledAtUtc);

public sealed record EnemySpawnRuntimeEvent(MonsterEntity Enemy);

public readonly record struct EnemyDespawnRuntimeEvent(int EnemyRuntimeId);

public readonly record struct EnemyHpChangedRuntimeEvent(
    int EnemyRuntimeId,
    int CurrentHp,
    int MaxHp,
    EnemyRuntimeState RuntimeState);

public sealed record GroundRewardSpawnRuntimeEvent(GroundRewardEntity Reward);

public readonly record struct GroundRewardDespawnRuntimeEvent(int RewardId);

public readonly record struct PlayerDamageRuntimeEvent(
    Guid TargetPlayerId,
    int EnemyRuntimeId,
    int Damage);
