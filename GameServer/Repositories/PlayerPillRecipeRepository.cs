using GameServer.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace GameServer.Repositories;

public sealed class PlayerPillRecipeRepository
{
    private readonly GameDb _db;

    public PlayerPillRecipeRepository(GameDb db)
    {
        _db = db;
    }

    public Task<List<PlayerPillRecipeEntity>> ListByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerPillRecipeEntity>()
            .Where(x => x.PlayerId == playerId)
            .OrderBy(x => x.PillRecipeTemplateId)
            .ToListAsync(cancellationToken);

    public Task<PlayerPillRecipeEntity?> GetByPlayerAndRecipeAsync(Guid playerId, int recipeId, CancellationToken cancellationToken = default) =>
        _db.GetTable<PlayerPillRecipeEntity>()
            .FirstOrDefaultAsync(x => x.PlayerId == playerId && x.PillRecipeTemplateId == recipeId, cancellationToken);

    public Task<long> CreateAsync(PlayerPillRecipeEntity entity, CancellationToken cancellationToken = default) =>
        _db.InsertWithInt64IdentityAsync(entity, token: cancellationToken);

    public Task<int> UpdateAsync(PlayerPillRecipeEntity entity, CancellationToken cancellationToken = default) =>
        _db.UpdateAsync(entity, token: cancellationToken);
}

