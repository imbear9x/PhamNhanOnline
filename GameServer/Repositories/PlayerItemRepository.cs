using GameServer.Entities;
using GameServer.Runtime;
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
            .Where(x => x.PlayerId == playerId && x.LocationType == (int)ItemLocationType.Inventory)
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
            .Where(x => x.PlayerId == playerId && x.LocationType == (int)ItemLocationType.Inventory && x.ItemTemplateId == itemTemplateId)
            .OrderBy(x => x.AcquiredAt)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

    public Task<List<PlayerItemEntity>> ListByTemplateAndLocationAsync(
        Guid? playerId,
        int itemTemplateId,
        ItemLocationType locationType,
        CancellationToken cancellationToken = default)
    {
        var query = _db.GetTable<PlayerItemEntity>()
            .Where(x => x.LocationType == (int)locationType && x.ItemTemplateId == itemTemplateId);

        if (playerId.HasValue)
            query = query.Where(x => x.PlayerId == playerId.Value);
        else
            query = query.Where(x => x.PlayerId == null);

        return query
            .OrderBy(x => x.AcquiredAt)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<List<PlayerItemEntity>> ListByLocationAsync(ItemLocationType locationType, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerItemEntity>()
            .Where(x => x.LocationType == (int)locationType)
            .OrderBy(x => x.AcquiredAt)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

    public Task<long> CreateAsync(PlayerItemEntity entity, CancellationToken cancellationToken = default) =>
        _db.InsertEntityWithInt64IdentityAsync(entity, cancellationToken);

    public Task<int> UpdateAsync(PlayerItemEntity entity, CancellationToken cancellationToken = default) =>
        _db.UpdateEntityAsync(entity, cancellationToken);

    public Task<int> DeleteAsync(long id, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerItemEntity>().Where(x => x.Id == id).DeleteAsync(cancellationToken);
}
