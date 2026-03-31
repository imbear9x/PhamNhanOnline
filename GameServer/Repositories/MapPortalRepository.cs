using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class MapPortalRepository
{
    private readonly GameDb _db;

    public MapPortalRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<MapPortalEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<MapPortalEntity>()
            .OrderBy(x => x.SourceMapTemplateId)
            .ThenBy(x => x.OrderIndex)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

    public Task<List<MapPortalEntity>> ListBySourceMapTemplateIdAsync(int sourceMapTemplateId, CancellationToken cancellationToken = default) =>
        _db.GetTable<MapPortalEntity>()
            .Where(x => x.SourceMapTemplateId == sourceMapTemplateId)
            .OrderBy(x => x.OrderIndex)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
}
