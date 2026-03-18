using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class PlayerMartialArtRepository
{
    private readonly GameDb _db;

    public PlayerMartialArtRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<PlayerMartialArtEntity>> ListByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerMartialArtEntity>()
            .Where(x => x.PlayerId == playerId)
            .OrderBy(x => x.MartialArtId)
            .ToListAsync(cancellationToken);

    public Task<PlayerMartialArtEntity?> GetByPlayerAndMartialArtAsync(Guid playerId, int martialArtId, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerMartialArtEntity>()
            .FirstOrDefaultAsync(x => x.PlayerId == playerId && x.MartialArtId == martialArtId, cancellationToken);

    public Task<long> CreateAsync(PlayerMartialArtEntity entity, CancellationToken cancellationToken = default) =>
        _db.InsertEntityWithInt64IdentityAsync(entity, cancellationToken);

    public Task<int> UpdateAsync(PlayerMartialArtEntity entity, CancellationToken cancellationToken = default) =>
        _db.UpdateEntityAsync(entity, cancellationToken);
}
