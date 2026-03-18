using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class PillRecipeMasteryStageRepository
{
    private readonly GameDb _db;

    public PillRecipeMasteryStageRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<PillRecipeMasteryStageEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<PillRecipeMasteryStageEntity>().ToListAsync(cancellationToken);
}
