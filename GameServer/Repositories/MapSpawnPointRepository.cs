using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class MapSpawnPointRepository
{
    private readonly GameDb _db;

    public MapSpawnPointRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<MapSpawnPointEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<MapSpawnPointEntity>()
            .OrderBy(x => x.MapTemplateId)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

    public Task<List<MapSpawnPointEntity>> ListByMapTemplateIdAsync(int mapTemplateId, CancellationToken cancellationToken = default) =>
        _db.GetTable<MapSpawnPointEntity>()
            .Where(x => x.MapTemplateId == mapTemplateId)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
}
