using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class CraftRecipeRequirementRepository
{
    private readonly GameDb _db;

    public CraftRecipeRequirementRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<CraftRecipeRequirementEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<CraftRecipeRequirementEntity>().ToListAsync(cancellationToken);
}
