using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class MartialArtStageRepository
{
    private readonly GameDb _db;

    public MartialArtStageRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<MartialArtStageEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<MartialArtStageEntity>().ToListAsync(cancellationToken);
}
