using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class AccountRepository
{
    private readonly GameDb _db;

    public AccountRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<Account>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<Account>().ToListAsync(cancellationToken);

    public Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.GetTable<Account>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Guid> CreateAsync(Account entity, CancellationToken cancellationToken = default)
    {
        await _db.InsertAsync(entity, token: cancellationToken);
        return entity.Id;
    }

    public Task<int> UpdateAsync(Account entity, CancellationToken cancellationToken = default) =>
        _db.UpdateAsync(entity, token: cancellationToken);

    public Task<int> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.GetTable<Account>().Where(x => x.Id == id).DeleteAsync(cancellationToken);
}

