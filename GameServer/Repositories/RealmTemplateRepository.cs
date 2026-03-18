using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class RealmTemplateRepository
{
    private readonly GameDb _db;

    public RealmTemplateRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<RealmTemplate>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<RealmTemplate>().ToListAsync(cancellationToken);

    public Task<RealmTemplate?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.GetTable<RealmTemplate>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<int> CreateAsync(RealmTemplate entity, CancellationToken cancellationToken = default)
    {
        await _db.InsertEntityAsync(entity, cancellationToken);
        return entity.Id;
    }

    public Task<int> UpdateAsync(RealmTemplate entity, CancellationToken cancellationToken = default) =>
        _db.UpdateEntityAsync(entity, cancellationToken);

    public Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        _db.GetTable<RealmTemplate>().Where(x => x.Id == id).DeleteAsync(cancellationToken);
}

