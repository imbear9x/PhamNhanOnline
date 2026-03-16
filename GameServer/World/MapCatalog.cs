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

    private ReadOnlyDictionary<int, MapDefinition> _definitions;
    private ReadOnlyDictionary<int, IReadOnlyList<MapZoneSlotDefinition>> _zoneSlotsByMap;
    private ReadOnlyDictionary<(int MapId, int ZoneIndex), MapZoneSlotDefinition> _zoneSlotsByKey;

    public MapCatalog(IServiceScopeFactory scopeFactory)
    {
        using var scope = scopeFactory.CreateScope();
        var mapRepository = scope.ServiceProvider.GetRequiredService<MapTemplateRepository>();
        var adjacentRepository = scope.ServiceProvider.GetRequiredService<MapTemplateAdjacentMapRepository>();
        var zoneSlotRepository = scope.ServiceProvider.GetRequiredService<MapZoneSlotRepository>();
        var spiritualEnergyRepository = scope.ServiceProvider.GetRequiredService<SpiritualEnergyTemplateRepository>();

        var mapEntities = mapRepository.GetAllAsync().GetAwaiter().GetResult();
        var zoneSlotEntities = zoneSlotRepository.GetAllAsync().GetAwaiter().GetResult();
        var spiritualEnergyEntities = spiritualEnergyRepository.GetAllAsync().GetAwaiter().GetResult();

        var spiritualEnergyById = spiritualEnergyEntities.ToDictionary(x => x.Id);
        var zoneSlotEntitiesByMap = zoneSlotEntities
            .GroupBy(x => x.MapTemplateId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<MapZoneSlotEntity>)g.OrderBy(x => x.ZoneIndex).ToArray());
        var definitions = new Dictionary<int, MapDefinition>(mapEntities.Count);
        var zoneSlotsByMap = new Dictionary<int, IReadOnlyList<MapZoneSlotDefinition>>(mapEntities.Count);
        var zoneSlotsByKey = new Dictionary<(int MapId, int ZoneIndex), MapZoneSlotDefinition>();
        foreach (var mapEntity in mapEntities)
        {
            var adjacentMapIds = adjacentRepository
                .ListByMapIdAsync(mapEntity.Id)
                .GetAwaiter()
                .GetResult()
                .Select(x => x.AdjacentMapTemplateId)
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
                InterestRadius: mapEntity.InterestRadius,
                MaxPublicZoneCount: mapEntity.MaxPublicZoneCount,
                MaxPlayersPerZone: mapEntity.MaxPlayersPerZone,
                SupportsCavePlacement: mapEntity.SupportsCavePlacement,
                DefaultSpawnPosition: new Vector2(mapEntity.DefaultSpawnX, mapEntity.DefaultSpawnY),
                IsPrivatePerPlayer: mapEntity.IsPrivatePerPlayer);

            definitions[mapEntity.Id] = new MapDefinition(template);
            var zoneSlots = BuildZoneSlotsForMap(mapEntity, zoneSlotEntitiesByMap, spiritualEnergyById);
            zoneSlotsByMap[mapEntity.Id] = zoneSlots;
            foreach (var zoneSlot in zoneSlots)
            {
                zoneSlotsByKey[(zoneSlot.MapId, zoneSlot.ZoneIndex)] = zoneSlot;
            }
        }

        Initialize(definitions, zoneSlotsByMap, zoneSlotsByKey);
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
        foreach (var slots in zoneSlotCopy.Values)
        {
            foreach (var zoneSlot in slots)
                zoneSlotsByKey[(zoneSlot.MapId, zoneSlot.ZoneIndex)] = zoneSlot;
        }

        Initialize(definitionCopy, zoneSlotCopy, zoneSlotsByKey);
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

    public bool CanTravel(int fromMapId, int toMapId)
    {
        return TryGet(fromMapId, out var from) && from.CanTravelTo(toMapId);
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
        Dictionary<(int MapId, int ZoneIndex), MapZoneSlotDefinition> zoneSlotsByKey)
    {
        if (definitions.Count == 0)
            throw new InvalidOperationException("No map templates were loaded from the database.");

        if (!definitions.Values.Any(x => x.Type == MapType.Home))
            throw new InvalidOperationException("At least one Home map template must exist in the database.");

        _definitions = new ReadOnlyDictionary<int, MapDefinition>(definitions);
        _zoneSlotsByMap = new ReadOnlyDictionary<int, IReadOnlyList<MapZoneSlotDefinition>>(zoneSlotsByMap);
        _zoneSlotsByKey = new ReadOnlyDictionary<(int MapId, int ZoneIndex), MapZoneSlotDefinition>(zoneSlotsByKey);
    }
}
