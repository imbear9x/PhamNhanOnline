using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class HerbGrowthStageConfigRepository
{
    private readonly GameDb _db;

    public HerbGrowthStageConfigRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<HerbGrowthStageConfigEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<HerbGrowthStageConfigEntity>().ToListAsync(cancellationToken);
}

