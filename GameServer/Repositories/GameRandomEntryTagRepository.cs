using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class GameRandomEntryTagRepository
{
    private readonly GameDb _db;

    public GameRandomEntryTagRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<GameRandomEntryTagEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<GameRandomEntryTagEntity>().ToListAsync(cancellationToken);
}
