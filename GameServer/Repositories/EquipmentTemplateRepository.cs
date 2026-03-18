using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class EquipmentTemplateRepository
{
    private readonly GameDb _db;

    public EquipmentTemplateRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<EquipmentTemplateEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<EquipmentTemplateEntity>().ToListAsync(cancellationToken);

    public Task<EquipmentTemplateEntity?> GetByItemTemplateIdAsync(int itemTemplateId, CancellationToken cancellationToken = default) =>
        _db.GetTable<EquipmentTemplateEntity>().FirstOrDefaultAsync(x => x.ItemTemplateId == itemTemplateId, cancellationToken);
}
