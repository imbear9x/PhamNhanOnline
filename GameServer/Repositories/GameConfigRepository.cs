using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class GameConfigRepository
{
    private readonly GameDb _db;

    public GameConfigRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<GameConfigEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<GameConfigEntity>().ToListAsync(cancellationToken);

    public Task<GameConfigEntity?> GetByKeyAsync(string configKey, CancellationToken cancellationToken = default) =>
        _db.GetTable<GameConfigEntity>()
            .FirstOrDefaultAsync(x => x.ConfigKey == configKey, cancellationToken);
}
