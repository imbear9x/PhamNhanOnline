using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class MapTemplateRepository
{
    private readonly GameDb _db;

    public MapTemplateRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<MapTemplateEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<MapTemplateEntity>().ToListAsync(cancellationToken);
}
