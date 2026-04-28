using System.Numerics;
using GameServer.Config;
using GameServer.DTO;
using GameServer.World;

namespace GameServer.Runtime;

internal static class PlayerInteractionMovementWait
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan MinimumWait = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan MaximumWait = TimeSpan.FromSeconds(30);
    private const double ExtraWaitSeconds = 1d;

    public static async Task<InteractionMovementWaitResult> WaitUntilWithinRangeAsync(
        PlayerSession player,
        Vector2 targetPosition,
        float maxDistance,
        DateTime utcNow,
        CharacterRuntimeService runtimeService,
        GameConfigValues gameConfig,
        CancellationToken cancellationToken = default)
    {
        var sourceMapId = player.MapId;
        var sourceInstanceId = player.InstanceId;
        var sourceZoneIndex = player.ZoneIndex;
        var resolvedMaxDistance = MathF.Max(0f, maxDistance);
        var resolvedMaxDistanceSquared = resolvedMaxDistance * resolvedMaxDistance;
        var anchor = player.CapturePositionSyncAnchor();
        if (Vector2.DistanceSquared(anchor.Position, targetPosition) <= resolvedMaxDistanceSquared)
            return InteractionMovementWaitResult.Reached;

        TryApplyInteractionCatchup(
            player,
            targetPosition,
            resolvedMaxDistance,
            utcNow,
            runtimeService,
            gameConfig);

        anchor = player.CapturePositionSyncAnchor();
        if (Vector2.DistanceSquared(anchor.Position, targetPosition) <= resolvedMaxDistanceSquared)
            return InteractionMovementWaitResult.Reached;

        player.SetDesiredMovementTarget(targetPosition, utcNow);
        var timeout = ResolveWaitTimeout(player, anchor.Position, targetPosition, utcNow);
        var deadlineUtc = utcNow + timeout;

        while (DateTime.UtcNow <= deadlineUtc)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var now = DateTime.UtcNow;
            var failureReason = ResolveContinueFailure(player, sourceMapId, sourceInstanceId, sourceZoneIndex, now);
            if (failureReason.HasValue)
                return failureReason.Value;

            anchor = player.CapturePositionSyncAnchor();
            if (Vector2.DistanceSquared(anchor.Position, targetPosition) <= resolvedMaxDistanceSquared)
                return InteractionMovementWaitResult.Reached;

            player.SetDesiredMovementTarget(targetPosition, now);
            await Task.Delay(PollInterval, cancellationToken);
        }

        return InteractionMovementWaitResult.Timeout;
    }

    private static TimeSpan ResolveWaitTimeout(
        PlayerSession player,
        Vector2 fromPosition,
        Vector2 targetPosition,
        DateTime utcNow)
    {
        var speed = ResolveEffectiveMoveSpeed(player, utcNow);
        if (speed <= 0d)
            return MinimumWait;

        var distance = Math.Sqrt(Vector2.DistanceSquared(fromPosition, targetPosition));
        var seconds = distance / speed + ExtraWaitSeconds;
        return TimeSpan.FromSeconds(Math.Clamp(seconds, MinimumWait.TotalSeconds, MaximumWait.TotalSeconds));
    }

    private static void TryApplyInteractionCatchup(
        PlayerSession player,
        Vector2 targetPosition,
        float maxDistance,
        DateTime utcNow,
        CharacterRuntimeService runtimeService,
        GameConfigValues gameConfig)
    {
        if (runtimeService == null || gameConfig == null)
            return;

        var speed = ResolveEffectiveMoveSpeed(player, utcNow);
        if (speed <= 0d)
            return;

        var catchupMultiplier = Math.Max(1d, gameConfig.CharacterPositionSyncCatchupMultiplier);
        var catchupSeconds = Math.Max(0d, gameConfig.CharacterPositionSyncCatchupMaxSeconds);
        var maxCatchupDistance = (float)Math.Max(0d, speed * catchupMultiplier * catchupSeconds);
        if (maxCatchupDistance <= 0f)
            return;

        var anchor = player.CapturePositionSyncAnchor();
        var delta = targetPosition - anchor.Position;
        var distance = delta.Length();
        var missingDistanceToRange = distance - MathF.Max(0f, maxDistance);
        if (missingDistanceToRange <= 0f || distance <= 0f)
            return;

        var advanceDistance = MathF.Min(missingDistanceToRange, maxCatchupDistance);
        if (advanceDistance <= 0f)
            return;

        var nextPosition = anchor.Position + delta / distance * advanceDistance;
        runtimeService.UpdatePosition(player, player.MapId, player.ZoneIndex, nextPosition, notifySelf: false);
    }

    private static double ResolveEffectiveMoveSpeed(PlayerSession player, DateTime utcNow)
    {
        var baseStats = player.RuntimeState.CaptureSnapshot().BaseStats;
        var baseMoveSpeed = Math.Max(0d, baseStats.GetEffectiveMoveSpeed());
        return CombatStatMath.ApplyModifiers(
            baseMoveSpeed,
            player.CombatStatuses.GetStatModifierAggregate(CharacterStatType.Speed, utcNow));
    }

    private static InteractionMovementWaitResult? ResolveContinueFailure(
        PlayerSession player,
        int sourceMapId,
        int sourceInstanceId,
        int sourceZoneIndex,
        DateTime utcNow)
    {
        if (!player.IsConnected)
            return InteractionMovementWaitResult.Disconnected;

        if (player.MapId != sourceMapId ||
            player.InstanceId != sourceInstanceId ||
            player.ZoneIndex != sourceZoneIndex)
            return InteractionMovementWaitResult.MapChanged;

        var currentState = player.RuntimeState.CaptureSnapshot().CurrentState;
        if (CharacterRuntimeStateCodes.IsDefeated(currentState))
            return InteractionMovementWaitResult.CharacterDefeated;

        if (currentState.CurrentState == CharacterRuntimeStateCodes.Cultivating ||
            currentState.CurrentState == CharacterRuntimeStateCodes.Practicing ||
            currentState.CurrentState == CharacterRuntimeStateCodes.Casting)
        {
            return InteractionMovementWaitResult.CharacterStateBlocked;
        }

        return player.IsStunned(utcNow)
            ? InteractionMovementWaitResult.CharacterStateBlocked
            : null;
    }
}

public enum InteractionMovementWaitResult
{
    Reached = 1,
    Timeout = 2,
    CharacterDefeated = 3,
    CharacterStateBlocked = 4,
    MapChanged = 5,
    Disconnected = 6
}
