using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class SkillRepository
{
    private readonly GameDb _db;

    public SkillRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<SkillEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<SkillEntity>().ToListAsync(cancellationToken);

    public Task<SkillEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.GetTable<SkillEntity>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
}
