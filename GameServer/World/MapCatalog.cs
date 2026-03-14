using System.Collections.ObjectModel;
using System.Numerics;

namespace GameServer.World;

public sealed class MapCatalog
{
    private const int DefaultMapId = 1;

    private readonly ReadOnlyDictionary<int, MapDefinition> _definitions;

    public MapCatalog()
    {
        var starterTemplate = new MapTemplate(
            TemplateCode: "starter_valley",
            Width: 1024,
            Height: 1024,
            CellSize: 64,
            InterestRadius: 160,
            MaxPlayersPerInstance: 50,
            DefaultSpawnPosition: new Vector2(128, 128));

        _definitions = new ReadOnlyDictionary<int, MapDefinition>(
            new Dictionary<int, MapDefinition>
            {
                [DefaultMapId] = new(DefaultMapId, "Starter Valley", starterTemplate)
            });
    }

    public IReadOnlyDictionary<int, MapDefinition> Definitions => _definitions;

    public MapDefinition ResolveOrDefault(int? mapId)
    {
        if (mapId.HasValue && _definitions.TryGetValue(mapId.Value, out var definition))
            return definition;

        return _definitions[DefaultMapId];
    }

    public bool TryGet(int mapId, out MapDefinition definition)
    {
        return _definitions.TryGetValue(mapId, out definition!);
    }
}
