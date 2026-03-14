using System.Collections.ObjectModel;
using System.Numerics;

namespace GameServer.World;

public sealed class MapCatalog
{
    public const int HomeMapId = 1;
    public const int StarterFarmMapId = 2;

    private readonly ReadOnlyDictionary<int, MapDefinition> _definitions;

    public MapCatalog()
    {
        var homeTemplate = new MapTemplate(
            TemplateId: HomeMapId,
            Name: "Player Home",
            Type: MapType.Home,
            ClientMapKey: "map_home_01",
            AdjacentMapIds: new[] { StarterFarmMapId },
            Width: 256,
            Height: 256,
            CellSize: 32,
            InterestRadius: 96,
            MaxPublicZoneCount: 0,
            MaxPlayersPerZone: 1,
            DefaultSpawnPosition: new Vector2(64, 64),
            IsPrivatePerPlayer: true);

        var starterFarmTemplate = new MapTemplate(
            TemplateId: StarterFarmMapId,
            Name: "Starter Plains",
            Type: MapType.Farm,
            ClientMapKey: "map_farm_01",
            AdjacentMapIds: new[] { HomeMapId },
            Width: 1024,
            Height: 1024,
            CellSize: 64,
            InterestRadius: 160,
            MaxPublicZoneCount: 2,
            MaxPlayersPerZone: 20,
            DefaultSpawnPosition: new Vector2(128, 128),
            IsPrivatePerPlayer: false);

        _definitions = new ReadOnlyDictionary<int, MapDefinition>(
            new Dictionary<int, MapDefinition>
            {
                [HomeMapId] = new(homeTemplate),
                [StarterFarmMapId] = new(starterFarmTemplate)
            });
    }

    public IReadOnlyDictionary<int, MapDefinition> Definitions => _definitions;

    public MapDefinition ResolveOrDefault(int? mapId)
    {
        if (mapId.HasValue && _definitions.TryGetValue(mapId.Value, out var definition))
            return definition;

        return _definitions[HomeMapId];
    }

    public MapDefinition ResolveHomeDefinition() => _definitions[HomeMapId];

    public bool TryGet(int mapId, out MapDefinition definition)
    {
        return _definitions.TryGetValue(mapId, out definition!);
    }

    public bool CanTravel(int fromMapId, int toMapId)
    {
        return TryGet(fromMapId, out var from) && from.CanTravelTo(toMapId);
    }
}
