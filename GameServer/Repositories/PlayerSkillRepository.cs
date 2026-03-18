using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class PlayerSkillRepository
{
    private readonly GameDb _db;

    public PlayerSkillRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<PlayerSkillEntity>> ListByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerSkillEntity>()
            .Where(x => x.PlayerId == playerId)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

    public Task<PlayerSkillEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerSkillEntity>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<long> CreateAsync(PlayerSkillEntity entity, CancellationToken cancellationToken = default) =>
        _db.InsertWithInt64IdentityAsync(entity, token: cancellationToken);

    public Task<int> UpdateAsync(PlayerSkillEntity entity, CancellationToken cancellationToken = default) =>
        _db.UpdateAsync(entity, token: cancellationToken);
}
