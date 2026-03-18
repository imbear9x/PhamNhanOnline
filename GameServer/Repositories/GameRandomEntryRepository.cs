using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class GameRandomEntryRepository
{
    private readonly GameDb _db;

    public GameRandomEntryRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<GameRandomEntryEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<GameRandomEntryEntity>().ToListAsync(cancellationToken);
}
