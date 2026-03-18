using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class CraftRecipeMutationBonusRepository
{
    private readonly GameDb _db;

    public CraftRecipeMutationBonusRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<CraftRecipeMutationBonusEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<CraftRecipeMutationBonusEntity>().ToListAsync(cancellationToken);
}
