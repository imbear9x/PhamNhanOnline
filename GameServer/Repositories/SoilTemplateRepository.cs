using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class SoilTemplateRepository
{
    private readonly GameDb _db;

    public SoilTemplateRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<SoilTemplateEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<SoilTemplateEntity>().ToListAsync(cancellationToken);

    public Task<SoilTemplateEntity?> GetByIdAsync(int itemTemplateId, CancellationToken cancellationToken = default) =>
        _db.GetTable<SoilTemplateEntity>().FirstOrDefaultAsync(x => x.ItemTemplateId == itemTemplateId, cancellationToken);
}

