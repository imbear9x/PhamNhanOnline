using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class GameTimeStateRepository
{
    public const int PrimaryId = 1;

    private readonly GameDb _db;

    public GameTimeStateRepository(GameDb db)
    {
        _db = db;
    }

    public Task<GameTimeState?> GetPrimaryAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<GameTimeState>().FirstOrDefaultAsync(x => x.Id == PrimaryId, cancellationToken);

    public async Task<int> CreateAsync(GameTimeState entity, CancellationToken cancellationToken = default)
    {
        await _db.InsertAsync(entity, token: cancellationToken);
        return entity.Id;
    }

    public Task<int> UpdateAsync(GameTimeState entity, CancellationToken cancellationToken = default) =>
        _db.GetTable<GameTimeState>()
            .Where(x => x.Id == entity.Id)
            .Set(x => x.AnchorUtc, entity.AnchorUtc)
            .Set(x => x.AnchorGameMinute, entity.AnchorGameMinute)
            .Set(x => x.GameMinutesPerRealMinute, entity.GameMinutesPerRealMinute)
            .Set(x => x.DaysPerGameYear, entity.DaysPerGameYear)
            .Set(x => x.RuntimeSaveIntervalSeconds, entity.RuntimeSaveIntervalSeconds)
            .Set(x => x.DerivedStateRefreshIntervalSeconds, entity.DerivedStateRefreshIntervalSeconds)
            .Set(x => x.UpdatedAt, entity.UpdatedAt)
            .UpdateAsync(cancellationToken);
}
