using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class HerbHarvestOutputRepository
{
    private readonly GameDb _db;

    public HerbHarvestOutputRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<HerbHarvestOutputEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<HerbHarvestOutputEntity>().ToListAsync(cancellationToken);
}

