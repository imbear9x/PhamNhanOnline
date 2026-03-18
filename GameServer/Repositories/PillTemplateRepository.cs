using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class PillTemplateRepository
{
    private readonly GameDb _db;

    public PillTemplateRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<PillTemplateEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<PillTemplateEntity>().ToListAsync(cancellationToken);
}

