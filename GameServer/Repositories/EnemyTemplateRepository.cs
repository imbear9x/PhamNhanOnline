using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class EnemyTemplateRepository
{
    private readonly GameDb _db;

    public EnemyTemplateRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<EnemyTemplateEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<EnemyTemplateEntity>().ToListAsync(cancellationToken);
}
