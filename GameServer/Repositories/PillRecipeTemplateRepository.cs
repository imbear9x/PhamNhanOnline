using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class PillRecipeTemplateRepository
{
    private readonly GameDb _db;

    public PillRecipeTemplateRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<PillRecipeTemplateEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<PillRecipeTemplateEntity>().ToListAsync(cancellationToken);

    public Task<PillRecipeTemplateEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.GetTable<PillRecipeTemplateEntity>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
}

