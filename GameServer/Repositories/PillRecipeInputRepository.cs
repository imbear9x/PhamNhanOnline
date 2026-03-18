using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class PillRecipeInputRepository
{
    private readonly GameDb _db;

    public PillRecipeInputRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<PillRecipeInputEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<PillRecipeInputEntity>().ToListAsync(cancellationToken);
}

