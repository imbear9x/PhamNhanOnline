using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class ServerRepository
{
    private readonly GameDb _db;

    public ServerRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<Server>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<Server>().ToListAsync(cancellationToken);

    public Task<Server?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.GetTable<Server>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<int> CreateAsync(Server entity, CancellationToken cancellationToken = default)
    {
        await _db.InsertEntityAsync(entity, cancellationToken);
        return entity.Id;
    }

    public Task<int> UpdateAsync(Server entity, CancellationToken cancellationToken = default) =>
        _db.UpdateEntityAsync(entity, cancellationToken);

    public Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        _db.GetTable<Server>().Where(x => x.Id == id).DeleteAsync(cancellationToken);
}

