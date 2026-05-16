using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class PlayerHerbRepository
{
    private readonly GameDb _db;

    public PlayerHerbRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<PlayerHerbEntity>> ListByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerHerbEntity>()
            .Where(x => x.PlayerId == playerId)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

    public Task<PlayerHerbEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerHerbEntity>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<List<PlayerHerbEntity>> ListExpiredInventoryHerbsAsync(DateTime utcNow, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerHerbEntity>()
            .Where(x => x.State == 1 && x.ExpireAt != null && x.ExpireAt <= utcNow)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

    public Task<long> CreateAsync(PlayerHerbEntity entity, CancellationToken cancellationToken = default) =>
        _db.InsertEntityWithInt64IdentityAsync(entity, cancellationToken);

    public Task<int> UpdateAsync(PlayerHerbEntity entity, CancellationToken cancellationToken = default) =>
        _db.UpdateEntityAsync(entity, cancellationToken);

    public Task<int> DeleteAsync(long id, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerHerbEntity>().Where(x => x.Id == id).DeleteAsync(cancellationToken);

    public Task<int> DeleteExpiredInventoryHerbsAsync(DateTime utcNow, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerHerbEntity>()
            .Where(x => x.State == 1 && x.ExpireAt != null && x.ExpireAt <= utcNow)
            .DeleteAsync(cancellationToken);

    public Task<int> DeleteByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerHerbEntity>().Where(x => x.PlayerId == playerId).DeleteAsync(cancellationToken);
}
