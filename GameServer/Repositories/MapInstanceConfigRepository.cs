using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class MapInstanceConfigRepository
{
    private readonly GameDb _db;

    public MapInstanceConfigRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<MapInstanceConfigEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<MapInstanceConfigEntity>().ToListAsync(cancellationToken);
}
