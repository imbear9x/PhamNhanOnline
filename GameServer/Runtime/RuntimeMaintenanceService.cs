using GameServer.Time;
using GameShared.Logging;

namespace GameServer.Runtime;

public sealed class RuntimeMaintenanceService
{
    private readonly CharacterRuntimeService _runtimeService;
    private readonly CharacterRuntimeSaveService _runtimeSaveService;
    private readonly GameTimeService _gameTimeService;

    private readonly CancellationTokenSource _cts = new();
    private Thread? _thread;
    private DateTime _nextRuntimeSaveUtc = DateTime.UtcNow;
    private DateTime _nextDerivedStateRefreshUtc = DateTime.UtcNow;

    public RuntimeMaintenanceService(
        CharacterRuntimeService runtimeService,
        CharacterRuntimeSaveService runtimeSaveService,
        GameTimeService gameTimeService)
    {
        _runtimeService = runtimeService;
        _runtimeSaveService = runtimeSaveService;
        _gameTimeService = gameTimeService;
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

        while (!token.IsCancellationRequested)
        {
            UpdateMaintenance(token);
            Thread.Sleep(50);
        }
    }

    private void UpdateMaintenance(CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;

        if (utcNow >= _nextRuntimeSaveUtc)
        {
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
    }
}
