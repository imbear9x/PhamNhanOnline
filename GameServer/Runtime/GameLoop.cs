using System.Diagnostics;
using GameServer.Diagnostics;
using GameServer.DTO;
using GameServer.World;

namespace GameServer.Runtime;

public sealed class GameLoop
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(50);

    private readonly WorldManager _worldManager;
    private readonly EnemyRewardRuntimeService _enemyRewardRuntimeService;
    private readonly CharacterRuntimeService _characterRuntimeService;
    private readonly WorldInterestService _interestService;
    private readonly MapInstanceLifecycleService _instanceLifecycleService;
    private readonly ServerMetricsService _metrics;

    private readonly CancellationTokenSource _cts = new();
    private Thread? _thread;

    public GameLoop(
        WorldManager worldManager,
        EnemyRewardRuntimeService enemyRewardRuntimeService,
        CharacterRuntimeService characterRuntimeService,
        WorldInterestService interestService,
        MapInstanceLifecycleService instanceLifecycleService,
        ServerMetricsService metrics)
    {
        _worldManager = worldManager;
        _enemyRewardRuntimeService = enemyRewardRuntimeService;
        _characterRuntimeService = characterRuntimeService;
        _interestService = interestService;
        _instanceLifecycleService = instanceLifecycleService;
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
        var utcNow = DateTime.UtcNow;

        foreach (var instance in instances)
        {
            instance.Update(utcNow);
            ApplyPendingPlayerDamage(instance);
            _enemyRewardRuntimeService.ProcessPendingEvents(instance, utcNow);
            PublishRuntimeEvents(instance);
            _instanceLifecycleService.HandleAfterWorldTick(instance, utcNow);
        }

        return instances.Count;
    }

    private void ApplyPendingPlayerDamage(MapInstance instance)
    {
        foreach (var damageEvent in instance.DequeuePendingPlayerDamages())
        {
            if (!_worldManager.TryGetPlayer(damageEvent.TargetPlayerId, out var targetPlayer))
                continue;

            if (targetPlayer.InstanceId != instance.InstanceId || targetPlayer.MapId != instance.MapId)
                continue;

            _characterRuntimeService.ApplyDamage(targetPlayer, damageEvent.Damage);
        }
    }

    private void PublishRuntimeEvents(MapInstance instance)
    {
        foreach (var spawn in instance.DequeuePendingEnemySpawns())
            _interestService.NotifyEnemySpawned(instance, spawn.Enemy);

        foreach (var hpChanged in instance.DequeuePendingEnemyHpChanges())
            _interestService.NotifyEnemyHpChanged(instance, hpChanged);

        foreach (var despawn in instance.DequeuePendingEnemyDespawns())
            _interestService.NotifyEnemyDespawned(instance, despawn.EnemyRuntimeId);

        foreach (var spawn in instance.DequeuePendingGroundRewardSpawns())
            _interestService.NotifyGroundRewardSpawned(instance, spawn.Reward);

        foreach (var despawn in instance.DequeuePendingGroundRewardDespawns())
            _interestService.NotifyGroundRewardDespawned(instance, despawn.RewardId);
    }
}
