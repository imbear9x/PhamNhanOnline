using System.Collections.Concurrent;
using System.Threading;

namespace GameServer.Diagnostics;

public sealed class ServerMetricsService
{
    private readonly ConcurrentDictionary<int, int> _sessionQueueDepths = new();
    private readonly ConcurrentDictionary<string, PacketTrafficTotals> _inboundPacketTypes = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, PacketTrafficTotals> _outboundPacketTypes = new(StringComparer.Ordinal);

    private long _inboundPacketsEnqueued;
    private long _inboundPacketsDropped;
    private long _inboundPacketsProcessed;
    private long _inboundPacketProcessingExceptions;
    private long _inboundPacketProcessingTicks;
    private long _maxInboundPacketProcessingTicks;
    private long _maxObservedQueueDepth;
    private long _totalInboundPacketBytes;

    private long _outboundPacketsSent;
    private long _totalOutboundPacketBytes;

    private long _worldTicks;
    private long _worldTickOverruns;
    private long _worldTickTicks;
    private long _maxWorldTickTicks;
    private int _lastWorldInstanceCount;

    private long _maintenanceTicks;
    private long _maintenanceTickOverruns;
    private long _maintenanceTickTicks;
    private long _maxMaintenanceTickTicks;
    private long _maintenanceSaveRuns;
    private long _maintenanceRefreshRuns;

    public void RecordInboundPacketEnqueued(int connectionId, int queueDepth, string packetType, int packetBytes)
    {
        Interlocked.Increment(ref _inboundPacketsEnqueued);
        Interlocked.Add(ref _totalInboundPacketBytes, packetBytes);
        _sessionQueueDepths[connectionId] = queueDepth;
        UpdateMax(ref _maxObservedQueueDepth, queueDepth);
        GetPacketTotals(_inboundPacketTypes, packetType).Add(packetBytes);
    }

    public void RecordInboundPacketDropped(int connectionId, int queueDepth, string packetType, int packetBytes)
    {
        Interlocked.Increment(ref _inboundPacketsDropped);
        _sessionQueueDepths[connectionId] = queueDepth;
        UpdateMax(ref _maxObservedQueueDepth, queueDepth);
        GetPacketTotals(_inboundPacketTypes, $"{packetType}#Dropped").Add(packetBytes);
    }

    public void RecordInboundPacketProcessed(int connectionId, TimeSpan duration, int queueDepth)
    {
        Interlocked.Increment(ref _inboundPacketsProcessed);
        Interlocked.Add(ref _inboundPacketProcessingTicks, duration.Ticks);
        UpdateMax(ref _maxInboundPacketProcessingTicks, duration.Ticks);
        _sessionQueueDepths[connectionId] = queueDepth;
        UpdateMax(ref _maxObservedQueueDepth, queueDepth);
    }

    public void RecordInboundPacketProcessingException()
    {
        Interlocked.Increment(ref _inboundPacketProcessingExceptions);
    }

    public void RecordOutboundPacketSent(string packetType, int packetBytes)
    {
        Interlocked.Increment(ref _outboundPacketsSent);
        Interlocked.Add(ref _totalOutboundPacketBytes, packetBytes);
        GetPacketTotals(_outboundPacketTypes, packetType).Add(packetBytes);
    }

    public void RemoveSession(int connectionId)
    {
        _sessionQueueDepths.TryRemove(connectionId, out _);
    }

    public void RecordWorldTick(TimeSpan duration, bool overrun, int instanceCount)
    {
        Interlocked.Increment(ref _worldTicks);
        if (overrun)
            Interlocked.Increment(ref _worldTickOverruns);

        Interlocked.Add(ref _worldTickTicks, duration.Ticks);
        UpdateMax(ref _maxWorldTickTicks, duration.Ticks);
        Volatile.Write(ref _lastWorldInstanceCount, instanceCount);
    }

    public void RecordMaintenanceTick(TimeSpan duration, bool overrun, bool ranSave, bool ranRefresh)
    {
        Interlocked.Increment(ref _maintenanceTicks);
        if (overrun)
            Interlocked.Increment(ref _maintenanceTickOverruns);
        if (ranSave)
            Interlocked.Increment(ref _maintenanceSaveRuns);
        if (ranRefresh)
            Interlocked.Increment(ref _maintenanceRefreshRuns);

        Interlocked.Add(ref _maintenanceTickTicks, duration.Ticks);
        UpdateMax(ref _maxMaintenanceTickTicks, duration.Ticks);
    }

    public ServerMetricsSnapshot CaptureSnapshot(int onlinePlayers)
    {
        var activeSessions = _sessionQueueDepths.Count;
        var totalQueuedPackets = 0L;
        foreach (var depth in _sessionQueueDepths.Values)
            totalQueuedPackets += depth;

        return new ServerMetricsSnapshot(
            InboundPacketsEnqueued: Interlocked.Read(ref _inboundPacketsEnqueued),
            InboundPacketsDropped: Interlocked.Read(ref _inboundPacketsDropped),
            InboundPacketsProcessed: Interlocked.Read(ref _inboundPacketsProcessed),
            InboundPacketProcessingExceptions: Interlocked.Read(ref _inboundPacketProcessingExceptions),
            AverageInboundPacketProcessingMs: ToAverageMilliseconds(
                Interlocked.Read(ref _inboundPacketProcessingTicks),
                Interlocked.Read(ref _inboundPacketsProcessed)),
            MaxInboundPacketProcessingMs: ToMilliseconds(Interlocked.Read(ref _maxInboundPacketProcessingTicks)),
            ActiveInboundSessions: activeSessions,
            TotalQueuedInboundPackets: totalQueuedPackets,
            MaxObservedQueueDepth: Interlocked.Read(ref _maxObservedQueueDepth),
            TotalInboundPacketBytes: Interlocked.Read(ref _totalInboundPacketBytes),
            OutboundPacketsSent: Interlocked.Read(ref _outboundPacketsSent),
            TotalOutboundPacketBytes: Interlocked.Read(ref _totalOutboundPacketBytes),
            TopInboundPacketTypes: CaptureTopPacketTypes(_inboundPacketTypes),
            TopOutboundPacketTypes: CaptureTopPacketTypes(_outboundPacketTypes),
            WorldTicks: Interlocked.Read(ref _worldTicks),
            WorldTickOverruns: Interlocked.Read(ref _worldTickOverruns),
            AverageWorldTickMs: ToAverageMilliseconds(
                Interlocked.Read(ref _worldTickTicks),
                Interlocked.Read(ref _worldTicks)),
            MaxWorldTickMs: ToMilliseconds(Interlocked.Read(ref _maxWorldTickTicks)),
            LastWorldInstanceCount: Volatile.Read(ref _lastWorldInstanceCount),
            MaintenanceTicks: Interlocked.Read(ref _maintenanceTicks),
            MaintenanceTickOverruns: Interlocked.Read(ref _maintenanceTickOverruns),
            AverageMaintenanceTickMs: ToAverageMilliseconds(
                Interlocked.Read(ref _maintenanceTickTicks),
                Interlocked.Read(ref _maintenanceTicks)),
            MaxMaintenanceTickMs: ToMilliseconds(Interlocked.Read(ref _maxMaintenanceTickTicks)),
            MaintenanceSaveRuns: Interlocked.Read(ref _maintenanceSaveRuns),
            MaintenanceRefreshRuns: Interlocked.Read(ref _maintenanceRefreshRuns),
            OnlinePlayers: onlinePlayers);
    }

    private static PacketTrafficTotals GetPacketTotals(
        ConcurrentDictionary<string, PacketTrafficTotals> source,
        string packetType)
    {
        return source.GetOrAdd(packetType, _ => new PacketTrafficTotals());
    }

    private static IReadOnlyList<PacketTypeMetricsSnapshot> CaptureTopPacketTypes(
        ConcurrentDictionary<string, PacketTrafficTotals> source)
    {
        return source
            .Select(static kvp => kvp.Value.Capture(kvp.Key))
            .OrderByDescending(static snapshot => snapshot.Bytes)
            .ThenByDescending(static snapshot => snapshot.Count)
            .Take(5)
            .ToArray();
    }

    private static void UpdateMax(ref long target, long candidate)
    {
        long snapshot;
        do
        {
            snapshot = Volatile.Read(ref target);
            if (candidate <= snapshot)
                return;
        }
        while (Interlocked.CompareExchange(ref target, candidate, snapshot) != snapshot);
    }

    private static double ToAverageMilliseconds(long totalTicks, long count)
    {
        if (count <= 0)
            return 0;

        return TimeSpan.FromTicks(totalTicks / count).TotalMilliseconds;
    }

    private static double ToMilliseconds(long ticks) => TimeSpan.FromTicks(ticks).TotalMilliseconds;

    private sealed class PacketTrafficTotals
    {
        private long _count;
        private long _bytes;

        public void Add(int packetBytes)
        {
            Interlocked.Increment(ref _count);
            Interlocked.Add(ref _bytes, packetBytes);
        }

        public PacketTypeMetricsSnapshot Capture(string packetType)
        {
            return new PacketTypeMetricsSnapshot(
                packetType,
                Interlocked.Read(ref _count),
                Interlocked.Read(ref _bytes));
        }
    }
}

public sealed record ServerMetricsSnapshot(
    long InboundPacketsEnqueued,
    long InboundPacketsDropped,
    long InboundPacketsProcessed,
    long InboundPacketProcessingExceptions,
    double AverageInboundPacketProcessingMs,
    double MaxInboundPacketProcessingMs,
    int ActiveInboundSessions,
    long TotalQueuedInboundPackets,
    long MaxObservedQueueDepth,
    long TotalInboundPacketBytes,
    long OutboundPacketsSent,
    long TotalOutboundPacketBytes,
    IReadOnlyList<PacketTypeMetricsSnapshot> TopInboundPacketTypes,
    IReadOnlyList<PacketTypeMetricsSnapshot> TopOutboundPacketTypes,
    long WorldTicks,
    long WorldTickOverruns,
    double AverageWorldTickMs,
    double MaxWorldTickMs,
    int LastWorldInstanceCount,
    long MaintenanceTicks,
    long MaintenanceTickOverruns,
    double AverageMaintenanceTickMs,
    double MaxMaintenanceTickMs,
    long MaintenanceSaveRuns,
    long MaintenanceRefreshRuns,
    int OnlinePlayers);

public sealed record PacketTypeMetricsSnapshot(string PacketType, long Count, long Bytes);
