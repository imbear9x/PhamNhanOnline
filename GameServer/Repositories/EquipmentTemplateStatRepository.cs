using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class EquipmentTemplateStatRepository
{
    private readonly GameDb _db;

    public EquipmentTemplateStatRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<EquipmentTemplateStatEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<EquipmentTemplateStatEntity>().ToListAsync(cancellationToken);
}
