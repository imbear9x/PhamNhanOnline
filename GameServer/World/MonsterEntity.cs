using System.Numerics;
using GameServer.Runtime;
using GameShared.Messages;

namespace GameServer.World;

public sealed class MonsterEntity
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, DamageContributionState> _contributions = new();

    public int Id { get; }
    public int SpawnGroupId { get; }
    public EnemyDefinition Definition { get; }
    public Vector2 SpawnPosition { get; }
    public Vector2 Position { get; private set; }
    public int Hp { get; private set; }
    public int MaxHp => Definition.MaxHp;
    public bool IsAlive => State != EnemyRuntimeState.Dead;
    public EnemyRuntimeState State { get; private set; }
    public Guid? LastHitPlayerId { get; private set; }
    public Guid? CombatTargetPlayerId { get; private set; }
    public DateTime SpawnedAtUtc { get; private set; }
    public DateTime? DiedAtUtc { get; private set; }
    public DateTime? LastDamagedAtUtc { get; private set; }
    public DateTime? NextAttackAtUtc { get; private set; }

    public MonsterEntity(int id, int spawnGroupId, EnemyDefinition definition, Vector2 spawnPosition, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (definition.MaxHp <= 0)
            throw new ArgumentOutOfRangeException(nameof(definition), "Enemy max HP must be positive.");

        Id = id;
        SpawnGroupId = spawnGroupId;
        Definition = definition;
        SpawnPosition = spawnPosition;
        Position = spawnPosition;
        Hp = definition.MaxHp;
        State = EnemyRuntimeState.Patrol;
        SpawnedAtUtc = utcNow;
    }

    public EnemyDamageApplicationResult ApplyDamage(Guid playerId, int damage, DateTime utcNow)
    {
        if (damage <= 0)
            return new EnemyDamageApplicationResult(false, false, Hp, MessageCode.EnemyAlreadyDead);

        lock (_sync)
        {
            if (State == EnemyRuntimeState.Dead)
                return new EnemyDamageApplicationResult(false, false, Hp, MessageCode.EnemyAlreadyDead);

            Hp = Math.Max(0, Hp - damage);
            State = EnemyRuntimeState.Combat;
            LastHitPlayerId = playerId;
            CombatTargetPlayerId = playerId;
            LastDamagedAtUtc = utcNow;
            NextAttackAtUtc ??= utcNow;

            if (_contributions.TryGetValue(playerId, out var existing))
            {
                _contributions[playerId] = existing with
                {
                    DamageDealt = existing.DamageDealt + damage,
                    LastHitAtUtc = utcNow
                };
            }
            else
            {
                _contributions[playerId] = new DamageContributionState(playerId, damage, utcNow);
            }

            if (Hp > 0)
                return new EnemyDamageApplicationResult(true, false, Hp, MessageCode.None);

            State = EnemyRuntimeState.Dead;
            DiedAtUtc = utcNow;
            return new EnemyDamageApplicationResult(true, true, 0, MessageCode.None);
        }
    }

    public void ReturnToPatrol()
    {
        lock (_sync)
        {
            if (State == EnemyRuntimeState.Dead)
                return;

            State = EnemyRuntimeState.Patrol;
            CombatTargetPlayerId = null;
            NextAttackAtUtc = null;
        }
    }

    public void RestoreFullHealth(DateTime utcNow)
    {
        lock (_sync)
        {
            if (State == EnemyRuntimeState.Dead)
                return;

            Hp = Definition.MaxHp;
            State = EnemyRuntimeState.Patrol;
            LastDamagedAtUtc = utcNow;
            _contributions.Clear();
            LastHitPlayerId = null;
            CombatTargetPlayerId = null;
            NextAttackAtUtc = null;
        }
    }

    public bool HasCombatTarget()
    {
        lock (_sync)
        {
            return State == EnemyRuntimeState.Combat && CombatTargetPlayerId.HasValue;
        }
    }

    public bool TryConsumeAttackWindow(DateTime utcNow)
    {
        lock (_sync)
        {
            if (State != EnemyRuntimeState.Combat || !CombatTargetPlayerId.HasValue)
                return false;

            if (NextAttackAtUtc.HasValue && utcNow < NextAttackAtUtc.Value)
                return false;

            var intervalMs = Math.Max(250, Definition.MinimumSkillIntervalMs);
            NextAttackAtUtc = utcNow.AddMilliseconds(intervalMs);
            return true;
        }
    }

    public IReadOnlyList<RewardTargetSnapshot> CaptureContributionsSnapshot()
    {
        lock (_sync)
        {
            return _contributions.Values
                .OrderByDescending(x => x.DamageDealt)
                .ThenBy(x => x.LastHitAtUtc)
                .Select(x => new RewardTargetSnapshot(x.PlayerId, x.DamageDealt, x.LastHitAtUtc))
                .ToArray();
        }
    }

    private readonly record struct DamageContributionState(
        Guid PlayerId,
        int DamageDealt,
        DateTime LastHitAtUtc);
}

public readonly record struct EnemyDamageApplicationResult(
    bool Applied,
    bool IsKilled,
    int RemainingHp,
    MessageCode Code);
