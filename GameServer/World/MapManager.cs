using System.Collections.Concurrent;

namespace GameServer.World;

public sealed class MapManager
{
    private static readonly TimeSpan EmptyPublicInstanceLifetime = TimeSpan.FromMinutes(2);

    private readonly MapCatalog _catalog;
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, MapInstance>> _maps = new();
    private int _nextInstanceId;

    public MapManager(MapCatalog catalog)
    {
        _catalog = catalog;
    }

    public IReadOnlyDictionary<int, ConcurrentDictionary<int, MapInstance>> Maps => _maps;

    public MapInstance JoinInstance(
        MapDefinition definition,
        PlayerSession player,
        int? requestedZoneIndex = null,
        bool autoSelectPublicZone = false)
    {
        if (definition.IsPrivatePerPlayer)
            return JoinPrivateInstance(definition, player);

        return JoinPublicInstance(definition, player, requestedZoneIndex, autoSelectPublicZone);
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
        if (instance.PlayerCount == 0 && instance.IsPrivate)
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

    public int ResolveAutoJoinZone(MapDefinition definition)
    {
        if (definition.IsPrivatePerPlayer)
            return 0;

        var populated = FindPopulatedPublicInstance(definition);
        if (populated is not null)
            return populated.ZoneIndex;

        return definition.DefaultZoneIndex;
    }

    public IReadOnlyDictionary<int, int> GetActivePlayerCountsByZone(int mapId)
    {
        var result = new Dictionary<int, int>();
        if (!_maps.TryGetValue(mapId, out var instances))
            return result;

        foreach (var instance in instances.Values)
        {
            if (instance.IsPrivate)
                continue;

            result[instance.ZoneIndex] = instance.PlayerCount;
        }

        return result;
    }

    public void CleanupExpiredEmptyPublicInstances(DateTime utcNow)
    {
        foreach (var (mapId, instances) in _maps)
        {
            foreach (var (instanceId, instance) in instances)
            {
                if (instance.IsPrivate || instance.PlayerCount > 0 || !instance.EmptySinceUtc.HasValue)
                    continue;

                if (utcNow - instance.EmptySinceUtc.Value < EmptyPublicInstanceLifetime)
                    continue;

                instances.TryRemove(instanceId, out _);
            }

            if (instances.IsEmpty)
                _maps.TryRemove(mapId, out _);
        }
    }

    private MapInstance JoinPrivateInstance(MapDefinition definition, PlayerSession player)
    {
        var instances = _maps.GetOrAdd(definition.MapId, _ => new ConcurrentDictionary<int, MapInstance>());
        foreach (var instance in instances.Values)
        {
            if (instance.OwnerCharacterId != player.CharacterData.CharacterId)
                continue;

            if (!instance.AddPlayer(player))
                throw new InvalidOperationException("Unable to rejoin private map instance.");

            return instance;
        }

        var created = CreateInstance(definition, zoneIndex: 0, ownerCharacterId: player.CharacterData.CharacterId);
        if (!created.AddPlayer(player))
            throw new InvalidOperationException("Unable to join private map instance.");

        return created;
    }

    private MapInstance JoinPublicInstance(
        MapDefinition definition,
        PlayerSession player,
        int? requestedZoneIndex,
        bool autoSelectPublicZone)
    {
        var targetZoneIndex = requestedZoneIndex;
        if (!targetZoneIndex.HasValue && autoSelectPublicZone)
            targetZoneIndex = ResolveAutoJoinZone(definition);
        if (!targetZoneIndex.HasValue && player.ZoneIndex > 0)
            targetZoneIndex = player.ZoneIndex;
        if (!targetZoneIndex.HasValue || targetZoneIndex.Value <= 0)
            targetZoneIndex = definition.DefaultZoneIndex;

        if (!IsValidPublicZone(definition, targetZoneIndex.Value))
            throw new InvalidOperationException($"Zone {targetZoneIndex.Value} is invalid for map {definition.MapId}.");

        if (TryGetPublicInstanceByZone(definition.MapId, targetZoneIndex.Value, out var preferred))
        {
            if (preferred.AddPlayer(player))
                return preferred;
        }

        var instance = CreatePublicInstance(definition, targetZoneIndex.Value);

        if (!instance.AddPlayer(player))
        {
            instance = TryGetPublicInstanceByZone(definition.MapId, targetZoneIndex.Value, out preferred)
                ? preferred
                : CreatePublicInstance(definition, targetZoneIndex.Value);
            if (!instance.AddPlayer(player))
                throw new InvalidOperationException($"Unable to join public map zone {targetZoneIndex.Value}.");
        }

        return instance;
    }

    private MapInstance? FindPopulatedPublicInstance(MapDefinition definition)
    {
        if (!_maps.TryGetValue(definition.MapId, out var instances))
            return null;

        foreach (var instance in instances.Values
                     .Where(x => !x.IsPrivate && x.PlayerCount > 0 && x.PlayerCount < definition.MaxPlayersPerZone)
                     .OrderByDescending(x => x.PlayerCount)
                     .ThenBy(x => x.ZoneIndex))
        {
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

    private MapInstance CreatePublicInstance(MapDefinition definition, int zoneIndex)
    {
        if (definition.MaxPublicZoneCount <= 0)
            throw new InvalidOperationException($"Map {definition.MapId} does not allow public zones.");

        if (!IsValidPublicZone(definition, zoneIndex))
            throw new InvalidOperationException($"Zone {zoneIndex} is invalid for map {definition.MapId}.");

        return CreateInstance(definition, zoneIndex, ownerCharacterId: null, useZoneIndexAsInstanceId: true);
    }

    private MapInstance CreateInstance(MapDefinition definition, int zoneIndex, Guid? ownerCharacterId, bool useZoneIndexAsInstanceId = false)
    {
        var instanceId = useZoneIndexAsInstanceId ? zoneIndex : Interlocked.Increment(ref _nextInstanceId);
        var instance = new MapInstance(instanceId, zoneIndex, definition, ownerCharacterId);
        var instances = _maps.GetOrAdd(definition.MapId, _ => new ConcurrentDictionary<int, MapInstance>());
        instances[instanceId] = instance;
        return instance;
    }

    private static bool IsValidPublicZone(MapDefinition definition, int zoneIndex)
    {
        return zoneIndex >= 1 && zoneIndex <= definition.MaxPublicZoneCount;
    }
}
