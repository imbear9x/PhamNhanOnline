using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class EnemyRewardRuleRepository
{
    private readonly GameDb _db;

    public EnemyRewardRuleRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<EnemyRewardRuleEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<EnemyRewardRuleEntity>().ToListAsync(cancellationToken);
}
