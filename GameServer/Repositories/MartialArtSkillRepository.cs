using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class MartialArtSkillRepository
{
    private readonly GameDb _db;

    public MartialArtSkillRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<MartialArtSkillEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<MartialArtSkillEntity>().ToListAsync(cancellationToken);
}
