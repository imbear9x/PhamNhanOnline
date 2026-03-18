using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class GameRandomFortuneTagRepository
{
    private readonly GameDb _db;

    public GameRandomFortuneTagRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<GameRandomFortuneTagEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<GameRandomFortuneTagEntity>().ToListAsync(cancellationToken);
}
