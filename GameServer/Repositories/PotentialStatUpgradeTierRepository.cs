using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class PotentialStatUpgradeTierRepository
{
    private readonly GameDb _db;

    public PotentialStatUpgradeTierRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<PotentialStatUpgradeTierEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<PotentialStatUpgradeTierEntity>().ToListAsync(cancellationToken);
}
