using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class PlayerSkillGrantSourceRepository
{
    private readonly GameDb _db;

    public PlayerSkillGrantSourceRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<PlayerSkillGrantSourceEntity>> ListByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerSkillGrantSourceEntity>()
            .Where(x => x.PlayerId == playerId)
            .OrderBy(x => x.PlayerSkillId)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

    public Task<long> CreateAsync(PlayerSkillGrantSourceEntity entity, CancellationToken cancellationToken = default) =>
        _db.InsertEntityWithInt64IdentityAsync(entity, cancellationToken);

    public Task<int> UpdateAsync(PlayerSkillGrantSourceEntity entity, CancellationToken cancellationToken = default) =>
        _db.UpdateEntityAsync(entity, cancellationToken);

    public Task<int> DeleteAsync(PlayerSkillGrantSourceEntity entity, CancellationToken cancellationToken = default) =>
        _db.DeleteAsync(entity, token: cancellationToken);
}
