using System.Numerics;
using GameServer.Runtime;
using GameShared.Messages;

namespace GameServer.World;

public sealed class MonsterEntity
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, DamageContributionState> _contributions = new();
    private readonly CombatStatusCollection _combatStatuses = new();

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
    public CombatStatusCollection CombatStatuses => _combatStatuses;

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
            return new EnemyDamageApplicationResult(false, false, 0, Hp, MessageCode.EnemyAlreadyDead);

        lock (_sync)
        {
            if (State == EnemyRuntimeState.Dead)
                return new EnemyDamageApplicationResult(false, false, 0, Hp, MessageCode.EnemyAlreadyDead);

            var previousHp = Hp;
            var remainingDamage = _combatStatuses.AbsorbIncomingDamage(damage, utcNow, out _);
            Hp = Math.Max(0, Hp - remainingDamage);
            var appliedDamage = Math.Max(0, previousHp - Hp);
            State = EnemyRuntimeState.Combat;
            LastHitPlayerId = playerId;
            CombatTargetPlayerId = playerId;
            LastDamagedAtUtc = utcNow;
            NextAttackAtUtc ??= utcNow;

            if (_contributions.TryGetValue(playerId, out var existing))
            {
                _contributions[playerId] = existing with
                {
                    DamageDealt = existing.DamageDealt + appliedDamage,
                    LastHitAtUtc = utcNow
                };
            }
            else
            {
                _contributions[playerId] = new DamageContributionState(playerId, appliedDamage, utcNow);
            }

            if (Hp > 0)
                return new EnemyDamageApplicationResult(appliedDamage > 0, false, appliedDamage, Hp, MessageCode.None);

            State = EnemyRuntimeState.Dead;
            DiedAtUtc = utcNow;
            return new EnemyDamageApplicationResult(appliedDamage > 0, true, appliedDamage, 0, MessageCode.None);
        }
    }

    public EnemyHealingApplicationResult RestoreHp(int amount, DateTime utcNow)
    {
        if (amount <= 0)
            return new EnemyHealingApplicationResult(false, 0, Hp, MessageCode.None);

        lock (_sync)
        {
            if (State == EnemyRuntimeState.Dead)
                return new EnemyHealingApplicationResult(false, 0, Hp, MessageCode.EnemyAlreadyDead);

            var previousHp = Hp;
            Hp = Math.Clamp(Hp + amount, 0, Definition.MaxHp);
            LastDamagedAtUtc = utcNow;
            return new EnemyHealingApplicationResult(Hp != previousHp, Hp - previousHp, Hp, MessageCode.None);
        }
    }

    public void ApplyShield(int amount, int? durationMs, DateTime utcNow)
    {
        lock (_sync)
        {
            if (State == EnemyRuntimeState.Dead)
                return;

            _combatStatuses.AddShield(amount, ResolveExpiresAtUtc(durationMs, utcNow));
        }
    }

    public void ApplyStun(int durationMs, DateTime utcNow)
    {
        if (durationMs <= 0)
            return;

        lock (_sync)
        {
            if (State == EnemyRuntimeState.Dead)
                return;

            _combatStatuses.AddStun(utcNow.AddMilliseconds(durationMs));
            NextAttackAtUtc = utcNow.AddMilliseconds(durationMs);
        }
    }

    public void ApplyStatModifier(
        CharacterStatType statType,
        decimal value,
        CombatValueType valueType,
        int? durationMs,
        DateTime utcNow)
    {
        lock (_sync)
        {
            if (State == EnemyRuntimeState.Dead)
                return;

            _combatStatuses.AddStatModifier(statType, value, valueType, ResolveExpiresAtUtc(durationMs, utcNow));
        }
    }

    public bool IsStunned(DateTime utcNow)
    {
        lock (_sync)
        {
            return _combatStatuses.IsStunned(utcNow);
        }
    }

    public int GetEffectiveAttack(DateTime utcNow)
    {
        lock (_sync)
        {
            return CombatStatMath.ApplyModifiers(
                Definition.BaseAttack,
                _combatStatuses.GetStatModifierAggregate(CharacterStatType.Attack, utcNow));
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

            if (_combatStatuses.IsStunned(utcNow))
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

    private static DateTime? ResolveExpiresAtUtc(int? durationMs, DateTime utcNow)
    {
        return durationMs is > 0
            ? utcNow.AddMilliseconds(durationMs.Value)
            : null;
    }
}

public readonly record struct EnemyDamageApplicationResult(
    bool Applied,
    bool IsKilled,
    int AppliedDamage,
    int RemainingHp,
    MessageCode Code);

public readonly record struct EnemyHealingApplicationResult(
    bool Applied,
    int HealingApplied,
    int CurrentHp,
    MessageCode Code);
