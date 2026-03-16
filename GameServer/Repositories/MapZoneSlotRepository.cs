using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class MapZoneSlotRepository
{
    private readonly GameDb _db;

    public MapZoneSlotRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<MapZoneSlotEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<MapZoneSlotEntity>()
            .OrderBy(x => x.MapTemplateId)
            .ThenBy(x => x.ZoneIndex)
            .ToListAsync(cancellationToken);

    public Task<List<MapZoneSlotEntity>> ListByMapTemplateIdAsync(int mapTemplateId, CancellationToken cancellationToken = default) =>
        _db.GetTable<MapZoneSlotEntity>()
            .Where(x => x.MapTemplateId == mapTemplateId)
            .OrderBy(x => x.ZoneIndex)
            .ToListAsync(cancellationToken);
}
