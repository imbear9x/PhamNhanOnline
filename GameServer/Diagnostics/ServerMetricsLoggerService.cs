using GameServer.World;
using GameShared.Logging;

namespace GameServer.Diagnostics;

public sealed class ServerMetricsLoggerService
{
    private static readonly TimeSpan LogInterval = TimeSpan.FromSeconds(10);

    private readonly ServerMetricsService _metrics;
    private readonly WorldManager _worldManager;
    private readonly CancellationTokenSource _cts = new();
    private Thread? _thread;
    private ServerMetricsSnapshot? _lastSnapshot;
    private DateTime _lastSnapshotUtc = DateTime.UtcNow;

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
        var nowUtc = DateTime.UtcNow;
        var previous = _lastSnapshot;
        var elapsedSeconds = Math.Max(0.001, (nowUtc - _lastSnapshotUtc).TotalSeconds);
        var inboundBytesDelta = previous is null
            ? snapshot.TotalInboundPacketBytes
            : snapshot.TotalInboundPacketBytes - previous.TotalInboundPacketBytes;
        var outboundBytesDelta = previous is null
            ? snapshot.TotalOutboundPacketBytes
            : snapshot.TotalOutboundPacketBytes - previous.TotalOutboundPacketBytes;
        var inboundPacketsDelta = previous is null
            ? snapshot.InboundPacketsEnqueued
            : snapshot.InboundPacketsEnqueued - previous.InboundPacketsEnqueued;
        var outboundPacketsDelta = previous is null
            ? snapshot.OutboundPacketsSent
            : snapshot.OutboundPacketsSent - previous.OutboundPacketsSent;

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
            $"InboundBytes={snapshot.TotalInboundPacketBytes}, " +
            $"OutboundPackets={snapshot.OutboundPacketsSent}, " +
            $"OutboundBytes={snapshot.TotalOutboundPacketBytes}, " +
            $"InboundPps={inboundPacketsDelta / elapsedSeconds:F1}, " +
            $"OutboundPps={outboundPacketsDelta / elapsedSeconds:F1}, " +
            $"InboundKBps={inboundBytesDelta / elapsedSeconds / 1024d:F2}, " +
            $"OutboundKBps={outboundBytesDelta / elapsedSeconds / 1024d:F2}, " +
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

        Logger.Info($"TopInboundPackets {FormatPacketTypeSummary(snapshot.TopInboundPacketTypes)}");
        Logger.Info($"TopOutboundPackets {FormatPacketTypeSummary(snapshot.TopOutboundPacketTypes)}");

        _lastSnapshot = snapshot;
        _lastSnapshotUtc = nowUtc;
    }

    private static string FormatPacketTypeSummary(IReadOnlyList<PacketTypeMetricsSnapshot> packetTypes)
    {
        if (packetTypes.Count == 0)
            return "none";

        return string.Join(
            "; ",
            packetTypes.Select(static packet =>
                $"{packet.PacketType}(count={packet.Count}, bytes={packet.Bytes})"));
    }
}
