using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class PlayerNotificationRepository
{
    private readonly GameDb _db;

    public PlayerNotificationRepository(GameDb db)
    {
        _db = db;
    }

    public Task<PlayerNotificationEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerNotificationEntity>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<List<PlayerNotificationEntity>> ListUnreadByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerNotificationEntity>()
            .Where(x => x.PlayerId == playerId && x.ReadAtUtc == null)
            .OrderBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

    public Task<PlayerNotificationEntity?> GetLatestUnreadByTypeAsync(
        Guid playerId,
        int notificationType,
        int sourceType,
        CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerNotificationEntity>()
            .Where(x =>
                x.PlayerId == playerId &&
                x.ReadAtUtc == null &&
                x.NotificationType == notificationType &&
                x.SourceType == sourceType)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<long> CreateAsync(PlayerNotificationEntity entity, CancellationToken cancellationToken = default) =>
        _db.InsertWithInt64IdentityAsync(entity, token: cancellationToken);

    public Task<int> UpdateAsync(PlayerNotificationEntity entity, CancellationToken cancellationToken = default) =>
        _db.UpdateAsync(entity, token: cancellationToken);
}
