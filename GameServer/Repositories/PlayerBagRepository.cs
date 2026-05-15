using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class PlayerBagRepository
{
    private readonly GameDb _db;

    public PlayerBagRepository(GameDb db)
    {
        _db = db;
    }

    public Task<PlayerBagEntity?> GetByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerBagEntity>()
            .FirstOrDefaultAsync(x => x.PlayerId == playerId, cancellationToken);

    public Task<List<PlayerBagEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerBagEntity>()
            .OrderBy(x => x.PlayerId)
            .ToListAsync(cancellationToken);

    public Task<int> CreateAsync(PlayerBagEntity entity, CancellationToken cancellationToken = default) =>
        _db.InsertEntityAsync(entity, cancellationToken);

    public Task<int> UpdateAsync(PlayerBagEntity entity, CancellationToken cancellationToken = default) =>
        _db.UpdateEntityAsync(entity, cancellationToken);
}