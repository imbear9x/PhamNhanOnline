using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class PillEffectRepository
{
    private readonly GameDb _db;

    public PillEffectRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<PillEffectEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<PillEffectEntity>().ToListAsync(cancellationToken);
}

