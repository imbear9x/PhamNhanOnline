using System.Threading;
using GameServer.Config;
using GameServer.Repositories;
using GameShared.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace GameServer.Runtime;

public sealed class HerbExpiryBackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly GameConfigValues _gameConfig;
    private DateTime _nextSweepUtc = DateTime.UtcNow;
    private int _sweepInProgress;

    public HerbExpiryBackgroundService(IServiceScopeFactory scopeFactory, GameConfigValues gameConfig)
    {
        _scopeFactory = scopeFactory;
        _gameConfig = gameConfig;
    }

    public void ScheduleSweepIfDue(CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        if (utcNow < _nextSweepUtc)
            return;

        if (Interlocked.CompareExchange(ref _sweepInProgress, 1, 0) != 0)
            return;

        _nextSweepUtc = utcNow.Add(_gameConfig.HerbExpirySweepInterval);
        _ = SweepExpiredHerbsAsync(cancellationToken);
    }

    private async Task SweepExpiredHerbsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<PlayerHerbRepository>();
            var utcNow = DateTime.UtcNow;
            var deleted = await repository.DeleteExpiredInventoryHerbsAsync(utcNow, cancellationToken);
            if (deleted > 0)
                Logger.Info($"[HerbExpirySweep] Deleted {deleted} expired herb(s)");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Herb expiry sweep failed.");
        }
        finally
        {
            Interlocked.Exchange(ref _sweepInProgress, 0);
        }
    }
}
