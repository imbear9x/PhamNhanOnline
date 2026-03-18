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

    public Task<long> CreateAsync(PlayerHerbEntity entity, CancellationToken cancellationToken = default) =>
        _db.InsertWithInt64IdentityAsync(entity, token: cancellationToken);

    public Task<int> UpdateAsync(PlayerHerbEntity entity, CancellationToken cancellationToken = default) =>
        _db.UpdateAsync(entity, token: cancellationToken);

    public Task<int> DeleteAsync(long id, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerHerbEntity>().Where(x => x.Id == id).DeleteAsync(cancellationToken);
}

