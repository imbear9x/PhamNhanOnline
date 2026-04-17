using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class GameRandomLuckTagRepository
{
    private readonly GameDb _db;

    public GameRandomLuckTagRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<GameRandomLuckTagEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<GameRandomLuckTagEntity>().ToListAsync(cancellationToken);
}
