using System.Diagnostics;
using GameServer.Diagnostics;
using GameServer.World;

namespace GameServer.Runtime;

public sealed class GameLoop
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(50);

    private readonly WorldManager _worldManager;
    private readonly ServerMetricsService _metrics;

    private readonly CancellationTokenSource _cts = new();
    private Thread? _thread;

    public GameLoop(WorldManager worldManager, ServerMetricsService metrics)
    {
        _worldManager = worldManager;
        _metrics = metrics;
    }

    public void Start()
    {
        if (_thread is not null)
            return;

        _thread = new Thread(Run)
        {
            IsBackground = true,
            Name = "GameServer.GameLoop"
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
        var stopwatch = Stopwatch.StartNew();
        var nextTick = stopwatch.Elapsed;

        while (!token.IsCancellationRequested)
        {
            var tickStart = stopwatch.Elapsed;
            var instanceCount = UpdateWorld();
            var tickDuration = stopwatch.Elapsed - tickStart;

            nextTick += TickInterval;
            var remaining = nextTick - stopwatch.Elapsed;
            var overrun = remaining <= TimeSpan.Zero;
            _metrics.RecordWorldTick(tickDuration, overrun, instanceCount);

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

    private int UpdateWorld()
    {
        var instances = _worldManager.MapManager.GetAllInstancesSnapshot();

        foreach (var instance in instances)
        {
            instance.Update();
        }

        return instances.Count;
    }
}
