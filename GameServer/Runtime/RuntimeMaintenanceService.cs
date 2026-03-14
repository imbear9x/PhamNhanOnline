using System.Diagnostics;
using GameServer.Diagnostics;
using GameServer.Time;
using GameShared.Logging;

namespace GameServer.Runtime;

public sealed class RuntimeMaintenanceService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(50);

    private readonly CharacterRuntimeService _runtimeService;
    private readonly CharacterRuntimeSaveService _runtimeSaveService;
    private readonly GameTimeService _gameTimeService;
    private readonly ServerMetricsService _metrics;

    private readonly CancellationTokenSource _cts = new();
    private Thread? _thread;
    private DateTime _nextRuntimeSaveUtc = DateTime.UtcNow;
    private DateTime _nextDerivedStateRefreshUtc = DateTime.UtcNow;

    public RuntimeMaintenanceService(
        CharacterRuntimeService runtimeService,
        CharacterRuntimeSaveService runtimeSaveService,
        GameTimeService gameTimeService,
        ServerMetricsService metrics)
    {
        _runtimeService = runtimeService;
        _runtimeSaveService = runtimeSaveService;
        _gameTimeService = gameTimeService;
        _metrics = metrics;
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

        return new MaintenanceActivity(ranSave, ranRefresh);
    }

    private readonly record struct MaintenanceActivity(bool RanSave, bool RanRefresh);
}
