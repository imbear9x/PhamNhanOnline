using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class MapEnemySpawnEntryRepository
{
    private readonly GameDb _db;

    public MapEnemySpawnEntryRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<MapEnemySpawnEntryEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<MapEnemySpawnEntryEntity>().ToListAsync(cancellationToken);
}
