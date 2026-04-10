using GameServer.Entities;
using GameServer.Runtime;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class PlayerPracticeSessionRepository
{
    private readonly GameDb _db;

    public PlayerPracticeSessionRepository(GameDb db)
    {
        _db = db;
    }

    public Task<PlayerPracticeSessionEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerPracticeSessionEntity>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<List<PlayerPracticeSessionEntity>> ListByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerPracticeSessionEntity>()
            .Where(x => x.PlayerId == playerId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<PlayerPracticeSessionEntity?> GetBlockingSessionAsync(Guid playerId, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerPracticeSessionEntity>()
            .Where(x => x.PlayerId == playerId &&
                        (x.PracticeState == (int)PracticeSessionState.Active ||
                         x.PracticeState == (int)PracticeSessionState.Paused))
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<PlayerPracticeSessionEntity?> GetLatestByTypeAsync(
        Guid playerId,
        PracticeType practiceType,
        CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerPracticeSessionEntity>()
            .Where(x => x.PlayerId == playerId && x.PracticeType == (int)practiceType)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<List<PlayerPracticeSessionEntity>> ListActiveByTypeAsync(
        PracticeType practiceType,
        CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerPracticeSessionEntity>()
            .Where(x => x.PracticeType == (int)practiceType && x.PracticeState == (int)PracticeSessionState.Active)
            .ToListAsync(cancellationToken);

    public Task<long> CreateAsync(PlayerPracticeSessionEntity entity, CancellationToken cancellationToken = default) =>
        _db.InsertWithInt64IdentityAsync(entity, token: cancellationToken);

    public Task<int> UpdateAsync(PlayerPracticeSessionEntity entity, CancellationToken cancellationToken = default) =>
        _db.UpdateAsync(entity, token: cancellationToken);
}
