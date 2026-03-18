using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class MartialArtRepository
{
    private readonly GameDb _db;

    public MartialArtRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<MartialArtEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _db.GetTable<MartialArtEntity>().ToListAsync(cancellationToken);

    public Task<MartialArtEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.GetTable<MartialArtEntity>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
}
