using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class SpiritualEnergyTemplateRepository
{
    private readonly GameDb _db;

    public SpiritualEnergyTemplateRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<SpiritualEnergyTemplateEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<SpiritualEnergyTemplateEntity>().ToListAsync(cancellationToken);

    public Task<SpiritualEnergyTemplateEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.GetTable<SpiritualEnergyTemplateEntity>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
}
