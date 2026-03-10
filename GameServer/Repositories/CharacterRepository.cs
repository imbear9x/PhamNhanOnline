using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class CharacterRepository
{
    private readonly GameDb _db;

    public CharacterRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<Character>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<Character>().ToListAsync(cancellationToken);

    public Task<Character?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.GetTable<Character>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Guid> CreateAsync(Character entity, CancellationToken cancellationToken = default)
    {
        await _db.InsertAsync(entity, token: cancellationToken);
        return entity.Id;
    }

    public Task<int> UpdateAsync(Character entity, CancellationToken cancellationToken = default) =>
        _db.UpdateAsync(entity, token: cancellationToken);

    public Task<int> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.GetTable<Character>().Where(x => x.Id == id).DeleteAsync(cancellationToken);

    public Task<List<Character>> ListByAccountAsync(Guid accountId, CancellationToken cancellationToken = default) =>
        _db.GetTable<Character>().Where(x => x.AccountId == accountId).ToListAsync(cancellationToken);

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalized = (name ?? string.Empty).Trim().ToLowerInvariant();
        return _db.GetTable<Character>()
            .AnyAsync(x => Sql.Lower(x.Name) == normalized, cancellationToken);
    }
}

