using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class PlayerCaveRepository
{
    private readonly GameDb _db;

    public PlayerCaveRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<PlayerCaveEntity>> ListByOwnerAsync(Guid ownerCharacterId, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerCaveEntity>()
            .Where(x => x.OwnerCharacterId == ownerCharacterId)
            .OrderByDescending(x => x.IsHome)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

    public Task<PlayerCaveEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerCaveEntity>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<PlayerCaveEntity?> GetHomeByOwnerAsync(Guid ownerCharacterId, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerCaveEntity>()
            .FirstOrDefaultAsync(x => x.OwnerCharacterId == ownerCharacterId && x.IsHome, cancellationToken);

    public Task<long> CreateAsync(PlayerCaveEntity entity, CancellationToken cancellationToken = default) =>
        _db.InsertWithInt64IdentityAsync(entity, token: cancellationToken);

    public Task<int> UpdateAsync(PlayerCaveEntity entity, CancellationToken cancellationToken = default) =>
        _db.UpdateAsync(entity, token: cancellationToken);
}

