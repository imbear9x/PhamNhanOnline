using System.Collections.Concurrent;
using GameServer.DTO;
using GameServer.Runtime;

namespace GameServer.World;

public sealed class WorldManager
{
    private readonly ConcurrentDictionary<Guid, PlayerSession> _onlinePlayers = new();
    public event Action<PlayerSession>? PlayerRemoving;

    public IReadOnlyDictionary<Guid, PlayerSession> OnlinePlayers => _onlinePlayers;
    public MapManager MapManager { get; }

    public WorldManager(MapManager mapManager)
    {
        MapManager = mapManager;
    }

    public PlayerSession AddOrUpdatePlayer(
        Guid playerId,
        int connectionId,
        CharacterDto character,
        CharacterBaseStatsDto baseStats,
        CharacterCurrentStateDto currentState)
    {
        return _onlinePlayers.AddOrUpdate(
            playerId,
            id => new PlayerSession(id, connectionId, character, new CharacterRuntimeState(baseStats, currentState)),
            (_, existing) =>
            {
                existing.UpdateConnection(connectionId);
                existing.UpdateCharacter(character);
                existing.SynchronizeFromCurrentState(existing.RuntimeState.CaptureSnapshot().CurrentState);
                return existing;
            });
    }

    public void RemovePlayer(Guid playerId)
    {
        if (_onlinePlayers.TryRemove(playerId, out var session))
        {
            PlayerRemoving?.Invoke(session);
            session.IsConnected = false;
            MapManager.RemovePlayer(session);
        }
    }

    public bool IsOwnedByConnection(Guid playerId, int connectionId)
    {
        return _onlinePlayers.TryGetValue(playerId, out var session) &&
               session.ConnectionId == connectionId;
    }

    public PlayerSession GetPlayer(Guid playerId)
    {
        if (_onlinePlayers.TryGetValue(playerId, out var session))
            return session;

        throw new KeyNotFoundException($"Player not online: {playerId}");
    }

    public bool TryGetPlayer(Guid playerId, out PlayerSession session)
    {
        return _onlinePlayers.TryGetValue(playerId, out session!);
    }

    public IReadOnlyCollection<PlayerSession> GetOnlinePlayersSnapshot()
    {
        return _onlinePlayers.Values.ToList();
    }
}
