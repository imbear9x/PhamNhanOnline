using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class PlayerSkillLoadoutRepository
{
    private readonly GameDb _db;

    public PlayerSkillLoadoutRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<PlayerSkillLoadoutEntity>> ListByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerSkillLoadoutEntity>()
            .Where(x => x.PlayerId == playerId)
            .OrderBy(x => x.SlotIndex)
            .ToListAsync(cancellationToken);

    public Task<PlayerSkillLoadoutEntity?> GetByPlayerAndSlotAsync(Guid playerId, int slotIndex, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerSkillLoadoutEntity>()
            .FirstOrDefaultAsync(x => x.PlayerId == playerId && x.SlotIndex == slotIndex, cancellationToken);

    public Task<long> CreateAsync(PlayerSkillLoadoutEntity entity, CancellationToken cancellationToken = default) =>
        _db.InsertEntityWithInt64IdentityAsync(entity, cancellationToken);

    public Task<int> UpdateAsync(PlayerSkillLoadoutEntity entity, CancellationToken cancellationToken = default) =>
        _db.UpdateEntityAsync(entity, cancellationToken);

    public Task<int> DeleteAsync(PlayerSkillLoadoutEntity entity, CancellationToken cancellationToken = default) =>
        _db.DeleteAsync(entity, token: cancellationToken);
}
