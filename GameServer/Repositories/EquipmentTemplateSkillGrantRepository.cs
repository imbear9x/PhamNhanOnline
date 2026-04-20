using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class EquipmentTemplateSkillGrantRepository
{
    private readonly GameDb _db;

    public EquipmentTemplateSkillGrantRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<EquipmentTemplateSkillGrantEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<EquipmentTemplateSkillGrantEntity>()
            .OrderBy(x => x.EquipmentTemplateId)
            .ThenBy(x => x.DisplayOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
}
