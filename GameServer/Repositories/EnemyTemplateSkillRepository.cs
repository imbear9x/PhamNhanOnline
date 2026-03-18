using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class EnemyTemplateSkillRepository
{
    private readonly GameDb _db;

    public EnemyTemplateSkillRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<EnemyTemplateSkillEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<EnemyTemplateSkillEntity>().ToListAsync(cancellationToken);
}
