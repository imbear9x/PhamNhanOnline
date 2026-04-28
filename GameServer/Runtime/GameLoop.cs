using System.Diagnostics;
using System.Numerics;
using GameServer.Config;
using GameServer.Diagnostics;
using GameServer.DTO;
using GameServer.World;
using GameShared.Enums;
using GameShared.Logging;

namespace GameServer.Runtime;

public sealed class GameLoop
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan MovementClampLogMinInterval = TimeSpan.FromMilliseconds(2000);
    private const float DesiredMovementArriveDistance = 0.01f;

    private readonly WorldManager _worldManager;
    private readonly CharacterRuntimeService _characterRuntimeService;
    private readonly WorldRuntimeSettlementService _settlementService;
    private readonly MapInstanceLifecycleService _instanceLifecycleService;
    private readonly ServerMetricsService _metrics;
    private readonly GameConfigValues _gameConfig;
    private readonly MapCatalog _mapCatalog;

    private readonly CancellationTokenSource _cts = new();
    private Thread? _thread;

    public GameLoop(
        WorldManager worldManager,
        CharacterRuntimeService characterRuntimeService,
        WorldRuntimeSettlementService settlementService,
        MapInstanceLifecycleService instanceLifecycleService,
        ServerMetricsService metrics,
        GameConfigValues gameConfig,
        MapCatalog mapCatalog)
    {
        _worldManager = worldManager;
        _characterRuntimeService = characterRuntimeService;
        _settlementService = settlementService;
        _instanceLifecycleService = instanceLifecycleService;
        _metrics = metrics;
        _gameConfig = gameConfig;
        _mapCatalog = mapCatalog;
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

        ApplyDesiredPlayerMovement(utcNow);

        foreach (var instance in instances)
        {
            _settlementService.UpdateAndProcessInstanceRuntime(instance, utcNow);
            _instanceLifecycleService.HandleAfterWorldTick(instance, utcNow);
        }

        return instances.Count;
    }

    private void ApplyDesiredPlayerMovement(DateTime utcNow)
    {
        foreach (var player in _worldManager.GetOnlinePlayersSnapshot())
        {
            if (!player.IsConnected)
                continue;

            if (!player.TryGetDesiredMovementTarget(out var desiredPosition))
                continue;

            var runtimeSnapshot = player.RuntimeState.CaptureSnapshot();
            if (CharacterRuntimeStateCodes.IsDefeated(runtimeSnapshot.CurrentState))
            {
                player.ClearDesiredMovementTarget();
                continue;
            }

            if (runtimeSnapshot.CurrentState.CurrentState == CharacterRuntimeStateCodes.Cultivating ||
                runtimeSnapshot.CurrentState.CurrentState == CharacterRuntimeStateCodes.Practicing ||
                runtimeSnapshot.CurrentState.CurrentState == CharacterRuntimeStateCodes.Casting ||
                player.IsStunned(utcNow))
            {
                continue;
            }

            if (!_mapCatalog.TryGet(player.MapId, out var mapDefinition))
            {
                player.ClearDesiredMovementTarget();
                continue;
            }

            desiredPosition = mapDefinition.ClampPosition(desiredPosition);
            var anchor = player.CapturePositionSyncAnchor();
            var delta = desiredPosition - anchor.Position;
            var distance = delta.Length();
            if (distance <= DesiredMovementArriveDistance)
            {
                player.ClearDesiredMovementTarget();
                continue;
            }

            var effectiveMoveSpeed = ResolveEffectiveMoveSpeed(player, runtimeSnapshot.BaseStats, utcNow);
            if (effectiveMoveSpeed <= 0d)
                continue;

            var elapsedSeconds = Math.Max(0d, (utcNow - anchor.LastSyncUtc).TotalSeconds);
            var cappedElapsedSeconds = Math.Min(
                elapsedSeconds,
                Math.Max(0d, _gameConfig.CharacterPositionSyncMaxElapsedSeconds));
            var maxStep = (float)Math.Max(0d, effectiveMoveSpeed * cappedElapsedSeconds);
            if (maxStep <= 0f)
                continue;

            var nextPosition = distance <= maxStep
                ? desiredPosition
                : anchor.Position + delta / distance * maxStep;

            _characterRuntimeService.UpdatePosition(player, player.MapId, player.ZoneIndex, nextPosition, notifySelf: false);

            if (distance <= maxStep)
                player.ClearDesiredMovementTarget();

            LogSuspiciousMovementIfNeeded(
                player,
                anchor.Position,
                desiredPosition,
                distance,
                effectiveMoveSpeed,
                cappedElapsedSeconds,
                utcNow);
        }
    }

    private double ResolveEffectiveMoveSpeed(PlayerSession player, CharacterBaseStatsDto baseStats, DateTime utcNow)
    {
        var baseMoveSpeed = Math.Max(0d, baseStats.GetEffectiveMoveSpeed());
        return CombatStatMath.ApplyModifiers(
            baseMoveSpeed,
            player.CombatStatuses.GetStatModifierAggregate(CharacterStatType.Speed, utcNow));
    }

    private void LogSuspiciousMovementIfNeeded(
        PlayerSession player,
        Vector2 fromPosition,
        Vector2 desiredPosition,
        float desiredDistance,
        double effectiveMoveSpeed,
        double elapsedSeconds,
        DateTime utcNow)
    {
        var suspiciousDistance =
            effectiveMoveSpeed *
            Math.Max(1d, _gameConfig.CharacterPositionSyncMaxSpeedMultiplier) *
            elapsedSeconds +
            Math.Max(0d, _gameConfig.CharacterPositionSyncGraceServerUnits);
        if (desiredDistance <= suspiciousDistance)
            return;

        if (!player.TryMarkMovementClampLogged(utcNow, MovementClampLogMinInterval))
            return;

        Logger.Info(
            $"[PositionSync] clamp movement player={player.CharacterData.Name} " +
            $"characterId={player.CharacterData.CharacterId} map={player.MapId} zone={player.ZoneIndex} " +
            $"from=({fromPosition.X},{fromPosition.Y}) desired=({desiredPosition.X},{desiredPosition.Y}) " +
            $"distance={desiredDistance:0.###} allowedLogDistance={suspiciousDistance:0.###} " +
            $"speed={effectiveMoveSpeed:0.###} elapsed={elapsedSeconds:0.###}.");
    }

}
