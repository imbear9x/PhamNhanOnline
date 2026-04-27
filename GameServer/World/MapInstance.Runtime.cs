using System.Numerics;
using GameServer.Runtime;
using GameShared.Logging;

namespace GameServer.World;

public sealed partial class MapInstance
{
    public void Update(DateTime utcNow)
    {
        lock (_sync)
        {
            UpdateSkillExecutionsUnsafe(utcNow);
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

            var shouldBroadcastDecision = monster.AdvancePatrolMovement(utcNow);
            var spawnState = _spawnStateByGroupId[monster.SpawnGroupId];
            if (shouldBroadcastDecision &&
                monster.State == EnemyRuntimeState.Patrol &&
                monster.MovementMode == EnemyMovementMode.None)
            {
                monster.WaitAtCurrentPosition(monster.RollPatrolPauseSeconds(_random), utcNow);
            }

            if (monster.State == EnemyRuntimeState.Combat)
            {
                PlayerSession? targetPlayer;
                if (!TryResolveCombatTargetUnsafe(monster, utcNow, out targetPlayer))
                {
                    shouldBroadcastDecision |= monster.ReturnToPatrol(utcNow);
                }
                else
                {
                    shouldBroadcastDecision |= monster.EnterCombat(targetPlayer.PlayerId, utcNow);
                    if (monster.TryConsumeAttackWindow(utcNow, out var selectedSkill))
                    {
                        if (selectedSkill is not null)
                        {
                            _pendingEnemySkillCastRequests.Enqueue(new EnemySkillCastRequestRuntimeEvent(
                                monster.Id,
                                targetPlayer.PlayerId,
                                selectedSkill.SkillId,
                                Math.Max(0, selectedSkill.OrderIndex)));
                        }
                        else
                        {
                            if (monster.TryMarkMissingAttackSkillLogged())
                            {
                                Logger.Info(
                                    $"Enemy cannot attack because no skill/basic attack is configured. " +
                                    $"MapId={MapId}, InstanceId={InstanceId}, EnemyRuntimeId={monster.Id}, " +
                                    $"EnemyTemplateId={monster.Definition.Id}, EnemyCode={monster.Definition.Code}.");
                            }
                        }
                    }
                }
            }

            if (monster.State == EnemyRuntimeState.Patrol)
            {
                if (monster.Definition.AiBehavior == EnemyAiBehavior.Aggressive &&
                    TryAcquireAggressiveTargetUnsafe(monster, out var aggressiveTarget))
                {
                    shouldBroadcastDecision |= monster.EnterCombat(aggressiveTarget.PlayerId, utcNow);
                }
                else if (monster.ShouldChooseNewPatrolDestination(utcNow))
                {
                    shouldBroadcastDecision |= TryScheduleNextPatrolDecisionUnsafe(monster, spawnState, utcNow);
                }
            }

            if (!monster.LastDamagedAtUtc.HasValue)
            {
                if (shouldBroadcastDecision)
                    _pendingEnemyMovementDecisions.Enqueue(new EnemyMovementDecisionRuntimeEvent(monster));

                continue;
            }

            if (!monster.Definition.EnableOutOfCombatRestore)
            {
                if (shouldBroadcastDecision)
                    _pendingEnemyMovementDecisions.Enqueue(new EnemyMovementDecisionRuntimeEvent(monster));

                continue;
            }

            var restoreDelaySeconds = Math.Max(0, monster.Definition.OutOfCombatRestoreDelaySeconds);
            if (restoreDelaySeconds <= 0 || utcNow - monster.LastDamagedAtUtc.Value < TimeSpan.FromSeconds(restoreDelaySeconds))
            {
                if (shouldBroadcastDecision)
                    _pendingEnemyMovementDecisions.Enqueue(new EnemyMovementDecisionRuntimeEvent(monster));

                continue;
            }

            if (monster.Definition.Kind == EnemyKind.Boss)
            {
                shouldBroadcastDecision |= monster.ReturnToPatrol(utcNow);
                _pendingEnemyHpChanges.Enqueue(new EnemyHpChangedRuntimeEvent(
                    monster.Id,
                    monster.Hp,
                    monster.MaxHp,
                    monster.State));
            }
            else
            {
                var resetChanged = monster.RestoreFullHealth(utcNow);
                _pendingEnemyHpChanges.Enqueue(new EnemyHpChangedRuntimeEvent(
                    monster.Id,
                    monster.Hp,
                    monster.MaxHp,
                    monster.State));
                shouldBroadcastDecision |= resetChanged;
            }

            if (shouldBroadcastDecision)
                _pendingEnemyMovementDecisions.Enqueue(new EnemyMovementDecisionRuntimeEvent(monster));
        }
    }

    private bool TryResolveCombatTargetUnsafe(MonsterEntity monster, DateTime utcNow, out PlayerSession targetPlayer)
    {
        targetPlayer = null!;

        var combatRadius = ResolveEffectiveCombatRadius(monster);
        if (combatRadius <= 0f)
            return false;

        Guid? pendingAggroOverridePlayerId = monster.PeekPendingAggroOverridePlayerId();
        if (pendingAggroOverridePlayerId.HasValue &&
            TryGetValidPlayerInRangeUnsafe(monster, pendingAggroOverridePlayerId.Value, combatRadius, out targetPlayer))
        {
            monster.ClearPendingAggroOverride(pendingAggroOverridePlayerId);
            return true;
        }

        monster.ClearPendingAggroOverride(pendingAggroOverridePlayerId);

        if (monster.Definition.AiBehavior == EnemyAiBehavior.Passive)
        {
            return monster.CombatTargetPlayerId.HasValue &&
                   TryGetValidPlayerInRangeUnsafe(monster, monster.CombatTargetPlayerId.Value, combatRadius, out targetPlayer);
        }

        return TryFindNearestPlayerInRangeUnsafe(monster, combatRadius, out targetPlayer);
    }

    private bool TryAcquireAggressiveTargetUnsafe(MonsterEntity monster, out PlayerSession targetPlayer)
    {
        targetPlayer = null!;
        var aggroRadius = ResolveEffectiveAggroRadius(monster);
        return aggroRadius > 0f && TryFindNearestPlayerInRangeUnsafe(monster, aggroRadius, out targetPlayer);
    }

    private float ResolveEffectiveAggroRadius(MonsterEntity monster)
    {
        var detectionRadius = Math.Max(0f, monster.Definition.DetectionRadius);
        var combatRadius = Math.Max(0f, monster.Definition.CombatRadius);
        if (detectionRadius <= 0f)
            return combatRadius;

        if (combatRadius <= 0f)
            return detectionRadius;

        return Math.Min(detectionRadius, combatRadius);
    }

    private float ResolveEffectiveCombatRadius(MonsterEntity monster)
    {
        return ResolveEffectiveAggroRadius(monster);
    }

    private bool TryGetValidPlayerInRangeUnsafe(
        MonsterEntity monster,
        Guid playerId,
        float range,
        out PlayerSession targetPlayer)
    {
        targetPlayer = null!;
        if (!_playersById.TryGetValue(playerId, out var player))
            return false;

        if (!IsValidEnemyTargetUnsafe(monster, player, range))
            return false;

        targetPlayer = player;
        return true;
    }

    private bool TryFindNearestPlayerInRangeUnsafe(
        MonsterEntity monster,
        float range,
        out PlayerSession targetPlayer)
    {
        targetPlayer = null!;
        var rangeSquared = range * range;
        var bestDistanceSquared = float.MaxValue;
        foreach (var candidate in _playersById.Values)
        {
            if (!IsValidEnemyTargetUnsafe(monster, candidate, range))
                continue;

            var distanceSquared = Vector2.DistanceSquared(monster.Position, candidate.Position);
            if (distanceSquared >= bestDistanceSquared)
                continue;

            bestDistanceSquared = distanceSquared;
            targetPlayer = candidate;
        }

        return targetPlayer != null && bestDistanceSquared <= rangeSquared;
    }

    private bool IsValidEnemyTargetUnsafe(MonsterEntity monster, PlayerSession candidate, float range)
    {
        if (!candidate.IsConnected ||
            candidate.InstanceId != InstanceId ||
            candidate.MapId != MapId ||
            CharacterRuntimeStateCodes.IsDefeated(candidate.RuntimeState.CaptureSnapshot().CurrentState))
        {
            return false;
        }

        var rangeSquared = range * range;
        return Vector2.DistanceSquared(monster.Position, candidate.Position) <= rangeSquared;
    }

    private bool TryScheduleNextPatrolDecisionUnsafe(MonsterEntity monster, SpawnGroupRuntimeState spawnState, DateTime utcNow)
    {
        if (!monster.IsWithinPatrolArea())
            return monster.StartMovingTo(Definition.ClampPosition(monster.GetPatrolCenterPosition()), monster.Definition.BaseMoveSpeed, utcNow);

        if (spawnState.Group.PatrolRouteType == EnemyPatrolRouteType.Horizontal &&
            monster.TryGetNextHorizontalPatrolTarget(out var horizontalTarget))
        {
            return monster.StartMovingTo(Definition.ClampPosition(horizontalTarget), monster.Definition.BaseMoveSpeed, utcNow);
        }

        var patrolRadius = Math.Max(0f, spawnState.Group.PatrolRadius);
        if (patrolRadius <= 0f || monster.Definition.BaseMoveSpeed <= 0f)
            return monster.WaitAtCurrentPosition(monster.RollPatrolPauseSeconds(_random), utcNow);

        var randomTarget = ResolveRandomPatrolTarget(monster.GetPatrolCenterPosition(), patrolRadius);
        return monster.StartMovingTo(Definition.ClampPosition(randomTarget), monster.Definition.BaseMoveSpeed, utcNow);
    }

    private Vector2 ResolveRandomPatrolTarget(Vector2 centerPosition, float patrolRadius)
    {
        if (patrolRadius <= 0f)
            return centerPosition;

        var angle = (_random.NextInt(3600) / 10f) * (MathF.PI / 180f);
        var distance = patrolRadius * MathF.Sqrt(_random.NextInt(10_000) / 10_000f);
        var position = new Vector2(
            centerPosition.X + MathF.Cos(angle) * distance,
            centerPosition.Y + MathF.Sin(angle) * distance);
        return Definition.ClampPosition(position);
    }

    private void UpdateSkillExecutionsUnsafe(DateTime utcNow)
    {
        for (var index = _pendingSkillExecutions.Count - 1; index >= 0; index--)
        {
            var execution = _pendingSkillExecutions[index];

            if (!execution.CastReleased && utcNow >= execution.CastCompletedAtUtc)
            {
                execution.MarkCastReleased();
                _pendingSkillCastReleases.Enqueue(new SkillCastReleaseRuntimeEvent(execution));
            }

            if (utcNow < execution.ImpactAtUtc)
                continue;

            _pendingSkillImpactDues.Enqueue(new SkillImpactDueRuntimeEvent(execution));
            _pendingSkillExecutions.RemoveAt(index);
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
        var enemy = new MonsterEntity(_nextMonsterId++, state.Group.Id, definition, state.Group, position, utcNow);
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
                _pendingGroundRewardDespawns.Enqueue(new GroundRewardDespawnRuntimeEvent(
                    reward.Id,
                    reward.GetPlayerItemIds(),
                    DestroyItems: true));
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
}
