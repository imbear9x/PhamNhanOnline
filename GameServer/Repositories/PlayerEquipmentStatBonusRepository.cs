using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class PlayerEquipmentStatBonusRepository
{
    private readonly GameDb _db;

    public PlayerEquipmentStatBonusRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<PlayerEquipmentStatBonusEntity>> ListByPlayerItemIdsAsync(IReadOnlyCollection<long> playerItemIds, CancellationToken cancellationToken = default)
    {
        if (playerItemIds.Count == 0)
            return Task.FromResult(new List<PlayerEquipmentStatBonusEntity>());

        return _db.GetTable<PlayerEquipmentStatBonusEntity>()
            .Where(x => playerItemIds.Contains(x.PlayerItemId))
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<long> CreateAsync(PlayerEquipmentStatBonusEntity entity, CancellationToken cancellationToken = default) =>
        _db.InsertWithInt64IdentityAsync(entity, token: cancellationToken);

    public Task<int> DeleteByPlayerItemIdAsync(long playerItemId, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerEquipmentStatBonusEntity>()
            .Where(x => x.PlayerItemId == playerItemId)
            .DeleteAsync(cancellationToken);
}
