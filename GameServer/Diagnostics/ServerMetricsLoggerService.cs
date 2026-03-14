using GameServer.World;
using GameShared.Logging;

namespace GameServer.Diagnostics;

public sealed class ServerMetricsLoggerService
{
    private static readonly TimeSpan LogInterval = TimeSpan.FromSeconds(30);

    private readonly ServerMetricsService _metrics;
    private readonly WorldManager _worldManager;
    private readonly CancellationTokenSource _cts = new();
    private Thread? _thread;

    public ServerMetricsLoggerService(ServerMetricsService metrics, WorldManager worldManager)
    {
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
            Name = "GameServer.MetricsLogger"
        };
        _thread.Start();
    }

    public void Stop()
    {
        _cts.Cancel();
        _thread?.Join(TimeSpan.FromSeconds(2));
        _thread = null;
    }

    private void Run()
    {
        var token = _cts.Token;
        while (!token.IsCancellationRequested)
        {
            if (token.WaitHandle.WaitOne(LogInterval))
                break;

            try
            {
                LogSnapshot();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to log server metrics snapshot.");
            }
        }
    }

    private void LogSnapshot()
    {
        var snapshot = _metrics.CaptureSnapshot(_worldManager.OnlinePlayers.Count);
        Logger.Info(
            "ServerMetrics " +
            $"OnlinePlayers={snapshot.OnlinePlayers}, " +
            $"ActiveInboundSessions={snapshot.ActiveInboundSessions}, " +
            $"QueuedInboundPackets={snapshot.TotalQueuedInboundPackets}, " +
            $"MaxQueueDepth={snapshot.MaxObservedQueueDepth}, " +
            $"InboundEnqueued={snapshot.InboundPacketsEnqueued}, " +
            $"InboundProcessed={snapshot.InboundPacketsProcessed}, " +
            $"InboundDropped={snapshot.InboundPacketsDropped}, " +
            $"InboundExceptions={snapshot.InboundPacketProcessingExceptions}, " +
            $"AvgInboundMs={snapshot.AverageInboundPacketProcessingMs:F2}, " +
            $"MaxInboundMs={snapshot.MaxInboundPacketProcessingMs:F2}, " +
            $"WorldTicks={snapshot.WorldTicks}, " +
            $"WorldTickOverruns={snapshot.WorldTickOverruns}, " +
            $"AvgWorldTickMs={snapshot.AverageWorldTickMs:F2}, " +
            $"MaxWorldTickMs={snapshot.MaxWorldTickMs:F2}, " +
            $"WorldInstances={snapshot.LastWorldInstanceCount}, " +
            $"MaintenanceTicks={snapshot.MaintenanceTicks}, " +
            $"MaintenanceTickOverruns={snapshot.MaintenanceTickOverruns}, " +
            $"AvgMaintenanceTickMs={snapshot.AverageMaintenanceTickMs:F2}, " +
            $"MaxMaintenanceTickMs={snapshot.MaxMaintenanceTickMs:F2}, " +
            $"MaintenanceSaves={snapshot.MaintenanceSaveRuns}, " +
            $"MaintenanceRefreshes={snapshot.MaintenanceRefreshRuns}");
    }
}
