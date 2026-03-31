using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using GameServer.Entities;
using GameServer.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace GameServer.World;

public sealed class MapCatalog
{
    public const int HomeMapId = 1;
    public const int StarterFarmMapId = 2;

    private ReadOnlyDictionary<int, MapDefinition> _definitions =
        new(new Dictionary<int, MapDefinition>());
    private ReadOnlyDictionary<int, IReadOnlyList<MapZoneSlotDefinition>> _zoneSlotsByMap =
        new(new Dictionary<int, IReadOnlyList<MapZoneSlotDefinition>>());
    private ReadOnlyDictionary<(int MapId, int ZoneIndex), MapZoneSlotDefinition> _zoneSlotsByKey =
        new(new Dictionary<(int MapId, int ZoneIndex), MapZoneSlotDefinition>());
    private ReadOnlyDictionary<int, IReadOnlyList<MapSpawnPointDefinition>> _spawnPointsByMap =
        new(new Dictionary<int, IReadOnlyList<MapSpawnPointDefinition>>());
    private ReadOnlyDictionary<(int MapId, int SpawnPointId), MapSpawnPointDefinition> _spawnPointsByKey =
        new(new Dictionary<(int MapId, int SpawnPointId), MapSpawnPointDefinition>());
    private ReadOnlyDictionary<int, IReadOnlyList<MapPortalDefinition>> _portalsByMap =
        new(new Dictionary<int, IReadOnlyList<MapPortalDefinition>>());
    private ReadOnlyDictionary<(int MapId, int PortalId), MapPortalDefinition> _portalsByKey =
        new(new Dictionary<(int MapId, int PortalId), MapPortalDefinition>());

    public MapCatalog(IServiceScopeFactory scopeFactory)
    {
        using var scope = scopeFactory.CreateScope();
        var mapRepository = scope.ServiceProvider.GetRequiredService<MapTemplateRepository>();
        var adjacentRepository = scope.ServiceProvider.GetRequiredService<MapTemplateAdjacentMapRepository>();
        var zoneSlotRepository = scope.ServiceProvider.GetRequiredService<MapZoneSlotRepository>();
        var spawnPointRepository = scope.ServiceProvider.GetRequiredService<MapSpawnPointRepository>();
        var portalRepository = scope.ServiceProvider.GetRequiredService<MapPortalRepository>();
        var spiritualEnergyRepository = scope.ServiceProvider.GetRequiredService<SpiritualEnergyTemplateRepository>();

        var mapEntities = mapRepository.GetAllAsync().GetAwaiter().GetResult();
        var mapEntitiesById = mapEntities.ToDictionary(x => x.Id);
        var adjacentEntities = adjacentRepository.GetAllAsync().GetAwaiter().GetResult();
        var zoneSlotEntities = zoneSlotRepository.GetAllAsync().GetAwaiter().GetResult();
        var spawnPointEntities = spawnPointRepository.GetAllAsync().GetAwaiter().GetResult();
        var portalEntities = portalRepository.GetAllAsync().GetAwaiter().GetResult();
        var spiritualEnergyEntities = spiritualEnergyRepository.GetAllAsync().GetAwaiter().GetResult();

        var spiritualEnergyById = spiritualEnergyEntities.ToDictionary(x => x.Id);
        var adjacentMapIdsByMap = adjacentEntities
            .GroupBy(x => x.MapTemplateId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<int>)g
                    .Select(x => x.AdjacentMapTemplateId)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToArray());
        var zoneSlotEntitiesByMap = zoneSlotEntities
            .GroupBy(x => x.MapTemplateId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<MapZoneSlotEntity>)g.OrderBy(x => x.ZoneIndex).ToArray());
        var spawnPointDefinitionsByMap = spawnPointEntities
            .GroupBy(x => x.MapTemplateId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<MapSpawnPointDefinition>)g
                    .OrderBy(x => x.Id)
                    .Select(BuildSpawnPointDefinition)
                    .ToArray());
        var portalEntitiesByMap = portalEntities
            .GroupBy(x => x.SourceMapTemplateId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<MapPortalEntity>)g
                    .OrderBy(x => x.OrderIndex)
                    .ThenBy(x => x.Id)
                    .ToArray());
        var definitions = new Dictionary<int, MapDefinition>(mapEntities.Count);
        var zoneSlotsByMap = new Dictionary<int, IReadOnlyList<MapZoneSlotDefinition>>(mapEntities.Count);
        var zoneSlotsByKey = new Dictionary<(int MapId, int ZoneIndex), MapZoneSlotDefinition>();
        var spawnPointsByMap = new Dictionary<int, IReadOnlyList<MapSpawnPointDefinition>>(mapEntities.Count);
        var spawnPointsByKey = new Dictionary<(int MapId, int SpawnPointId), MapSpawnPointDefinition>();
        var portalsByMap = new Dictionary<int, IReadOnlyList<MapPortalDefinition>>(mapEntities.Count);
        var portalsByKey = new Dictionary<(int MapId, int PortalId), MapPortalDefinition>();
        foreach (var mapEntity in mapEntities)
        {
            spawnPointDefinitionsByMap.TryGetValue(mapEntity.Id, out var spawnPointsForMap);
            spawnPointsForMap ??= Array.Empty<MapSpawnPointDefinition>();

            portalEntitiesByMap.TryGetValue(mapEntity.Id, out var portalEntitiesForMap);
            portalEntitiesForMap ??= Array.Empty<MapPortalEntity>();
            var portalDefinitionsForMap = BuildPortalDefinitions(
                mapEntity.Id,
                portalEntitiesForMap,
                spawnPointDefinitionsByMap,
                mapEntitiesById);

            var portalAdjacentMapIds = portalDefinitionsForMap
                .Where(x => x.IsEnabled)
                .Select(x => x.TargetMapId);
            adjacentMapIdsByMap.TryGetValue(mapEntity.Id, out var configuredAdjacentMapIds);
            var adjacentMapIds = portalAdjacentMapIds
                .Concat(configuredAdjacentMapIds ?? Array.Empty<int>())
                .Distinct()
                .OrderBy(x => x)
                .ToArray();

            var template = new MapTemplate(
                TemplateId: mapEntity.Id,
                Name: mapEntity.Name,
                Type: (MapType)mapEntity.MapType,
                ClientMapKey: mapEntity.ClientMapKey,
                SpiritualEnergyPerMinute: mapEntity.SpiritualEnergy,
                AdjacentMapIds: adjacentMapIds,
                Width: mapEntity.Width,
                Height: mapEntity.Height,
                CellSize: mapEntity.CellSize,
                MaxPublicZoneCount: mapEntity.MaxPublicZoneCount,
                MaxPlayersPerZone: mapEntity.MaxPlayersPerZone,
                SupportsCavePlacement: mapEntity.SupportsCavePlacement,
                DefaultSpawnPosition: new Vector2(mapEntity.DefaultSpawnX, mapEntity.DefaultSpawnY),
                IsPrivatePerPlayer: mapEntity.IsPrivatePerPlayer);

            definitions[mapEntity.Id] = new MapDefinition(template)
            {
                SpawnPoints = spawnPointsForMap,
                Portals = portalDefinitionsForMap
            };
            var zoneSlots = BuildZoneSlotsForMap(mapEntity, zoneSlotEntitiesByMap, spiritualEnergyById);
            zoneSlotsByMap[mapEntity.Id] = zoneSlots;
            foreach (var zoneSlot in zoneSlots)
            {
                zoneSlotsByKey[(zoneSlot.MapId, zoneSlot.ZoneIndex)] = zoneSlot;
            }

            spawnPointsByMap[mapEntity.Id] = spawnPointsForMap;
            foreach (var spawnPoint in spawnPointsForMap)
                spawnPointsByKey[(spawnPoint.MapId, spawnPoint.Id)] = spawnPoint;

            portalsByMap[mapEntity.Id] = portalDefinitionsForMap;
            foreach (var portal in portalDefinitionsForMap)
                portalsByKey[(portal.SourceMapId, portal.Id)] = portal;
        }

        Initialize(definitions, zoneSlotsByMap, zoneSlotsByKey, spawnPointsByMap, spawnPointsByKey, portalsByMap, portalsByKey);
    }

    public MapCatalog(
        IReadOnlyDictionary<int, MapDefinition> definitions,
        IReadOnlyDictionary<int, IReadOnlyList<MapZoneSlotDefinition>>? zoneSlotsByMap = null)
    {
        var definitionCopy = new Dictionary<int, MapDefinition>(definitions);
        var zoneSlotCopy = zoneSlotsByMap is null
            ? new Dictionary<int, IReadOnlyList<MapZoneSlotDefinition>>()
            : zoneSlotsByMap.ToDictionary(x => x.Key, x => x.Value);
        var zoneSlotsByKey = new Dictionary<(int MapId, int ZoneIndex), MapZoneSlotDefinition>();
        var spawnPointsByMap = new Dictionary<int, IReadOnlyList<MapSpawnPointDefinition>>();
        var spawnPointsByKey = new Dictionary<(int MapId, int SpawnPointId), MapSpawnPointDefinition>();
        var portalsByMap = new Dictionary<int, IReadOnlyList<MapPortalDefinition>>();
        var portalsByKey = new Dictionary<(int MapId, int PortalId), MapPortalDefinition>();
        foreach (var slots in zoneSlotCopy.Values)
        {
            foreach (var zoneSlot in slots)
                zoneSlotsByKey[(zoneSlot.MapId, zoneSlot.ZoneIndex)] = zoneSlot;
        }

        foreach (var definition in definitionCopy.Values)
        {
            var spawnPoints = definition.SpawnPoints ?? Array.Empty<MapSpawnPointDefinition>();
            spawnPointsByMap[definition.MapId] = spawnPoints;
            foreach (var spawnPoint in spawnPoints)
                spawnPointsByKey[(spawnPoint.MapId, spawnPoint.Id)] = spawnPoint;

            var portals = definition.Portals ?? Array.Empty<MapPortalDefinition>();
            portalsByMap[definition.MapId] = portals;
            foreach (var portal in portals)
                portalsByKey[(portal.SourceMapId, portal.Id)] = portal;
        }

        Initialize(definitionCopy, zoneSlotCopy, zoneSlotsByKey, spawnPointsByMap, spawnPointsByKey, portalsByMap, portalsByKey);
    }

    public IReadOnlyDictionary<int, MapDefinition> Definitions => _definitions;

    public MapDefinition ResolveOrDefault(int? mapId)
    {
        if (mapId.HasValue && _definitions.TryGetValue(mapId.Value, out var definition))
            return definition;

        return ResolveHomeDefinition();
    }

    public MapDefinition ResolveHomeDefinition()
    {
        foreach (var definition in _definitions.Values)
        {
            if (definition.Type == MapType.Home)
                return definition;
        }

        throw new InvalidOperationException("Home map definition is missing.");
    }

    public bool TryGet(int mapId, out MapDefinition definition)
    {
        return _definitions.TryGetValue(mapId, out definition!);
    }

    public IReadOnlyList<MapZoneSlotDefinition> GetZoneSlots(int mapId)
    {
        return _zoneSlotsByMap.TryGetValue(mapId, out var slots)
            ? slots
            : Array.Empty<MapZoneSlotDefinition>();
    }

    public bool TryGetZoneSlot(int mapId, int zoneIndex, out MapZoneSlotDefinition slot)
    {
        return _zoneSlotsByKey.TryGetValue((mapId, zoneIndex), out slot!);
    }

    public IReadOnlyList<MapSpawnPointDefinition> GetSpawnPoints(int mapId)
    {
        return _spawnPointsByMap.TryGetValue(mapId, out var spawnPoints)
            ? spawnPoints
            : Array.Empty<MapSpawnPointDefinition>();
    }

    public bool TryGetSpawnPoint(int mapId, int spawnPointId, out MapSpawnPointDefinition spawnPoint)
    {
        return _spawnPointsByKey.TryGetValue((mapId, spawnPointId), out spawnPoint!);
    }

    public IReadOnlyList<MapPortalDefinition> GetPortals(int mapId)
    {
        return _portalsByMap.TryGetValue(mapId, out var portals)
            ? portals
            : Array.Empty<MapPortalDefinition>();
    }

    public bool TryGetPortal(int mapId, int portalId, out MapPortalDefinition portal)
    {
        return _portalsByKey.TryGetValue((mapId, portalId), out portal!);
    }

    public bool CanTravel(int fromMapId, int toMapId)
    {
        return TryGet(fromMapId, out var from) && from.CanTravelTo(toMapId);
    }

    private static MapSpawnPointDefinition BuildSpawnPointDefinition(MapSpawnPointEntity entity)
    {
        return new MapSpawnPointDefinition(
            entity.Id,
            entity.MapTemplateId,
            entity.Code,
            entity.Name,
            Enum.IsDefined(typeof(MapSpawnPointCategory), entity.SpawnCategory)
                ? (MapSpawnPointCategory)entity.SpawnCategory
                : MapSpawnPointCategory.Custom,
            new Vector2(entity.PosX, entity.PosY),
            entity.FacingDegrees,
            entity.Description ?? string.Empty);
    }

    private static IReadOnlyList<MapPortalDefinition> BuildPortalDefinitions(
        int mapId,
        IReadOnlyList<MapPortalEntity> portalEntities,
        IReadOnlyDictionary<int, IReadOnlyList<MapSpawnPointDefinition>> spawnPointDefinitionsByMap,
        IReadOnlyDictionary<int, MapTemplateEntity> mapEntitiesById)
    {
        if (portalEntities.Count == 0)
            return Array.Empty<MapPortalDefinition>();

        var result = new List<MapPortalDefinition>(portalEntities.Count);
        for (var i = 0; i < portalEntities.Count; i++)
        {
            var portalEntity = portalEntities[i];
            if (portalEntity.SourceMapTemplateId != mapId)
            {
                throw new InvalidOperationException(
                    $"Portal {portalEntity.Id} belongs to source map {portalEntity.SourceMapTemplateId}, expected {mapId}.");
            }

            if (!spawnPointDefinitionsByMap.TryGetValue(portalEntity.TargetMapTemplateId, out var targetSpawnPoints) ||
                !targetSpawnPoints.Any(x => x.Id == portalEntity.TargetSpawnPointId))
            {
                throw new InvalidOperationException(
                    $"Portal {portalEntity.Id} references missing target spawn point {portalEntity.TargetMapTemplateId}:{portalEntity.TargetSpawnPointId}.");
            }

            if (!mapEntitiesById.TryGetValue(portalEntity.TargetMapTemplateId, out var targetMapEntity))
            {
                throw new InvalidOperationException(
                    $"Portal {portalEntity.Id} references missing target map {portalEntity.TargetMapTemplateId}.");
            }

            result.Add(new MapPortalDefinition(
                portalEntity.Id,
                portalEntity.SourceMapTemplateId,
                portalEntity.Code,
                portalEntity.Name,
                new Vector2(portalEntity.SourceX, portalEntity.SourceY),
                portalEntity.InteractionRadius,
                Enum.IsDefined(typeof(MapPortalInteractionMode), portalEntity.InteractionMode)
                    ? (MapPortalInteractionMode)portalEntity.InteractionMode
                    : MapPortalInteractionMode.Touch,
                portalEntity.TargetMapTemplateId,
                targetMapEntity.Name,
                portalEntity.TargetSpawnPointId,
                portalEntity.IsEnabled,
                portalEntity.OrderIndex,
                portalEntity.Description ?? string.Empty));
        }

        return result;
    }

    private static IReadOnlyList<MapZoneSlotDefinition> BuildZoneSlotsForMap(
        MapTemplateEntity mapEntity,
        IReadOnlyDictionary<int, IReadOnlyList<MapZoneSlotEntity>> zoneSlotEntitiesByMap,
        IReadOnlyDictionary<int, SpiritualEnergyTemplateEntity> spiritualEnergyById)
    {
        if (mapEntity.IsPrivatePerPlayer || !mapEntity.SupportsCavePlacement || mapEntity.MaxPublicZoneCount <= 0)
            return Array.Empty<MapZoneSlotDefinition>();

        zoneSlotEntitiesByMap.TryGetValue(mapEntity.Id, out var configuredSlots);
        var configuredByIndex = configuredSlots?
            .GroupBy(x => x.ZoneIndex)
            .ToDictionary(g => g.Key, g => g.Last());

        var result = new List<MapZoneSlotDefinition>(mapEntity.MaxPublicZoneCount);
        for (var zoneIndex = 1; zoneIndex <= mapEntity.MaxPublicZoneCount; zoneIndex++)
        {
            var slotEntity = configuredByIndex is not null && configuredByIndex.TryGetValue(zoneIndex, out var configured)
                ? configured
                : null;
            decimal zoneSpiritualEnergy = mapEntity.SpiritualEnergy;
            var energyId = 0;
            var energyCode = string.Empty;
            var energyName = string.Empty;
            if (slotEntity is not null)
            {
                if (!spiritualEnergyById.TryGetValue(slotEntity.SpiritualEnergyTemplateId, out var energy))
                {
                    throw new InvalidOperationException(
                        $"Map zone slot {slotEntity.Id} references missing spiritual_energy_templates row {slotEntity.SpiritualEnergyTemplateId}.");
                }

                energyId = energy.Id;
                energyCode = energy.Code;
                energyName = energy.Name;
                zoneSpiritualEnergy = mapEntity.SpiritualEnergy * energy.LkPerMinute;
            }

            result.Add(new MapZoneSlotDefinition(
                mapEntity.Id,
                zoneIndex,
                energyId,
                energyCode,
                energyName,
                zoneSpiritualEnergy));
        }

        return result;
    }

    private void Initialize(
        Dictionary<int, MapDefinition> definitions,
        Dictionary<int, IReadOnlyList<MapZoneSlotDefinition>> zoneSlotsByMap,
        Dictionary<(int MapId, int ZoneIndex), MapZoneSlotDefinition> zoneSlotsByKey,
        Dictionary<int, IReadOnlyList<MapSpawnPointDefinition>> spawnPointsByMap,
        Dictionary<(int MapId, int SpawnPointId), MapSpawnPointDefinition> spawnPointsByKey,
        Dictionary<int, IReadOnlyList<MapPortalDefinition>> portalsByMap,
        Dictionary<(int MapId, int PortalId), MapPortalDefinition> portalsByKey)
    {
        if (definitions.Count == 0)
            throw new InvalidOperationException("No map templates were loaded from the database.");

        if (!definitions.Values.Any(x => x.Type == MapType.Home))
            throw new InvalidOperationException("At least one Home map template must exist in the database.");

        _definitions = new ReadOnlyDictionary<int, MapDefinition>(definitions);
        _zoneSlotsByMap = new ReadOnlyDictionary<int, IReadOnlyList<MapZoneSlotDefinition>>(zoneSlotsByMap);
        _zoneSlotsByKey = new ReadOnlyDictionary<(int MapId, int ZoneIndex), MapZoneSlotDefinition>(zoneSlotsByKey);
        _spawnPointsByMap = new ReadOnlyDictionary<int, IReadOnlyList<MapSpawnPointDefinition>>(spawnPointsByMap);
        _spawnPointsByKey = new ReadOnlyDictionary<(int MapId, int SpawnPointId), MapSpawnPointDefinition>(spawnPointsByKey);
        _portalsByMap = new ReadOnlyDictionary<int, IReadOnlyList<MapPortalDefinition>>(portalsByMap);
        _portalsByKey = new ReadOnlyDictionary<(int MapId, int PortalId), MapPortalDefinition>(portalsByKey);
    }
}
