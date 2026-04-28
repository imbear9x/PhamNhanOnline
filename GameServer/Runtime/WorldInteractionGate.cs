using System.Numerics;
using GameServer.Config;
using GameServer.World;
using GameShared.Logging;
using GameShared.Messages;

namespace GameServer.Runtime;

public sealed class WorldInteractionGate
{
    private readonly CharacterCultivationService _cultivationService;
    private readonly CharacterRuntimeService _runtimeService;
    private readonly GameConfigValues _gameConfig;
    private readonly WorldRuntimeSettlementService _settlementService;
    private readonly WorldTargetResolver _targetResolver;

    public WorldInteractionGate(
        CharacterCultivationService cultivationService,
        CharacterRuntimeService runtimeService,
        GameConfigValues gameConfig,
        WorldRuntimeSettlementService settlementService,
        WorldTargetResolver targetResolver)
    {
        _cultivationService = cultivationService;
        _runtimeService = runtimeService;
        _gameConfig = gameConfig;
        _settlementService = settlementService;
        _targetResolver = targetResolver;
    }

    public WorldInteractionGateResult CheckPlayerCanStartAction(
        PlayerSession player,
        WorldInteractionActionKind actionKind,
        string actionName,
        DateTime utcNow)
    {
        var failure = ResolvePlayerActionFailure(player, actionKind, utcNow);
        if (!failure.HasValue)
            return WorldInteractionGateResult.Succeeded(default);

        if (failure.Value == PlayerActionFailure.CharacterDefeated)
        {
            LogCharacterDefeated(actionName, player);
            return WorldInteractionGateResult.CharacterDefeated();
        }

        return WorldInteractionGateResult.Failed(ToMessageCode(player, actionKind, failure.Value, utcNow));
    }

    public async Task<WorldInteractionGateResult> PrepareAsync(
        WorldInteractionGateRequest request,
        CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var stateResult = CheckPlayerCanStartAction(request.Player, request.ActionKind, request.ActionName, utcNow);
        if (!stateResult.Success)
            return stateResult;

        var resolveResult = ResolveTarget(request, utcNow);
        if (!resolveResult.Success)
            return WorldInteractionGateResult.Failed(resolveResult.FailureCode);

        var targetSnapshot = resolveResult.Snapshot;
        var rangeResult = await EnsureWithinRangeAsync(request, targetSnapshot, cancellationToken);
        if (!rangeResult.Success)
            return rangeResult;

        utcNow = DateTime.UtcNow;
        resolveResult = ResolveTarget(request, utcNow);
        if (!resolveResult.Success)
            return WorldInteractionGateResult.Failed(resolveResult.FailureCode);
        targetSnapshot = resolveResult.Snapshot;

        var settlementResult = _settlementService.SettleBeforePlayerAction(request.Player, request.Instance, utcNow);
        if (settlementResult != WorldRuntimeSettlementResult.Completed)
            return ConvertSettlementFailure(request, settlementResult);

        utcNow = DateTime.UtcNow;
        stateResult = CheckPlayerCanStartAction(request.Player, request.ActionKind, request.ActionName, utcNow);
        if (!stateResult.Success)
            return stateResult;

        resolveResult = ResolveTarget(request, utcNow);
        if (!resolveResult.Success)
            return WorldInteractionGateResult.Failed(resolveResult.FailureCode);

        targetSnapshot = resolveResult.Snapshot;
        if (!IsWithinRange(request.Player, targetSnapshot.Position, request.MaxDistance))
            return WorldInteractionGateResult.Failed(request.OutOfRangeCode);

        return WorldInteractionGateResult.Succeeded(targetSnapshot);
    }

    private WorldTargetResolveResult ResolveTarget(WorldInteractionGateRequest request, DateTime utcNow)
    {
        return _targetResolver.ResolveForPlayerInteraction(
            request.Player,
            request.Instance,
            request.Target,
            utcNow);
    }

    private async Task<WorldInteractionGateResult> EnsureWithinRangeAsync(
        WorldInteractionGateRequest request,
        WorldTargetSnapshot targetSnapshot,
        CancellationToken cancellationToken)
    {
        if (IsWithinRange(request.Player, targetSnapshot.Position, request.MaxDistance))
            return WorldInteractionGateResult.Succeeded(targetSnapshot);

        var waitResult = await PlayerInteractionMovementWait.WaitUntilWithinRangeAsync(
            request.Player,
            targetSnapshot.Position,
            MathF.Max(0f, request.MaxDistance),
            DateTime.UtcNow,
            _runtimeService,
            _gameConfig,
            cancellationToken);

        return waitResult == InteractionMovementWaitResult.Reached
            ? WorldInteractionGateResult.Succeeded(targetSnapshot)
            : ConvertWaitFailure(request, waitResult);
    }

    private static bool IsWithinRange(PlayerSession player, Vector2 targetPosition, float maxDistance)
    {
        if (maxDistance < 0f)
            return true;

        var resolvedMaxDistance = MathF.Max(0f, maxDistance);
        var currentPosition = player.CapturePositionSyncAnchor().Position;
        return Vector2.DistanceSquared(currentPosition, targetPosition) <= resolvedMaxDistance * resolvedMaxDistance;
    }

    private WorldInteractionGateResult ConvertWaitFailure(
        WorldInteractionGateRequest request,
        InteractionMovementWaitResult waitResult)
    {
        return waitResult switch
        {
            InteractionMovementWaitResult.CharacterDefeated => LogAndReturnCharacterDefeated(request, waitResult),
            InteractionMovementWaitResult.MapChanged or
                InteractionMovementWaitResult.Disconnected => WorldInteractionGateResult.Failed(MessageCode.CharacterNotInWorldInstance, waitResult),
            InteractionMovementWaitResult.CharacterStateBlocked => WorldInteractionGateResult.Failed(
                ResolveCurrentActionBlockedCode(request.Player, request.ActionKind),
                waitResult),
            _ => WorldInteractionGateResult.Failed(request.OutOfRangeCode, waitResult)
        };
    }

    private WorldInteractionGateResult ConvertSettlementFailure(
        WorldInteractionGateRequest request,
        WorldRuntimeSettlementResult settlementResult)
    {
        return settlementResult switch
        {
            WorldRuntimeSettlementResult.CharacterDefeated => LogAndReturnCharacterDefeated(request, settlementResult),
            WorldRuntimeSettlementResult.MapChanged or
                WorldRuntimeSettlementResult.Disconnected => WorldInteractionGateResult.Failed(MessageCode.CharacterNotInWorldInstance, settlementResult),
            _ => WorldInteractionGateResult.Failed(
                ResolveCurrentActionBlockedCode(request.Player, request.ActionKind),
                settlementResult)
        };
    }

    private WorldInteractionGateResult LogAndReturnCharacterDefeated(
        WorldInteractionGateRequest request,
        InteractionMovementWaitResult waitResult)
    {
        LogCharacterDefeated(request.ActionName, request.Player);
        return WorldInteractionGateResult.CharacterDefeated(waitResult);
    }

    private WorldInteractionGateResult LogAndReturnCharacterDefeated(
        WorldInteractionGateRequest request,
        WorldRuntimeSettlementResult settlementResult)
    {
        LogCharacterDefeated(request.ActionName, request.Player);
        return WorldInteractionGateResult.CharacterDefeated(settlementResult);
    }

    private MessageCode ResolveCurrentActionBlockedCode(
        PlayerSession player,
        WorldInteractionActionKind actionKind)
    {
        var failure = ResolvePlayerActionFailure(player, actionKind, DateTime.UtcNow);
        return failure.HasValue
            ? ToMessageCode(player, actionKind, failure.Value, DateTime.UtcNow)
            : MessageCode.CharacterActionsRestricted;
    }

    private PlayerActionFailure? ResolvePlayerActionFailure(
        PlayerSession player,
        WorldInteractionActionKind actionKind,
        DateTime utcNow)
    {
        if (!player.IsConnected)
            return PlayerActionFailure.NotInWorldInstance;

        var currentState = player.RuntimeState.CaptureSnapshot().CurrentState;
        if (CharacterRuntimeStateCodes.IsDefeated(currentState))
            return PlayerActionFailure.CharacterDefeated;

        if (player.AreCharacterActionsRestricted)
            return PlayerActionFailure.ActionsRestricted;

        if (_cultivationService.IsCultivating(player))
            return PlayerActionFailure.Cultivating;

        if (currentState.CurrentState == CharacterRuntimeStateCodes.Practicing)
            return PlayerActionFailure.Practicing;

        if (currentState.CurrentState == CharacterRuntimeStateCodes.Casting || player.IsCastingSkill)
            return PlayerActionFailure.Casting;

        if (player.IsStunned(utcNow))
            return PlayerActionFailure.Stunned;

        return null;
    }

    private static MessageCode ToMessageCode(
        PlayerSession player,
        WorldInteractionActionKind actionKind,
        PlayerActionFailure failure,
        DateTime utcNow)
    {
        return failure switch
        {
            PlayerActionFailure.NotInWorldInstance => MessageCode.CharacterNotInWorldInstance,
            PlayerActionFailure.CharacterDefeated or
                PlayerActionFailure.ActionsRestricted => MessageCode.CharacterActionsRestricted,
            PlayerActionFailure.Cultivating => actionKind switch
            {
                WorldInteractionActionKind.CombatSkill => MessageCode.CharacterCannotMoveWhileCultivating,
                WorldInteractionActionKind.PortalTravel => MessageCode.PracticeAlreadyActive,
                _ => MessageCode.CharacterActionsRestricted
            },
            PlayerActionFailure.Practicing => actionKind switch
            {
                WorldInteractionActionKind.GroundRewardPickup => MessageCode.CharacterActionsRestricted,
                _ => MessageCode.PracticeAlreadyActive
            },
            PlayerActionFailure.Casting => actionKind switch
            {
                WorldInteractionActionKind.CombatSkill => MessageCode.SkillAlreadyCasting,
                WorldInteractionActionKind.PortalTravel => MessageCode.CharacterCannotActWhileCasting,
                _ => MessageCode.CharacterActionsRestricted
            },
            PlayerActionFailure.Stunned => MessageCode.CharacterCannotActWhileStunned,
            _ => player.IsStunned(utcNow)
                ? MessageCode.CharacterCannotActWhileStunned
                : MessageCode.CharacterActionsRestricted
        };
    }

    private static void LogCharacterDefeated(string actionName, PlayerSession player)
    {
        Logger.Info(
            $"World interaction canceled because character is defeated. " +
            $"Action={actionName}, CharacterId={player.CharacterData.CharacterId}, " +
            $"Map={player.MapId}, Instance={player.InstanceId}, Zone={player.ZoneIndex}.");
    }

    private enum PlayerActionFailure
    {
        NotInWorldInstance = 1,
        CharacterDefeated = 2,
        ActionsRestricted = 3,
        Cultivating = 4,
        Practicing = 5,
        Casting = 6,
        Stunned = 7
    }
}
