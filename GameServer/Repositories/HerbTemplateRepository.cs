using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class HerbTemplateRepository
{
    private readonly GameDb _db;

    public HerbTemplateRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<HerbTemplateEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<HerbTemplateEntity>().ToListAsync(cancellationToken);

    public Task<HerbTemplateEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.GetTable<HerbTemplateEntity>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
}

