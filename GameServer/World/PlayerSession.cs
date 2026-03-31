using System.Numerics;
using GameServer.DTO;
using GameServer.Runtime;

namespace GameServer.World;

public sealed class PlayerSession
{
    private readonly object _sync = new();
    private readonly HashSet<Guid> _visibleCharacterIds = new();
    private readonly Dictionary<long, DateTime> _skillCooldownsByPlayerSkillId = new();
    private int _lastReportedRemainingLifespan = int.MinValue;
    private bool _lifespanExpiredProcessed;
    private bool _characterActionsRestricted;
    private (int ExecutionId, long PlayerSkillId, DateTime CastCompletedAtUtc)? _activeSkillCast;
    private MapEntryContext _lastMapEntryContext = new(MapEntryReason.Unknown, null, null, Vector2.Zero);

    public Guid PlayerId { get; }
    public int ConnectionId { get; private set; }
    public CharacterDto CharacterData { get; private set; }
    public CharacterRuntimeState RuntimeState { get; }

    public int MapId { get; internal set; }
    public int InstanceId { get; internal set; }
    public int ZoneIndex { get; internal set; }
    public CombatStatusCollection CombatStatuses { get; } = new();

    public Vector2 Position { get; private set; }
    public bool IsConnected { get; internal set; }
    public bool IsLifespanExpiredProcessed
    {
        get
        {
            lock (_sync)
            {
                return _lifespanExpiredProcessed;
            }
        }
    }

    public bool AreCharacterActionsRestricted
    {
        get
        {
            lock (_sync)
            {
                return _characterActionsRestricted;
            }
        }
    }

    public bool IsCastingSkill
    {
        get
        {
            lock (_sync)
            {
                return _activeSkillCast is not null;
            }
        }
    }

    public MapEntryContext LastMapEntryContext
    {
        get
        {
            lock (_sync)
            {
                return _lastMapEntryContext;
            }
        }
    }

    public PlayerSession(
        Guid playerId,
        int connectionId,
        CharacterDto characterData,
        CharacterRuntimeState runtimeState)
    {
        PlayerId = playerId;
        ConnectionId = connectionId;
        CharacterData = characterData;
        RuntimeState = runtimeState;
        IsConnected = true;
        Position = Vector2.Zero;
        SynchronizeFromCurrentState(runtimeState.CaptureSnapshot().CurrentState);
    }

    public bool IsStunned(DateTime utcNow) => CombatStatuses.IsStunned(utcNow);

    public CombatStatSnapshot CaptureCombatStatsSnapshot(DateTime utcNow)
    {
        var snapshot = RuntimeState.CaptureSnapshot();
        var baseStats = snapshot.BaseStats;
        return new CombatStatSnapshot(
            CombatStatMath.ApplyModifiers(baseStats.GetEffectiveHp(), CombatStatuses.GetStatModifierAggregate(CharacterStatType.MaxHp, utcNow)),
            CombatStatMath.ApplyModifiers(baseStats.GetEffectiveMp(), CombatStatuses.GetStatModifierAggregate(CharacterStatType.MaxMp, utcNow)),
            CombatStatMath.ApplyModifiers(baseStats.GetEffectiveStamina(), CombatStatuses.GetStatModifierAggregate(CharacterStatType.MaxStamina, utcNow)),
            CombatStatMath.ApplyModifiers(baseStats.GetEffectiveAttack(), CombatStatuses.GetStatModifierAggregate(CharacterStatType.Attack, utcNow)),
            CombatStatMath.ApplyModifiers(baseStats.GetEffectiveSpeed(), CombatStatuses.GetStatModifierAggregate(CharacterStatType.Speed, utcNow)),
            CombatStatMath.ApplyModifiers(baseStats.GetEffectiveSpiritualSense(), CombatStatuses.GetStatModifierAggregate(CharacterStatType.SpiritualSense, utcNow)),
            CombatStatMath.ApplyModifiers(baseStats.GetEffectiveFortune(), CombatStatuses.GetStatModifierAggregate(CharacterStatType.Fortune, utcNow)));
    }

    public void UpdateConnection(int connectionId)
    {
        lock (_sync)
        {
            ConnectionId = connectionId;
            IsConnected = true;
        }
    }

    public void UpdateCharacter(CharacterDto characterData)
    {
        lock (_sync)
        {
            CharacterData = characterData;
        }
    }

    public void Move(Vector2 direction)
    {
        lock (_sync)
        {
            Position += direction;
        }
    }

    public void SetPosition(Vector2 position)
    {
        lock (_sync)
        {
            Position = position;
        }
    }

    public void SynchronizeFromCurrentState(CharacterCurrentStateDto currentState)
    {
        lock (_sync)
        {
            MapId = currentState.CurrentMapId ?? 0;
            ZoneIndex = currentState.CurrentZoneIndex;
            Position = new Vector2(currentState.CurrentPosX, currentState.CurrentPosY);
        }
    }

    public void SetMapEntryContext(MapEntryContext entryContext)
    {
        lock (_sync)
        {
            _lastMapEntryContext = entryContext;
        }
    }

    public bool TryUpdateReportedRemainingLifespan(int remainingLifespan)
    {
        lock (_sync)
        {
            if (_lastReportedRemainingLifespan == remainingLifespan)
                return false;

            _lastReportedRemainingLifespan = remainingLifespan;
            return true;
        }
    }

    public IReadOnlyCollection<Guid> GetVisibleCharacterIdsSnapshot()
    {
        lock (_sync)
        {
            return _visibleCharacterIds.ToArray();
        }
    }

    public bool AddVisibleCharacter(Guid characterId)
    {
        lock (_sync)
        {
            return _visibleCharacterIds.Add(characterId);
        }
    }

    public bool RemoveVisibleCharacter(Guid characterId)
    {
        lock (_sync)
        {
            return _visibleCharacterIds.Remove(characterId);
        }
    }

    public void ClearVisibleCharacters()
    {
        lock (_sync)
        {
            _visibleCharacterIds.Clear();
        }
    }

    public void MarkLifespanExpiredProcessed()
    {
        lock (_sync)
        {
            _lifespanExpiredProcessed = true;
        }
    }

    public void SetCharacterActionsRestricted(bool restricted)
    {
        lock (_sync)
        {
            _characterActionsRestricted = restricted;
        }
    }

    public bool TryBeginSkillCast(int executionId, long playerSkillId, DateTime castCompletedAtUtc, DateTime cooldownUntilUtc)
    {
        lock (_sync)
        {
            if (_activeSkillCast is not null)
                return false;

            _activeSkillCast = (executionId, playerSkillId, castCompletedAtUtc);
            _skillCooldownsByPlayerSkillId[playerSkillId] = cooldownUntilUtc;
            return true;
        }
    }

    public bool IsSkillOnCooldown(long playerSkillId, DateTime utcNow, out DateTime cooldownUntilUtc)
    {
        lock (_sync)
        {
            if (!_skillCooldownsByPlayerSkillId.TryGetValue(playerSkillId, out cooldownUntilUtc))
                return false;

            if (utcNow >= cooldownUntilUtc)
            {
                _skillCooldownsByPlayerSkillId.Remove(playerSkillId);
                cooldownUntilUtc = default;
                return false;
            }

            return true;
        }
    }

    public void CompleteSkillCast(int executionId)
    {
        lock (_sync)
        {
            if (_activeSkillCast?.ExecutionId != executionId)
                return;

            _activeSkillCast = null;
        }
    }
}
