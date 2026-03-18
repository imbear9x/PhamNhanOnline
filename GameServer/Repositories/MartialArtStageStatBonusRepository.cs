using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class MartialArtStageStatBonusRepository
{
    private readonly GameDb _db;

    public MartialArtStageStatBonusRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<MartialArtStageStatBonusEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<MartialArtStageStatBonusEntity>().ToListAsync(cancellationToken);
}
