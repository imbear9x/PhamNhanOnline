using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class GameRandomTableRepository
{
    private readonly GameDb _db;

    public GameRandomTableRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<GameRandomTableEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<GameRandomTableEntity>().ToListAsync(cancellationToken);
}
