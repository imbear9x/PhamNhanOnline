using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class PlayerGardenPlotRepository
{
    private readonly GameDb _db;

    public PlayerGardenPlotRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<PlayerGardenPlotEntity>> ListByCaveIdAsync(long caveId, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerGardenPlotEntity>()
            .Where(x => x.CaveId == caveId)
            .OrderBy(x => x.PlotIndex)
            .ToListAsync(cancellationToken);

    public Task<PlayerGardenPlotEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerGardenPlotEntity>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<PlayerGardenPlotEntity?> GetByCaveAndPlotIndexAsync(long caveId, int plotIndex, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerGardenPlotEntity>()
            .FirstOrDefaultAsync(x => x.CaveId == caveId && x.PlotIndex == plotIndex, cancellationToken);

    public Task<long> CreateAsync(PlayerGardenPlotEntity entity, CancellationToken cancellationToken = default) =>
        _db.InsertWithInt64IdentityAsync(entity, token: cancellationToken);

    public Task<int> UpdateAsync(PlayerGardenPlotEntity entity, CancellationToken cancellationToken = default) =>
        _db.UpdateAsync(entity, token: cancellationToken);
}

