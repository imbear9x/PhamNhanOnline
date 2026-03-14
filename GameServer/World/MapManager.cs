using System.Collections.Concurrent;

namespace GameServer.World;

public sealed class MapManager
{
    private readonly MapCatalog _catalog;
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, MapInstance>> _maps = new();
    private int _nextInstanceId;

    public MapManager(MapCatalog catalog)
    {
        _catalog = catalog;
    }

    public IReadOnlyDictionary<int, ConcurrentDictionary<int, MapInstance>> Maps => _maps;

    public MapInstance JoinInstance(MapDefinition definition, PlayerSession player)
    {
        if (definition.IsPrivatePerPlayer)
            return JoinPrivateInstance(definition, player);

        return JoinPublicInstance(definition, player);
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
        if (instance.PlayerCount == 0)
        {
            instances.TryRemove(instanceId, out _);
            if (instances.IsEmpty)
                _maps.TryRemove(mapId, out _);
        }
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

    private MapInstance JoinPrivateInstance(MapDefinition definition, PlayerSession player)
    {
        var instances = _maps.GetOrAdd(definition.MapId, _ => new ConcurrentDictionary<int, MapInstance>());
        foreach (var instance in instances.Values)
        {
            if (instance.OwnerPlayerId != player.PlayerId)
                continue;

            if (!instance.AddPlayer(player))
                throw new InvalidOperationException("Unable to rejoin private map instance.");

            return instance;
        }

        var created = CreateInstance(definition, zoneIndex: 0, ownerPlayerId: player.PlayerId);
        if (!created.AddPlayer(player))
            throw new InvalidOperationException("Unable to join private map instance.");

        return created;
    }

    private MapInstance JoinPublicInstance(MapDefinition definition, PlayerSession player)
    {
        if (player.ZoneIndex > 0 && TryGetPublicInstanceByZone(definition.MapId, player.ZoneIndex, out var preferred))
        {
            if (preferred.AddPlayer(player))
                return preferred;
        }

        var instance = FindAvailablePublicInstance(definition);
        if (instance is null)
            instance = CreateNextPublicInstance(definition);

        if (!instance.AddPlayer(player))
        {
            instance = FindAvailablePublicInstance(definition) ?? CreateNextPublicInstance(definition);
            if (!instance.AddPlayer(player))
                throw new InvalidOperationException("Unable to join a public map zone.");
        }

        return instance;
    }

    private MapInstance? FindAvailablePublicInstance(MapDefinition definition)
    {
        if (!_maps.TryGetValue(definition.MapId, out var instances))
            return null;

        foreach (var instance in instances.Values.OrderBy(x => x.ZoneIndex))
        {
            if (instance.IsPrivate)
                continue;

            if (instance.PlayerCount < definition.MaxPlayersPerZone)
                return instance;
        }

        return null;
    }

    private bool TryGetPublicInstanceByZone(int mapId, int zoneIndex, out MapInstance instance)
    {
        instance = null!;
        if (!_maps.TryGetValue(mapId, out var instances))
            return false;

        foreach (var candidate in instances.Values)
        {
            if (!candidate.IsPrivate && candidate.ZoneIndex == zoneIndex)
            {
                instance = candidate;
                return true;
            }
        }

        return false;
    }

    private MapInstance CreateNextPublicInstance(MapDefinition definition)
    {
        if (definition.MaxPublicZoneCount <= 0)
            throw new InvalidOperationException($"Map {definition.MapId} does not allow public zones.");

        var usedZones = new HashSet<int>();
        if (_maps.TryGetValue(definition.MapId, out var existing))
        {
            foreach (var instance in existing.Values)
            {
                if (!instance.IsPrivate)
                    usedZones.Add(instance.ZoneIndex);
            }
        }

        for (var zoneIndex = 1; zoneIndex <= definition.MaxPublicZoneCount; zoneIndex++)
        {
            if (usedZones.Contains(zoneIndex))
                continue;

            return CreateInstance(definition, zoneIndex, ownerPlayerId: null);
        }

        throw new InvalidOperationException($"No public zone slot is available for map {definition.MapId}.");
    }

    private MapInstance CreateInstance(MapDefinition definition, int zoneIndex, Guid? ownerPlayerId)
    {
        var instanceId = Interlocked.Increment(ref _nextInstanceId);
        var instance = new MapInstance(instanceId, zoneIndex, definition, ownerPlayerId);
        var instances = _maps.GetOrAdd(definition.MapId, _ => new ConcurrentDictionary<int, MapInstance>());
        instances[instanceId] = instance;
        return instance;
    }
}
