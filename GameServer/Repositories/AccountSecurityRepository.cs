using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class AccountSecurityRepository
{
    private readonly GameDb _db;

    public AccountSecurityRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<AccountSecurity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<AccountSecurity>().ToListAsync(cancellationToken);

    public Task<AccountSecurity?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken = default) =>
        _db.GetTable<AccountSecurity>().FirstOrDefaultAsync(x => x.AccountId == accountId, cancellationToken);

    public async Task<Guid> CreateAsync(AccountSecurity entity, CancellationToken cancellationToken = default)
    {
        await _db.InsertEntityAsync(entity, cancellationToken);
        return entity.AccountId;
    }

    public Task<int> UpdateAsync(AccountSecurity entity, CancellationToken cancellationToken = default) =>
        _db.UpdateEntityAsync(entity, cancellationToken);

    public Task<int> DeleteAsync(Guid accountId, CancellationToken cancellationToken = default) =>
        _db.GetTable<AccountSecurity>().Where(x => x.AccountId == accountId).DeleteAsync(cancellationToken);
}

