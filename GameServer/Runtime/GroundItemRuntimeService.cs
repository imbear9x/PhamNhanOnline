using GameServer.Services;
using GameServer.World;
using GameShared.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace GameServer.Runtime;

public sealed class GroundItemRuntimeService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public GroundItemRuntimeService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void CleanupResidualGroundItemsOnStartup()
    {
        try
        {
            CleanupResidualGroundItemsOnStartupAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to cleanup residual ground items during server startup.");
            throw;
        }
    }

    public void ProcessDespawnedRewards(IReadOnlyCollection<GroundRewardDespawnRuntimeEvent> despawns)
    {
        if (despawns.Count == 0)
            return;

        try
        {
            ProcessDespawnedRewardsAsync(despawns).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to cleanup expired ground item instances.");
        }
    }

    private async Task CleanupResidualGroundItemsOnStartupAsync()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var itemService = scope.ServiceProvider.GetRequiredService<ItemService>();
        await itemService.CleanupResidualGroundItemsAsync();
    }

    private async Task ProcessDespawnedRewardsAsync(IReadOnlyCollection<GroundRewardDespawnRuntimeEvent> despawns)
    {
        var itemIds = despawns
            .Where(static x => x.DestroyItems && x.PlayerItemIds.Count > 0)
            .SelectMany(static x => x.PlayerItemIds)
            .Distinct()
            .ToArray();
        if (itemIds.Length == 0)
            return;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var itemService = scope.ServiceProvider.GetRequiredService<ItemService>();
        await itemService.CleanupExpiredGroundItemsAsync(itemIds);
    }
}
