using System.Diagnostics;
using GameServer.Diagnostics;
using GameServer.DTO;
using GameServer.World;
using GameShared.Enums;

namespace GameServer.Runtime;

public sealed class GameLoop
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(50);

    private readonly WorldManager _worldManager;
    private readonly EnemyRewardRuntimeService _enemyRewardRuntimeService;
    private readonly CharacterRuntimeService _characterRuntimeService;
    private readonly SkillExecutionService _skillExecutionService;
    private readonly GroundItemRuntimeService _groundItemRuntimeService;
    private readonly WorldInterestService _interestService;
    private readonly MapInstanceLifecycleService _instanceLifecycleService;
    private readonly ServerMetricsService _metrics;

    private readonly CancellationTokenSource _cts = new();
    private Thread? _thread;

    public GameLoop(
        WorldManager worldManager,
        EnemyRewardRuntimeService enemyRewardRuntimeService,
        CharacterRuntimeService characterRuntimeService,
        SkillExecutionService skillExecutionService,
        GroundItemRuntimeService groundItemRuntimeService,
        WorldInterestService interestService,
        MapInstanceLifecycleService instanceLifecycleService,
        ServerMetricsService metrics)
    {
        _worldManager = worldManager;
        _enemyRewardRuntimeService = enemyRewardRuntimeService;
        _characterRuntimeService = characterRuntimeService;
        _skillExecutionService = skillExecutionService;
        _groundItemRuntimeService = groundItemRuntimeService;
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
            ApplyPendingEnemySkillCastRequests(instance, utcNow);
            ApplyPendingSkillCastReleases(instance, utcNow);
            ApplyPendingSkillImpacts(instance, utcNow);
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

    private void ApplyPendingEnemySkillCastRequests(MapInstance instance, DateTime utcNow)
    {
        foreach (var castRequest in instance.DequeuePendingEnemySkillCastRequests())
        {
            if (!instance.TryGetMonster(castRequest.EnemyRuntimeId, out var monster) || !monster.IsAlive)
                continue;

            if (!_worldManager.TryGetPlayer(castRequest.TargetPlayerId, out var targetPlayer))
                continue;

            if (targetPlayer.InstanceId != instance.InstanceId || targetPlayer.MapId != instance.MapId)
                continue;

            if (!_skillExecutionService.TryGetSkillDefinition(castRequest.SkillId, out var skillDefinition))
                continue;

            var casterTarget = new CombatTargetReference(
                monster.Definition.Kind == EnemyKind.Boss ? GameShared.Enums.CombatTargetKind.Boss : GameShared.Enums.CombatTargetKind.Enemy,
                null,
                monster.Id,
                null);
            var target = new CombatTargetReference(
                GameShared.Enums.CombatTargetKind.Character,
                targetPlayer.CharacterData.CharacterId,
                null,
                null);
            var execution = instance.EnqueueSkillExecution(
                casterTarget,
                null,
                null,
                0,
                skillDefinition.Id,
                skillDefinition.Code,
                skillDefinition.GroupCode,
                castRequest.SkillSlotIndex,
                skillDefinition.TargetType,
                _skillExecutionService.CaptureCasterStats(monster, utcNow),
                target,
                skillDefinition.CastTimeMs,
                skillDefinition.TravelTimeMs,
                utcNow);
            _interestService.NotifySkillCastStarted(instance, execution);
        }
    }

    private void ApplyPendingSkillCastReleases(MapInstance instance, DateTime utcNow)
    {
        foreach (var releaseEvent in instance.DequeuePendingSkillCastReleases())
        {
            var execution = releaseEvent.Execution;
            _skillExecutionService.ResolveCastRelease(instance, execution, utcNow);
            if (!execution.CasterPlayerId.HasValue ||
                !_worldManager.TryGetPlayer(execution.CasterPlayerId.Value, out var caster) ||
                caster.MapId != instance.MapId ||
                caster.InstanceId != instance.InstanceId)
            {
                continue;
            }

            caster.CompleteSkillCast(execution.ExecutionId);
            var currentState = caster.RuntimeState.CaptureSnapshot().CurrentState;
            if (currentState.CurrentState != CharacterRuntimeStateCodes.Casting)
                continue;

            _characterRuntimeService.ApplyCurrentStateMutation(
                caster,
                state => state with { CurrentState = CharacterRuntimeStateCodes.Idle },
                persist: false);
        }
    }

    private void ApplyPendingSkillImpacts(MapInstance instance, DateTime utcNow)
    {
        foreach (var impactEvent in instance.DequeuePendingSkillImpactDues())
        {
            var resolvedImpact = _skillExecutionService.ResolveImpact(instance, impactEvent.Execution, utcNow);
            instance.EnqueueSkillImpactResolved(resolvedImpact);
        }
    }

    private void PublishRuntimeEvents(MapInstance instance)
    {
        var groundDespawns = instance.DequeuePendingGroundRewardDespawns();
        _groundItemRuntimeService.ProcessDespawnedRewards(groundDespawns);

        foreach (var spawn in instance.DequeuePendingEnemySpawns())
            _interestService.NotifyEnemySpawned(instance, spawn.Enemy);

        foreach (var hpChanged in instance.DequeuePendingEnemyHpChanges())
            _interestService.NotifyEnemyHpChanged(instance, hpChanged);

        foreach (var impact in instance.DequeuePendingSkillImpactResolutions())
            _interestService.NotifySkillImpactResolved(instance, impact);

        foreach (var despawn in instance.DequeuePendingEnemyDespawns())
            _interestService.NotifyEnemyDespawned(instance, despawn.EnemyRuntimeId);

        foreach (var spawn in instance.DequeuePendingGroundRewardSpawns())
            _interestService.NotifyGroundRewardSpawned(instance, spawn.Reward);

        foreach (var despawn in groundDespawns)
            _interestService.NotifyGroundRewardDespawned(instance, despawn.RewardId);
    }
}
