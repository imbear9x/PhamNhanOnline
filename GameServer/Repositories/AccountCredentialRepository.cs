using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class AccountCredentialRepository
{
    private readonly GameDb _db;

    public AccountCredentialRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<AccountCredential>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<AccountCredential>().ToListAsync(cancellationToken);

    public Task<AccountCredential?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.GetTable<AccountCredential>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Guid> CreateAsync(AccountCredential entity, CancellationToken cancellationToken = default)
    {
        await _db.InsertAsync(entity, token: cancellationToken);
        return entity.Id;
    }

    public Task<int> UpdateAsync(AccountCredential entity, CancellationToken cancellationToken = default) =>
        _db.UpdateAsync(entity, token: cancellationToken);

    public Task<int> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.GetTable<AccountCredential>().Where(x => x.Id == id).DeleteAsync(cancellationToken);

    public Task<AccountCredential?> GetByAccountAndProviderAsync(
        Guid accountId,
        string provider,
        CancellationToken cancellationToken = default) =>
        _db.GetTable<AccountCredential>()
            .FirstOrDefaultAsync(
                x => x.AccountId == accountId && x.Provider == provider,
                cancellationToken);

    public Task<AccountCredential?> GetByProviderUserIdAsync(
        string provider,
        string providerUserId,
        CancellationToken cancellationToken = default) =>
        _db.GetTable<AccountCredential>()
            .FirstOrDefaultAsync(
                x => x.Provider == provider && x.ProviderUserId == providerUserId,
                cancellationToken);

    public Task<List<AccountCredential>> ListByAccountAsync(Guid accountId, CancellationToken cancellationToken = default) =>
        _db.GetTable<AccountCredential>().Where(x => x.AccountId == accountId).ToListAsync(cancellationToken);
}

