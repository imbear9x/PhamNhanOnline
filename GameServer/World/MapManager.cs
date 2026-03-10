using System.Collections.Concurrent;

namespace GameServer.World;

public sealed class MapManager
{
    // mapId -> (instanceId -> instance)
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, MapInstance>> _maps = new();
    private int _nextInstanceId;

    public IReadOnlyDictionary<int, ConcurrentDictionary<int, MapInstance>> Maps => _maps;

    public MapInstance? FindAvailableInstance(int mapId)
    {
        if (!_maps.TryGetValue(mapId, out var instances))
            return null;

        foreach (var instance in instances.Values)
        {
            if (instance.PlayerCount < MapInstance.MaxPlayers)
                return instance;
        }

        return null;
    }

    public MapInstance CreateInstance(int mapId)
    {
        var instanceId = Interlocked.Increment(ref _nextInstanceId);
        var instance = new MapInstance(instanceId, mapId);

        var instances = _maps.GetOrAdd(mapId, _ => new ConcurrentDictionary<int, MapInstance>());
        instances[instanceId] = instance;

        return instance;
    }

    public MapInstance JoinInstance(int mapId, PlayerSession player)
    {
        // Fast path: try available instance first.
        var instance = FindAvailableInstance(mapId);
        if (instance is null)
            instance = CreateInstance(mapId);

        if (!instance.AddPlayer(player))
        {
            // Race: instance filled between checks. Create a new one and join it.
            instance = CreateInstance(mapId);
            if (!instance.AddPlayer(player))
                throw new InvalidOperationException("Unable to join a map instance.");
        }

        return instance;
    }

    public void RemovePlayer(PlayerSession player)
    {
        if (!_maps.TryGetValue(player.MapId, out var instances))
            return;

        if (!instances.TryGetValue(player.InstanceId, out var instance))
            return;

        instance.RemovePlayer(player);
    }

    public IReadOnlyCollection<MapInstance> GetAllInstancesSnapshot()
    {
        var result = new List<MapInstance>();
        foreach (var perMap in _maps.Values)
        {
            result.AddRange(perMap.Values);
        }

        return result;
    }
}

