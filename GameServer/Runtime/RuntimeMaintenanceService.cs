using System.Diagnostics;
using GameServer.Diagnostics;
using GameServer.Services;
using GameServer.Time;
using GameServer.World;
using GameShared.Logging;

namespace GameServer.Runtime;

public sealed class RuntimeMaintenanceService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan PracticeSettlementInterval = TimeSpan.FromSeconds(1);

    private readonly CharacterRuntimeService _runtimeService;
    private readonly CharacterRuntimeSaveService _runtimeSaveService;
    private readonly CharacterCultivationService _cultivationService;
    private readonly AlchemyPracticeService _alchemyPracticeService;
    private readonly GameTimeService _gameTimeService;
    private readonly ServerMetricsService _metrics;
    private readonly WorldManager _worldManager;

    private readonly CancellationTokenSource _cts = new();
    private Thread? _thread;
    private DateTime _nextRuntimeSaveUtc = DateTime.UtcNow;
    private DateTime _nextDerivedStateRefreshUtc = DateTime.UtcNow;
    private DateTime _nextCultivationSettlementUtc = DateTime.UtcNow;
    private DateTime _nextPracticeSettlementUtc = DateTime.UtcNow;
    private DateTime _nextEmptyInstanceCleanupUtc = DateTime.UtcNow;

    public RuntimeMaintenanceService(
        CharacterRuntimeService runtimeService,
        CharacterRuntimeSaveService runtimeSaveService,
        CharacterCultivationService cultivationService,
        AlchemyPracticeService alchemyPracticeService,
        GameTimeService gameTimeService,
        ServerMetricsService metrics,
        WorldManager worldManager)
    {
        _runtimeService = runtimeService;
        _runtimeSaveService = runtimeSaveService;
        _cultivationService = cultivationService;
        _alchemyPracticeService = alchemyPracticeService;
        _gameTimeService = gameTimeService;
        _metrics = metrics;
        _worldManager = worldManager;
    }

    public void Start()
    {
        if (_thread is not null)
            return;

        _thread = new Thread(Run)
        {
            IsBackground = true,
            Name = "GameServer.RuntimeMaintenance"
        };
        _thread.Start();
    }

    public void Stop()
    {
        _cts.Cancel();
        _thread?.Join(TimeSpan.FromSeconds(5));
        _thread = null;

        try
        {
            _runtimeSaveService.SaveDirtyPlayersAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Final runtime save failed during maintenance shutdown.");
        }
    }

    private void Run()
    {
        var token = _cts.Token;
        var stopwatch = Stopwatch.StartNew();
        var nextTick = stopwatch.Elapsed;

        while (!token.IsCancellationRequested)
        {
            var tickStart = stopwatch.Elapsed;
            var activity = UpdateMaintenance(token);
            var tickDuration = stopwatch.Elapsed - tickStart;

            nextTick += TickInterval;
            var remaining = nextTick - stopwatch.Elapsed;
            var overrun = remaining <= TimeSpan.Zero;
            _metrics.RecordMaintenanceTick(tickDuration, overrun, activity.RanSave, activity.RanRefresh);

            if (remaining > TimeSpan.Zero)
            {
                token.WaitHandle.WaitOne(remaining);
                continue;
            }

            if (-remaining > TickInterval)
            {
                nextTick = stopwatch.Elapsed;
            }
        }
    }

    private MaintenanceActivity UpdateMaintenance(CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var ranSave = false;
        var ranRefresh = false;

        if (utcNow >= _nextRuntimeSaveUtc)
        {
            ranSave = true;
            try
            {
                _runtimeSaveService.SaveDirtyPlayersAsync(cancellationToken).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Periodic runtime save failed.");
            }

            _nextRuntimeSaveUtc = DateTime.UtcNow.AddSeconds(_gameTimeService.Config.RuntimeSaveIntervalSeconds);
        }

        if (utcNow >= _nextDerivedStateRefreshUtc)
        {
            ranRefresh = true;
            try
            {
                _runtimeService.RefreshTimeDerivedStateForOnlinePlayersAsync(cancellationToken).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Periodic time-derived runtime refresh failed.");
            }

            _nextDerivedStateRefreshUtc = DateTime.UtcNow.AddSeconds(_gameTimeService.Config.DerivedStateRefreshIntervalSeconds);
        }

        if (utcNow >= _nextCultivationSettlementUtc)
        {
            try
            {
                _cultivationService.SettleCultivationAsync(cancellationToken).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Periodic cultivation settlement failed.");
            }

            _nextCultivationSettlementUtc = DateTime.UtcNow.Add(_cultivationService.SettlementInterval);
        }

        if (utcNow >= _nextPracticeSettlementUtc)
        {
            try
            {
                _alchemyPracticeService.EnsureDueSessionsCompletedAsync(cancellationToken).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Periodic alchemy practice settlement failed.");
            }

            _nextPracticeSettlementUtc = DateTime.UtcNow.Add(PracticeSettlementInterval);
        }

        if (utcNow >= _nextEmptyInstanceCleanupUtc)
        {
            _worldManager.MapManager.CleanupExpiredInstances(utcNow);
            _nextEmptyInstanceCleanupUtc = DateTime.UtcNow.AddSeconds(15);
        }

        return new MaintenanceActivity(ranSave, ranRefresh);
    }

    private readonly record struct MaintenanceActivity(bool RanSave, bool RanRefresh);
}
