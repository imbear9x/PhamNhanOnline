using System.Collections.Concurrent;

namespace GameServer.World;

public sealed class MapManager
{
    private readonly MapCatalog _catalog;
    // mapId -> (instanceId -> instance)
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, MapInstance>> _maps = new();
    private int _nextInstanceId;

    public MapManager(MapCatalog catalog)
    {
        _catalog = catalog;
    }

    public IReadOnlyDictionary<int, ConcurrentDictionary<int, MapInstance>> Maps => _maps;

    public MapInstance? FindAvailableInstance(MapDefinition definition)
    {
        if (!_maps.TryGetValue(definition.MapId, out var instances))
            return null;

        foreach (var instance in instances.Values)
        {
            if (instance.PlayerCount < definition.MaxPlayersPerInstance)
                return instance;
        }

        return null;
    }

    public MapInstance CreateInstance(MapDefinition definition)
    {
        var instanceId = Interlocked.Increment(ref _nextInstanceId);
        var instance = new MapInstance(instanceId, definition);

        var instances = _maps.GetOrAdd(definition.MapId, _ => new ConcurrentDictionary<int, MapInstance>());
        instances[instanceId] = instance;

        return instance;
    }

    public MapInstance JoinInstance(MapDefinition definition, PlayerSession player)
    {
        if (TryGetInstance(definition.MapId, player.InstanceId, out var currentInstance))
        {
            currentInstance.AddPlayer(player);
            return currentInstance;
        }

        // Fast path: try available instance first.
        var instance = FindAvailableInstance(definition);
        if (instance is null)
            instance = CreateInstance(definition);

        if (!instance.AddPlayer(player))
        {
            // Race: instance filled between checks. Create a new one and join it.
            instance = CreateInstance(definition);
            if (!instance.AddPlayer(player))
                throw new InvalidOperationException("Unable to join a map instance.");
        }

        return instance;
    }

    public void RemovePlayer(PlayerSession player)
    {
        RemovePlayer(player.MapId, player.InstanceId, player);
    }

    public void RemovePlayer(int mapId, int instanceId, PlayerSession player)
    {
        if (!_maps.TryGetValue(mapId, out var instances))
            return;

        if (!instances.TryGetValue(instanceId, out var instance))
            return;

        instance.RemovePlayer(player);
    }

    public bool TryGetInstance(int mapId, int instanceId, out MapInstance instance)
    {
        if (instanceId != 0 &&
            _maps.TryGetValue(mapId, out var instances) &&
            instances.TryGetValue(instanceId, out var resolved))
        {
            instance = resolved;
            return true;
        }

        instance = null!;
        return false;
    }

    public MapDefinition ResolveDefinitionOrDefault(int? mapId) => _catalog.ResolveOrDefault(mapId);

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

