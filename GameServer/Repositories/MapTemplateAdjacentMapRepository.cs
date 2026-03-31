using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class MapTemplateAdjacentMapRepository
{
    private readonly GameDb _db;

    public MapTemplateAdjacentMapRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<MapTemplateAdjacentMapEntity>> ListByMapIdAsync(int mapTemplateId, CancellationToken cancellationToken = default) =>
        _db.GetTable<MapTemplateAdjacentMapEntity>()
            .Where(x => x.MapTemplateId == mapTemplateId)
            .ToListAsync(cancellationToken);

    public Task<List<MapTemplateAdjacentMapEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<MapTemplateAdjacentMapEntity>()
            .OrderBy(x => x.MapTemplateId)
            .ThenBy(x => x.AdjacentMapTemplateId)
            .ToListAsync(cancellationToken);
}
