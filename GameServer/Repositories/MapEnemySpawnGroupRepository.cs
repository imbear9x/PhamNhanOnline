using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class MapEnemySpawnGroupRepository
{
    private readonly GameDb _db;

    public MapEnemySpawnGroupRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<MapEnemySpawnGroupEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<MapEnemySpawnGroupEntity>().ToListAsync(cancellationToken);
}
