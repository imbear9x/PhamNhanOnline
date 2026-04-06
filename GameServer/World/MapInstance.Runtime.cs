using System.Numerics;
using GameServer.Runtime;

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

            if (monster.HasCombatTarget())
            {
                if (!monster.CombatTargetPlayerId.HasValue ||
                    !_playersById.TryGetValue(monster.CombatTargetPlayerId.Value, out var targetPlayer) ||
                    !targetPlayer.IsConnected ||
                    targetPlayer.InstanceId != InstanceId ||
                    CharacterRuntimeStateCodes.IsDefeated(targetPlayer.RuntimeState.CaptureSnapshot().CurrentState))
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
                        _pendingPlayerDamages.Enqueue(new PlayerDamageRuntimeEvent(
                            targetPlayer.PlayerId,
                            monster.Id,
                            Math.Max(1, monster.GetEffectiveAttack(utcNow))));
                    }
                }
            }

            if (!monster.LastDamagedAtUtc.HasValue)
                continue;

            if (!monster.Definition.EnableOutOfCombatRestore)
                continue;

            var restoreDelaySeconds = Math.Max(0, monster.Definition.OutOfCombatRestoreDelaySeconds);
            if (restoreDelaySeconds <= 0)
                continue;

            if (utcNow - monster.LastDamagedAtUtc.Value < TimeSpan.FromSeconds(restoreDelaySeconds))
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
