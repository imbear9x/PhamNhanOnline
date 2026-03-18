using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class CharacterBaseStatRepository
{
    private readonly GameDb _db;

    public CharacterBaseStatRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<CharacterBaseStat>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<CharacterBaseStat>().ToListAsync(cancellationToken);

    public Task<CharacterBaseStat?> GetByIdAsync(Guid characterId, CancellationToken cancellationToken = default) =>
        _db.GetTable<CharacterBaseStat>().FirstOrDefaultAsync(x => x.CharacterId == characterId, cancellationToken);

    public async Task<Guid> CreateAsync(CharacterBaseStat entity, CancellationToken cancellationToken = default)
    {
        await _db.InsertEntityAsync(entity, cancellationToken);
        return entity.CharacterId;
    }

    public Task<int> UpdateAsync(CharacterBaseStat entity, CancellationToken cancellationToken = default) =>
        _db.UpdateEntityAsync(entity, cancellationToken);

    public Task<int> DeleteAsync(Guid characterId, CancellationToken cancellationToken = default) =>
        _db.GetTable<CharacterBaseStat>().Where(x => x.CharacterId == characterId).DeleteAsync(cancellationToken);
}
