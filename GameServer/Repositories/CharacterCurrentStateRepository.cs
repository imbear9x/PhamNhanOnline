using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class CharacterCurrentStateRepository
{
    private readonly GameDb _db;

    public CharacterCurrentStateRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<CharacterCurrentState>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<CharacterCurrentState>().ToListAsync(cancellationToken);

    public Task<CharacterCurrentState?> GetByIdAsync(Guid characterId, CancellationToken cancellationToken = default) =>
        _db.GetTable<CharacterCurrentState>().FirstOrDefaultAsync(x => x.CharacterId == characterId, cancellationToken);

    public async Task<Guid> CreateAsync(CharacterCurrentState entity, CancellationToken cancellationToken = default)
    {
        await _db.InsertEntityAsync(entity, cancellationToken);
        return entity.CharacterId;
    }

    public Task<int> UpdateAsync(CharacterCurrentState entity, CancellationToken cancellationToken = default) =>
        _db.UpdateEntityAsync(entity, cancellationToken);

    public Task<int> DeleteAsync(Guid characterId, CancellationToken cancellationToken = default) =>
        _db.GetTable<CharacterCurrentState>().Where(x => x.CharacterId == characterId).DeleteAsync(cancellationToken);
}
