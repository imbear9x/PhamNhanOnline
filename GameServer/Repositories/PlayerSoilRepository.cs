using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class PlayerSoilRepository
{
    private readonly GameDb _db;

    public PlayerSoilRepository(GameDb db)
    {
        _db = db;
    }

    public Task<PlayerSoilEntity?> GetByPlayerItemIdAsync(long playerItemId, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerSoilEntity>().FirstOrDefaultAsync(x => x.PlayerItemId == playerItemId, cancellationToken);

    public Task<List<PlayerSoilEntity>> ListByPlayerItemIdsAsync(IReadOnlyCollection<long> playerItemIds, CancellationToken cancellationToken = default)
    {
        if (playerItemIds.Count == 0)
            return Task.FromResult(new List<PlayerSoilEntity>());

        return _db.GetTable<PlayerSoilEntity>()
            .Where(x => playerItemIds.Contains(x.PlayerItemId))
            .ToListAsync(cancellationToken);
    }

    public Task CreateAsync(PlayerSoilEntity entity, CancellationToken cancellationToken = default) =>
        _db.InsertAsync(entity, token: cancellationToken);

    public Task<int> UpdateAsync(PlayerSoilEntity entity, CancellationToken cancellationToken = default) =>
        _db.UpdateAsync(entity, token: cancellationToken);

    public Task<int> DeleteAsync(long playerItemId, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerSoilEntity>().Where(x => x.PlayerItemId == playerItemId).DeleteAsync(cancellationToken);
}

