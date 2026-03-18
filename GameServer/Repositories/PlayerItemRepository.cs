using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class PlayerItemRepository
{
    private readonly GameDb _db;

    public PlayerItemRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<PlayerItemEntity>> ListByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerItemEntity>()
            .Where(x => x.PlayerId == playerId)
            .OrderBy(x => x.AcquiredAt)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

    public Task<PlayerItemEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerItemEntity>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<List<PlayerItemEntity>> ListByIdsAsync(IReadOnlyCollection<long> itemIds, CancellationToken cancellationToken = default)
    {
        if (itemIds.Count == 0)
            return Task.FromResult(new List<PlayerItemEntity>());

        return _db.GetTable<PlayerItemEntity>()
            .Where(x => itemIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
    }

    public Task<List<PlayerItemEntity>> ListByTemplateIdAsync(Guid playerId, int itemTemplateId, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerItemEntity>()
            .Where(x => x.PlayerId == playerId && x.ItemTemplateId == itemTemplateId)
            .OrderBy(x => x.AcquiredAt)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

    public Task<long> CreateAsync(PlayerItemEntity entity, CancellationToken cancellationToken = default) =>
        _db.InsertWithInt64IdentityAsync(entity, token: cancellationToken);

    public Task<int> UpdateAsync(PlayerItemEntity entity, CancellationToken cancellationToken = default) =>
        _db.UpdateAsync(entity, token: cancellationToken);

    public Task<int> DeleteAsync(long id, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerItemEntity>().Where(x => x.Id == id).DeleteAsync(cancellationToken);
}
