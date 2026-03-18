using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class SkillEffectRepository
{
    private readonly GameDb _db;

    public SkillEffectRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<SkillEffectEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<SkillEffectEntity>().ToListAsync(cancellationToken);
}
