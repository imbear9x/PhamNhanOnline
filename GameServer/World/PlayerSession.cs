using System.Numerics;
using GameServer.DTO;
using GameServer.Runtime;

namespace GameServer.World;

public sealed class PlayerSession
{
    private readonly object _sync = new();
    private readonly HashSet<Guid> _visibleCharacterIds = new();
    private int _lastReportedRemainingLifespan = int.MinValue;
    private bool _lifespanExpiredProcessed;
    private bool _characterActionsRestricted;

    public Guid PlayerId { get; }
    public int ConnectionId { get; private set; }
    public CharacterDto CharacterData { get; private set; }
    public CharacterRuntimeState RuntimeState { get; }

    public int MapId { get; internal set; }
    public int InstanceId { get; internal set; }

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
            Position = new Vector2(currentState.CurrentPosX, currentState.CurrentPosY);
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
}
