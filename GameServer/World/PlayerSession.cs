using System.Numerics;
using GameServer.DTO;

namespace GameServer.World;

public sealed class PlayerSession
{
    private readonly object _sync = new();

    public Guid PlayerId { get; }
    public CharacterDto CharacterData { get; }

    public int MapId { get; internal set; }
    public int InstanceId { get; internal set; }

    public Vector2 Position { get; private set; }
    public bool IsConnected { get; internal set; }

    public PlayerSession(Guid playerId, CharacterDto characterData)
    {
        PlayerId = playerId;
        CharacterData = characterData;
        IsConnected = true;
        Position = Vector2.Zero;
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
}

