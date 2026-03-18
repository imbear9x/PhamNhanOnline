using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class MartialArtBookTemplateRepository
{
    private readonly GameDb _db;

    public MartialArtBookTemplateRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<MartialArtBookTemplateEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<MartialArtBookTemplateEntity>().ToListAsync(cancellationToken);

    public Task<MartialArtBookTemplateEntity?> GetByItemTemplateIdAsync(int itemTemplateId, CancellationToken cancellationToken = default) =>
        _db.GetTable<MartialArtBookTemplateEntity>().FirstOrDefaultAsync(x => x.ItemTemplateId == itemTemplateId, cancellationToken);
}
