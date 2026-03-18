using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class MartialArtSkillScalingRepository
{
    private readonly GameDb _db;

    public MartialArtSkillScalingRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<MartialArtSkillScalingEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<MartialArtSkillScalingEntity>().ToListAsync(cancellationToken);
}
