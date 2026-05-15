using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class BagGradeConfigRepository
{
    private readonly GameDb _db;

    public BagGradeConfigRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<BagGradeConfigEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<BagGradeConfigEntity>()
            .OrderBy(x => x.Grade)
            .ToListAsync(cancellationToken);

    public Task<BagGradeConfigEntity?> GetByGradeAsync(int grade, CancellationToken cancellationToken = default) =>
        _db.GetTable<BagGradeConfigEntity>()
            .FirstOrDefaultAsync(x => x.Grade == grade, cancellationToken);
}