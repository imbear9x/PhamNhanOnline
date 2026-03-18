using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class ItemTemplateRepository
{
    private readonly GameDb _db;

    public ItemTemplateRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<ItemTemplateEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<ItemTemplateEntity>().ToListAsync(cancellationToken);

    public Task<ItemTemplateEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.GetTable<ItemTemplateEntity>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
}
