using GameServer.DTO;

namespace GameServer.Runtime;

public sealed class CharacterRuntimeState
{
    private readonly object _sync = new();

    private CharacterBaseStatsDto _baseStats;
    private CharacterCurrentStateDto _currentState;
    private long _baseStatsVersion;
    private long _currentStateVersion;
    private CharacterRuntimeDirtyFlags _dirtyFlags;

    public CharacterRuntimeState(CharacterBaseStatsDto baseStats, CharacterCurrentStateDto currentState)
    {
        _baseStats = baseStats;
        _currentState = currentState;
    }

    public CharacterRuntimeSnapshot CaptureSnapshot()
    {
        lock (_sync)
        {
            return new CharacterRuntimeSnapshot(
                _baseStats,
                _currentState,
                _baseStatsVersion,
                _currentStateVersion,
                _dirtyFlags);
        }
    }

    public CharacterRuntimeSnapshot UpdateBaseStats(Func<CharacterBaseStatsDto, CharacterBaseStatsDto> update)
    {
        lock (_sync)
        {
            _baseStats = update(_baseStats);
            _baseStatsVersion++;
            _dirtyFlags |= CharacterRuntimeDirtyFlags.BaseStats;
            return CreateSnapshotNoLock();
        }
    }

    public CharacterRuntimeSnapshot UpdateCurrentState(
        Func<CharacterCurrentStateDto, CharacterCurrentStateDto> update,
        bool markDirty = true)
    {
        lock (_sync)
        {
            _currentState = update(_currentState);
            _currentStateVersion++;
            if (markDirty)
                _dirtyFlags |= CharacterRuntimeDirtyFlags.CurrentState;
            return CreateSnapshotNoLock();
        }
    }

    public void MarkBaseStatsPersisted(long version)
    {
        lock (_sync)
        {
            if (_baseStatsVersion == version)
                _dirtyFlags &= ~CharacterRuntimeDirtyFlags.BaseStats;
        }
    }

    public void MarkCurrentStatePersisted(long version, DateTime savedAtUtc)
    {
        lock (_sync)
        {
            if (_currentStateVersion != version)
                return;

            _currentState = _currentState with { LastSavedAt = savedAtUtc };
            _dirtyFlags &= ~CharacterRuntimeDirtyFlags.CurrentState;
        }
    }

    private CharacterRuntimeSnapshot CreateSnapshotNoLock()
    {
        return new CharacterRuntimeSnapshot(
            _baseStats,
            _currentState,
            _baseStatsVersion,
            _currentStateVersion,
            _dirtyFlags);
    }
}
