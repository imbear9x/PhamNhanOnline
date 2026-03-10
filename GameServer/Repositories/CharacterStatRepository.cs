using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class CharacterStatRepository
{
    private readonly GameDb _db;

    public CharacterStatRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<CharacterStat>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<CharacterStat>().ToListAsync(cancellationToken);

    public Task<CharacterStat?> GetByIdAsync(Guid characterId, CancellationToken cancellationToken = default) =>
        _db.GetTable<CharacterStat>().FirstOrDefaultAsync(x => x.CharacterId == characterId, cancellationToken);

    public async Task<Guid> CreateAsync(CharacterStat entity, CancellationToken cancellationToken = default)
    {
        await _db.InsertAsync(entity, token: cancellationToken);
        return entity.CharacterId;
    }

    public Task<int> UpdateAsync(CharacterStat entity, CancellationToken cancellationToken = default) =>
        _db.UpdateAsync(entity, token: cancellationToken);

    public Task<int> DeleteAsync(Guid characterId, CancellationToken cancellationToken = default) =>
        _db.GetTable<CharacterStat>().Where(x => x.CharacterId == characterId).DeleteAsync(cancellationToken);
}

