using System.Numerics;
using GameServer.Runtime;
using GameShared.Messages;

namespace GameServer.World;

public sealed partial class MapInstance
{
    public EnemyDamageApplicationResult ApplyEnemyDamage(Guid? attackerPlayerId, int enemyRuntimeId, int damage, DateTime utcNow)
    {
        lock (_sync)
        {
            var enemy = Monsters.FirstOrDefault(x => x.Id == enemyRuntimeId);
            if (enemy is null)
                return new EnemyDamageApplicationResult(false, false, 0, 0, MessageCode.EnemyNotFound);

            var result = enemy.ApplyDamage(attackerPlayerId, damage, utcNow);
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

    public bool TryGetEnemySnapshot(int enemyRuntimeId, out EnemyTargetSnapshot snapshot)
    {
        lock (_sync)
        {
            var enemy = Monsters.FirstOrDefault(x => x.Id == enemyRuntimeId);
            if (enemy is null)
            {
                snapshot = default;
                return false;
            }

            snapshot = new EnemyTargetSnapshot(enemy.Id, enemy.Position, enemy.IsAlive);
            return true;
        }
    }

    public bool TryGetCombatTargetSnapshot(CombatTargetReference target, out CombatTargetSnapshot snapshot)
    {
        lock (_sync)
        {
            switch (target.Kind)
            {
                case GameShared.Enums.CombatTargetKind.Character:
                    if (!target.CharacterId.HasValue)
                        break;

                    var targetPlayer = Players.FirstOrDefault(x => x.CharacterData.CharacterId == target.CharacterId.Value);
                    if (targetPlayer is null || !targetPlayer.IsConnected)
                        break;

                    snapshot = new CombatTargetSnapshot(
                        target.Kind,
                        target.CharacterId,
                        null,
                        targetPlayer.Position,
                        !targetPlayer.RuntimeState.CaptureSnapshot().CurrentState.IsDead);
                    return true;

                case GameShared.Enums.CombatTargetKind.Enemy:
                case GameShared.Enums.CombatTargetKind.Boss:
                case GameShared.Enums.CombatTargetKind.Dummy:
                case GameShared.Enums.CombatTargetKind.Npc:
                    if (!target.RuntimeId.HasValue)
                        break;

                    var enemy = Monsters.FirstOrDefault(x => x.Id == target.RuntimeId.Value);
                    if (enemy is null)
                        break;

                    snapshot = new CombatTargetSnapshot(
                        enemy.Definition.Kind == EnemyKind.Boss ? GameShared.Enums.CombatTargetKind.Boss : target.Kind,
                        null,
                        enemy.Id,
                        enemy.Position,
                        enemy.IsAlive);
                    return true;

                case GameShared.Enums.CombatTargetKind.GroundPoint:
                    if (!target.GroundPosition.HasValue)
                        break;

                    snapshot = new CombatTargetSnapshot(
                        target.Kind,
                        null,
                        null,
                        Definition.ClampPosition(target.GroundPosition.Value),
                        true);
                    return true;
            }

            snapshot = default;
            return false;
        }
    }

    public PendingSkillExecution EnqueueSkillExecution(
        CombatTargetReference caster,
        Guid? casterPlayerId,
        Guid? casterCharacterId,
        long playerSkillId,
        int skillId,
        string skillCode,
        string skillGroupCode,
        int skillSlotIndex,
        SkillTargetType targetType,
        CombatStatSnapshot casterStats,
        CombatTargetReference? target,
        int castTimeMs,
        int travelTimeMs,
        DateTime utcNow)
    {
        lock (_sync)
        {
            var castStartedAtUtc = utcNow;
            var castCompletedAtUtc = utcNow.AddMilliseconds(Math.Max(0, castTimeMs));
            var impactAtUtc = castCompletedAtUtc.AddMilliseconds(Math.Max(0, travelTimeMs));
            var execution = new PendingSkillExecution(
                _nextSkillExecutionId++,
                caster,
                casterPlayerId,
                casterCharacterId,
                playerSkillId,
                skillId,
                skillCode,
                skillGroupCode,
                skillSlotIndex,
                targetType,
                casterStats,
                target,
                Math.Max(0, castTimeMs),
                Math.Max(0, travelTimeMs),
                castStartedAtUtc,
                castCompletedAtUtc,
                impactAtUtc);

            _pendingSkillExecutions.Add(execution);
            return execution;
        }
    }

    public void EnqueueSkillImpactResolved(SkillImpactResolvedRuntimeEvent impact)
    {
        lock (_sync)
        {
            _pendingSkillImpactResolutions.Enqueue(impact);
        }
    }

    public EnemyHealingApplicationResult ApplyEnemyHealing(int enemyRuntimeId, int healing, DateTime utcNow)
    {
        lock (_sync)
        {
            var enemy = Monsters.FirstOrDefault(x => x.Id == enemyRuntimeId);
            if (enemy is null)
                return new EnemyHealingApplicationResult(false, 0, 0, MessageCode.EnemyNotFound);

            var result = enemy.RestoreHp(healing, utcNow);
            if (result.Applied)
            {
                _pendingEnemyHpChanges.Enqueue(new EnemyHpChangedRuntimeEvent(
                    enemy.Id,
                    enemy.Hp,
                    enemy.MaxHp,
                    enemy.State));
            }

            return result;
        }
    }

    public bool TryApplyEnemyShield(int enemyRuntimeId, int amount, int? durationMs, DateTime utcNow)
    {
        lock (_sync)
        {
            var enemy = Monsters.FirstOrDefault(x => x.Id == enemyRuntimeId);
            if (enemy is null)
                return false;

            enemy.ApplyShield(amount, durationMs, utcNow);
            return true;
        }
    }

    public bool TryApplyEnemyStun(int enemyRuntimeId, int durationMs, DateTime utcNow)
    {
        lock (_sync)
        {
            var enemy = Monsters.FirstOrDefault(x => x.Id == enemyRuntimeId);
            if (enemy is null)
                return false;

            enemy.ApplyStun(durationMs, utcNow);
            return true;
        }
    }

    public bool TryApplyEnemyStatModifier(
        int enemyRuntimeId,
        CharacterStatType statType,
        decimal value,
        CombatValueType valueType,
        int? durationMs,
        DateTime utcNow)
    {
        lock (_sync)
        {
            var enemy = Monsters.FirstOrDefault(x => x.Id == enemyRuntimeId);
            if (enemy is null)
                return false;

            enemy.ApplyStatModifier(statType, value, valueType, durationMs, utcNow);
            return true;
        }
    }

    public bool TryGetMonster(int enemyRuntimeId, out MonsterEntity monster)
    {
        lock (_sync)
        {
            monster = Monsters.FirstOrDefault(x => x.Id == enemyRuntimeId)!;
            return monster != null;
        }
    }
}
