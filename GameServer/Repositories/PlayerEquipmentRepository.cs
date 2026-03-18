using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class PlayerEquipmentRepository
{
    private readonly GameDb _db;

    public PlayerEquipmentRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<PlayerEquipmentEntity>> ListByPlayerItemIdsAsync(IReadOnlyCollection<long> playerItemIds, CancellationToken cancellationToken = default)
    {
        if (playerItemIds.Count == 0)
            return Task.FromResult(new List<PlayerEquipmentEntity>());

        return _db.GetTable<PlayerEquipmentEntity>()
            .Where(x => playerItemIds.Contains(x.PlayerItemId))
            .ToListAsync(cancellationToken);
    }

    public Task<PlayerEquipmentEntity?> GetByPlayerItemIdAsync(long playerItemId, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerEquipmentEntity>().FirstOrDefaultAsync(x => x.PlayerItemId == playerItemId, cancellationToken);

    public Task<int> CreateAsync(PlayerEquipmentEntity entity, CancellationToken cancellationToken = default) =>
        _db.InsertEntityAsync(entity, cancellationToken);

    public Task<int> UpdateAsync(PlayerEquipmentEntity entity, CancellationToken cancellationToken = default) =>
        _db.UpdateEntityAsync(entity, cancellationToken);

    public Task<int> DeleteAsync(long playerItemId, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerEquipmentEntity>().Where(x => x.PlayerItemId == playerItemId).DeleteAsync(cancellationToken);
}
