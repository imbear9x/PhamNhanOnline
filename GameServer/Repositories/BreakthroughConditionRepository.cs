using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class BreakthroughConditionRepository
{
    private readonly GameDb _db;

    public BreakthroughConditionRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<BreakthroughCondition>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<BreakthroughCondition>().ToListAsync(cancellationToken);

    public Task<BreakthroughCondition?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.GetTable<BreakthroughCondition>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<int> CreateAsync(BreakthroughCondition entity, CancellationToken cancellationToken = default)
    {
        var id = await _db.InsertWithInt32IdentityAsync(entity, token: cancellationToken);
        entity.Id = id;
        return id;
    }

    public Task<int> UpdateAsync(BreakthroughCondition entity, CancellationToken cancellationToken = default) =>
        _db.UpdateAsync(entity, token: cancellationToken);

    public Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        _db.GetTable<BreakthroughCondition>().Where(x => x.Id == id).DeleteAsync(cancellationToken);
}

