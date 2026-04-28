using System.Collections.Concurrent;
using GameServer.World;
using GameShared.Enums;

namespace GameServer.Runtime;

public sealed class WorldRuntimeSettlementService
{
    private readonly ConcurrentDictionary<(int MapId, int InstanceId), object> _settlementLocksByInstance = new();
    private readonly WorldManager _worldManager;
    private readonly CharacterRuntimeService _characterRuntimeService;
    private readonly SkillExecutionService _skillExecutionService;
    private readonly EnemyRewardRuntimeService _enemyRewardRuntimeService;
    private readonly GroundItemRuntimeService _groundItemRuntimeService;
    private readonly WorldInterestService _interestService;

    public WorldRuntimeSettlementService(
        WorldManager worldManager,
        CharacterRuntimeService characterRuntimeService,
        SkillExecutionService skillExecutionService,
        EnemyRewardRuntimeService enemyRewardRuntimeService,
        GroundItemRuntimeService groundItemRuntimeService,
        WorldInterestService interestService)
    {
        _worldManager = worldManager;
        _characterRuntimeService = characterRuntimeService;
        _skillExecutionService = skillExecutionService;
        _enemyRewardRuntimeService = enemyRewardRuntimeService;
        _groundItemRuntimeService = groundItemRuntimeService;
        _interestService = interestService;
    }

    public void ProcessInstanceRuntime(MapInstance instance, DateTime utcNow)
    {
        ProcessInstanceRuntime(instance, utcNow, updateInstance: false);
    }

    public void UpdateAndProcessInstanceRuntime(MapInstance instance, DateTime utcNow)
    {
        ProcessInstanceRuntime(instance, utcNow, updateInstance: true);
    }

    public WorldRuntimeSettlementResult SettleBeforePlayerAction(
        PlayerSession player,
        MapInstance instance,
        DateTime utcNow)
    {
        ProcessInstanceRuntime(instance, utcNow, updateInstance: true);
        return ResolvePlayerActionReadiness(player, instance, utcNow);
    }

    private void ProcessInstanceRuntime(MapInstance instance, DateTime utcNow, bool updateInstance)
    {
        var settlementLock = _settlementLocksByInstance.GetOrAdd((instance.MapId, instance.InstanceId), _ => new object());
        lock (settlementLock)
        {
            if (updateInstance)
                instance.Update(utcNow);

            instance.QueueDueSkillExecutionEvents(utcNow);
            ApplyPendingEnemySkillCastRequests(instance, utcNow);
            instance.QueueDueSkillExecutionEvents(utcNow);
            ApplyPendingSkillCastReleases(instance, utcNow);
            instance.QueueDueSkillExecutionEvents(utcNow);
            ApplyPendingSkillImpacts(instance, utcNow);
            _enemyRewardRuntimeService.ProcessPendingEvents(instance, utcNow);
            PublishRuntimeEvents(instance);
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

            if (CharacterRuntimeStateCodes.IsDefeated(targetPlayer.RuntimeState.CaptureSnapshot().CurrentState))
                continue;

            if (!_skillExecutionService.TryGetSkillDefinition(castRequest.SkillId, out var skillDefinition))
                continue;

            var casterTarget = new CombatTargetReference(
                monster.Definition.Kind == EnemyKind.Boss ? CombatTargetKind.Boss : CombatTargetKind.Enemy,
                null,
                monster.Id,
                null);
            var target = new CombatTargetReference(
                CombatTargetKind.Character,
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

        foreach (var movementDecision in instance.DequeuePendingEnemyMovementDecisions())
            _interestService.NotifyEnemyMovementDecision(instance, movementDecision.Enemy);

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

    private static WorldRuntimeSettlementResult ResolvePlayerActionReadiness(
        PlayerSession player,
        MapInstance instance,
        DateTime utcNow)
    {
        if (!player.IsConnected)
            return WorldRuntimeSettlementResult.Disconnected;

        if (player.MapId != instance.MapId || player.InstanceId != instance.InstanceId)
            return WorldRuntimeSettlementResult.MapChanged;

        var currentState = player.RuntimeState.CaptureSnapshot().CurrentState;
        if (CharacterRuntimeStateCodes.IsDefeated(currentState))
            return WorldRuntimeSettlementResult.CharacterDefeated;

        if (currentState.CurrentState == CharacterRuntimeStateCodes.Cultivating ||
            currentState.CurrentState == CharacterRuntimeStateCodes.Practicing ||
            currentState.CurrentState == CharacterRuntimeStateCodes.Casting ||
            player.IsStunned(utcNow))
        {
            return WorldRuntimeSettlementResult.CharacterStateBlocked;
        }

        return WorldRuntimeSettlementResult.Completed;
    }
}

public enum WorldRuntimeSettlementResult
{
    Completed = 1,
    CharacterDefeated = 2,
    CharacterStateBlocked = 3,
    MapChanged = 4,
    Disconnected = 5
}
