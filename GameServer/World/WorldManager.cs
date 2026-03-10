using System.Collections.Concurrent;
using GameServer.DTO;

namespace GameServer.World;

public sealed class WorldManager
{
    private readonly ConcurrentDictionary<Guid, PlayerSession> _onlinePlayers = new();

    public IReadOnlyDictionary<Guid, PlayerSession> OnlinePlayers => _onlinePlayers;
    public MapManager MapManager { get; }

    public WorldManager(MapManager mapManager)
    {
        MapManager = mapManager;
    }

    public PlayerSession AddPlayer(Guid playerId, CharacterDto character)
    {
        return _onlinePlayers.GetOrAdd(playerId, id => new PlayerSession(id, character));
    }

    public void RemovePlayer(Guid playerId)
    {
        if (_onlinePlayers.TryRemove(playerId, out var session))
        {
            session.IsConnected = false;
            MapManager.RemovePlayer(session);
        }
    }

    public PlayerSession GetPlayer(Guid playerId)
    {
        if (_onlinePlayers.TryGetValue(playerId, out var session))
            return session;

        throw new KeyNotFoundException($"Player not online: {playerId}");
    }
}

