using System.Numerics;
using GameServer.Randomness;
using GameServer.Runtime;
using GameShared.Messages;

namespace GameServer.World;

public sealed class MonsterEntity
{
    private const float PositionEpsilon = 0.001f;

    private readonly object _sync = new();
    private readonly Dictionary<Guid, DamageContributionState> _contributions = new();
    private readonly CombatStatusCollection _combatStatuses = new();
    private readonly Vector2 _patrolCenterPosition;
    private readonly float _patrolRadius;

    private int _nextSkillIndex;
    private int _movementDecisionVersion;
    private bool _nextHorizontalTargetIsRight;
    private bool _missingAttackSkillLogged;
    private DateTime? _waitUntilUtc;
    private DateTime _lastMovementUpdateUtc;
    private Guid? _pendingAggroOverridePlayerId;

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
    public EnemyMovementMode MovementMode { get; private set; }
    public Vector2 MovementTargetPosition { get; private set; }
    public float MovementSpeed { get; private set; }
    public int MovementDecisionVersion => _movementDecisionVersion;

    public MonsterEntity(
        int id,
        int spawnGroupId,
        EnemyDefinition definition,
        MapEnemySpawnGroupDefinition spawnGroup,
        Vector2 spawnPosition,
        DateTime utcNow)
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
        _patrolCenterPosition = spawnGroup.CenterPosition;
        _patrolRadius = Math.Max(0f, spawnGroup.PatrolRadius);
        _lastMovementUpdateUtc = utcNow;
        MovementTargetPosition = spawnPosition;
    }

    public EnemyDamageApplicationResult ApplyDamage(Guid? playerId, int damage, DateTime utcNow)
    {
        if (damage <= 0)
            return new EnemyDamageApplicationResult(false, false, 0, Hp, MessageCode.EnemyAlreadyDead);

        lock (_sync)
        {
            if (State == EnemyRuntimeState.Dead)
                return new EnemyDamageApplicationResult(false, false, 0, Hp, MessageCode.EnemyAlreadyDead);

            UpdateMovementUnsafe(utcNow);

            var previousHp = Hp;
            var remainingDamage = _combatStatuses.AbsorbIncomingDamage(damage, utcNow, out _);
            Hp = Math.Max(0, Hp - remainingDamage);
            var appliedDamage = Math.Max(0, previousHp - Hp);
            State = EnemyRuntimeState.Combat;
            LastHitPlayerId = playerId;
            CombatTargetPlayerId = playerId;
            LastDamagedAtUtc = utcNow;
            NextAttackAtUtc ??= utcNow;
            _pendingAggroOverridePlayerId = playerId;

            if (playerId.HasValue && _contributions.TryGetValue(playerId.Value, out var existing))
            {
                _contributions[playerId.Value] = existing with
                {
                    DamageDealt = existing.DamageDealt + appliedDamage,
                    LastHitAtUtc = utcNow
                };
            }
            else if (playerId.HasValue)
            {
                _contributions[playerId.Value] = new DamageContributionState(playerId.Value, appliedDamage, utcNow);
            }

            if (Hp > 0)
                return new EnemyDamageApplicationResult(appliedDamage > 0, false, appliedDamage, Hp, MessageCode.None);

            State = EnemyRuntimeState.Dead;
            DiedAtUtc = utcNow;
            CombatTargetPlayerId = null;
            ClearMovementDecisionUnsafe();
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

            UpdateMovementUnsafe(utcNow);
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

            _combatStatuses.AddShield(amount, ResolveExpiresAtUtc(durationMs, utcNow), CombatStatusSourceType.Skill);
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

            _combatStatuses.AddStun(utcNow.AddMilliseconds(durationMs), CombatStatusSourceType.Skill);
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

            _combatStatuses.AddStatModifier(statType, value, valueType, ResolveExpiresAtUtc(durationMs, utcNow), CombatStatusSourceType.Skill);
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

    public bool AdvancePatrolMovement(DateTime utcNow)
    {
        lock (_sync)
        {
            if (State == EnemyRuntimeState.Dead)
            {
                _lastMovementUpdateUtc = utcNow;
                return false;
            }

            return UpdateMovementUnsafe(utcNow);
        }
    }

    public bool ShouldChooseNewPatrolDestination(DateTime utcNow)
    {
        lock (_sync)
        {
            if (State != EnemyRuntimeState.Patrol || MovementMode != EnemyMovementMode.None)
                return false;

            if (!_waitUntilUtc.HasValue)
                return true;

            return utcNow >= _waitUntilUtc.Value;
        }
    }

    public bool StartMovingTo(Vector2 targetPosition, float moveSpeed, DateTime utcNow)
    {
        lock (_sync)
        {
            if (State == EnemyRuntimeState.Dead)
                return false;

            UpdateMovementUnsafe(utcNow);
            var clampedTarget = targetPosition;
            if (Vector2.DistanceSquared(Position, clampedTarget) <= PositionEpsilon * PositionEpsilon ||
                moveSpeed <= 0f)
            {
                _waitUntilUtc = utcNow;
                return StopMovementUnsafe();
            }

            _waitUntilUtc = null;
            MovementMode = EnemyMovementMode.MoveToPoint;
            MovementTargetPosition = clampedTarget;
            MovementSpeed = moveSpeed;
            _movementDecisionVersion++;
            _lastMovementUpdateUtc = utcNow;
            return true;
        }
    }

    public bool WaitAtCurrentPosition(float waitSeconds, DateTime utcNow)
    {
        lock (_sync)
        {
            if (State == EnemyRuntimeState.Dead)
                return false;

            UpdateMovementUnsafe(utcNow);
            _waitUntilUtc = waitSeconds > 0f ? utcNow.AddSeconds(waitSeconds) : utcNow;
            return StopMovementUnsafe();
        }
    }

    public bool EnterCombat(Guid? targetPlayerId, DateTime utcNow)
    {
        lock (_sync)
        {
            if (State == EnemyRuntimeState.Dead)
                return false;

            UpdateMovementUnsafe(utcNow);
            var changed = State != EnemyRuntimeState.Combat || CombatTargetPlayerId != targetPlayerId;
            State = EnemyRuntimeState.Combat;
            CombatTargetPlayerId = targetPlayerId;
            NextAttackAtUtc ??= utcNow;
            _waitUntilUtc = null;
            return StopMovementUnsafe() || changed;
        }
    }

    public bool ReturnToPatrol(DateTime utcNow)
    {
        lock (_sync)
        {
            if (State == EnemyRuntimeState.Dead)
                return false;

            UpdateMovementUnsafe(utcNow);
            var changed = State != EnemyRuntimeState.Patrol || CombatTargetPlayerId.HasValue;
            State = EnemyRuntimeState.Patrol;
            CombatTargetPlayerId = null;
            NextAttackAtUtc = null;
            _waitUntilUtc ??= utcNow;
            return changed;
        }
    }

    public bool RestoreFullHealth(DateTime utcNow)
    {
        lock (_sync)
        {
            if (State == EnemyRuntimeState.Dead)
                return false;

            UpdateMovementUnsafe(utcNow);
            var hpChanged = Hp != Definition.MaxHp;
            Hp = Definition.MaxHp;
            State = EnemyRuntimeState.Patrol;
            LastDamagedAtUtc = utcNow;
            _contributions.Clear();
            LastHitPlayerId = null;
            CombatTargetPlayerId = null;
            NextAttackAtUtc = null;
            _pendingAggroOverridePlayerId = null;
            _waitUntilUtc = utcNow;
            var movementChanged = StopMovementUnsafe();
            return hpChanged || movementChanged;
        }
    }

    public bool HasCombatTarget()
    {
        lock (_sync)
        {
            return State == EnemyRuntimeState.Combat && CombatTargetPlayerId.HasValue;
        }
    }

    public bool TryConsumeAttackWindow(DateTime utcNow, out EnemySkillLoadoutDefinition? skill)
    {
        skill = null;
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
            if (Definition.Skills.Count > 0)
            {
                if (_nextSkillIndex >= Definition.Skills.Count)
                    _nextSkillIndex = 0;

                skill = Definition.Skills[_nextSkillIndex];
                _nextSkillIndex = (_nextSkillIndex + 1) % Definition.Skills.Count;
            }

            return true;
        }
    }

    public bool TryMarkMissingAttackSkillLogged()
    {
        lock (_sync)
        {
            if (_missingAttackSkillLogged)
                return false;

            _missingAttackSkillLogged = true;
            return true;
        }
    }

    public CombatStatSnapshot CaptureCombatStatsSnapshot(DateTime utcNow)
    {
        lock (_sync)
        {
            return new CombatStatSnapshot(
                CombatStatMath.ApplyModifiers(Definition.MaxHp, _combatStatuses.GetStatModifierAggregate(CharacterStatType.MaxHp, utcNow)),
                0,
                0,
                CombatStatMath.ApplyModifiers(Definition.BaseAttack, _combatStatuses.GetStatModifierAggregate(CharacterStatType.Attack, utcNow)),
                CombatStatMath.ApplyModifiers((int)Math.Round(Definition.BaseMoveSpeed), _combatStatuses.GetStatModifierAggregate(CharacterStatType.Speed, utcNow)),
                0,
                0);
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

    public bool IsWithinPatrolArea()
    {
        lock (_sync)
        {
            return IsWithinPatrolAreaUnsafe(Position);
        }
    }

    public Vector2 GetPatrolCenterPosition()
    {
        return _patrolCenterPosition;
    }

    public bool TryGetNextHorizontalPatrolTarget(out Vector2 target)
    {
        lock (_sync)
        {
            if (_patrolRadius <= 0f)
            {
                target = default;
                return false;
            }

            var direction = _nextHorizontalTargetIsRight ? 1f : -1f;
            target = new Vector2(
                _patrolCenterPosition.X + (_patrolRadius * direction),
                _patrolCenterPosition.Y);
            _nextHorizontalTargetIsRight = !_nextHorizontalTargetIsRight;
            return true;
        }
    }

    public Guid? PeekPendingAggroOverridePlayerId()
    {
        lock (_sync)
        {
            return _pendingAggroOverridePlayerId;
        }
    }

    public void ClearPendingAggroOverride(Guid? playerId)
    {
        lock (_sync)
        {
            if (_pendingAggroOverridePlayerId == playerId)
                _pendingAggroOverridePlayerId = null;
        }
    }

    public float RollPatrolPauseSeconds(IRandomNumberProvider random)
    {
        var minPause = Math.Max(0f, Definition.PatrolPauseSecondsMin);
        var maxPause = Math.Max(minPause, Definition.PatrolPauseSecondsMax);
        if (maxPause <= minPause + 0.001f)
            return minPause;

        var raw = random.NextInt(10_000) / 10_000f;
        return minPause + ((maxPause - minPause) * raw);
    }

    private bool UpdateMovementUnsafe(DateTime utcNow)
    {
        if (State == EnemyRuntimeState.Dead)
        {
            _lastMovementUpdateUtc = utcNow;
            return false;
        }

        var deltaSeconds = Math.Max(0d, (utcNow - _lastMovementUpdateUtc).TotalSeconds);
        _lastMovementUpdateUtc = utcNow;
        if (MovementMode != EnemyMovementMode.MoveToPoint || MovementSpeed <= 0f || deltaSeconds <= 0d)
            return false;

        var toTarget = MovementTargetPosition - Position;
        var distanceRemaining = toTarget.Length();
        if (distanceRemaining <= PositionEpsilon)
        {
            Position = MovementTargetPosition;
            return StopMovementUnsafe();
        }

        var movementStep = (float)(MovementSpeed * deltaSeconds);
        if (movementStep >= distanceRemaining)
        {
            Position = MovementTargetPosition;
            return StopMovementUnsafe();
        }

        Position += Vector2.Normalize(toTarget) * movementStep;
        return false;
    }

    private bool StopMovementUnsafe()
    {
        if (MovementMode == EnemyMovementMode.None &&
            Vector2.DistanceSquared(MovementTargetPosition, Position) <= PositionEpsilon * PositionEpsilon &&
            MovementSpeed <= 0f)
        {
            return false;
        }

        MovementMode = EnemyMovementMode.None;
        MovementTargetPosition = Position;
        MovementSpeed = 0f;
        _movementDecisionVersion++;
        return true;
    }

    private void ClearMovementDecisionUnsafe()
    {
        MovementMode = EnemyMovementMode.None;
        MovementTargetPosition = Position;
        MovementSpeed = 0f;
        _waitUntilUtc = null;
        _movementDecisionVersion++;
    }

    private bool IsWithinPatrolAreaUnsafe(Vector2 position)
    {
        var radius = _patrolRadius;
        if (radius <= 0f)
            return Vector2.DistanceSquared(position, _patrolCenterPosition) <= PositionEpsilon * PositionEpsilon;

        return Vector2.DistanceSquared(position, _patrolCenterPosition) <= radius * radius;
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
