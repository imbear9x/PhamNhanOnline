using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class BreakthroughAttemptRepository
{
    private readonly GameDb _db;

    public BreakthroughAttemptRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<BreakthroughAttempt>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<BreakthroughAttempt>().ToListAsync(cancellationToken);

    public Task<BreakthroughAttempt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.GetTable<BreakthroughAttempt>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> HasFailedAttemptForRealmAsync(Guid characterId, int realmId, CancellationToken cancellationToken = default) =>
        _db.GetTable<BreakthroughAttempt>()
            .AnyAsync(
                x => x.CharacterId == characterId &&
                     x.RealmId == realmId &&
                     x.Result == false,
                cancellationToken);

    public async Task<Guid> CreateAsync(BreakthroughAttempt entity, CancellationToken cancellationToken = default)
    {
        await _db.InsertAsync(entity, token: cancellationToken);
        return entity.Id;
    }

    public Task<int> UpdateAsync(BreakthroughAttempt entity, CancellationToken cancellationToken = default) =>
        _db.UpdateAsync(entity, token: cancellationToken);

    public Task<int> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.GetTable<BreakthroughAttempt>().Where(x => x.Id == id).DeleteAsync(cancellationToken);
}

