using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class CraftRecipeRepository
{
    private readonly GameDb _db;

    public CraftRecipeRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<CraftRecipeEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<CraftRecipeEntity>().ToListAsync(cancellationToken);

    public Task<CraftRecipeEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.GetTable<CraftRecipeEntity>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
}
