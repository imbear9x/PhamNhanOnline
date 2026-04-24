using System;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    [DisallowMultipleComponent]
    public sealed class WorldEntityMovementView : MonoBehaviour
    {
        private const float DefaultPacketIntervalSeconds = 0.10f;
        private const float MinPacketIntervalSeconds = 0.05f;
        private const float MaxPacketIntervalSeconds = 0.60f;
        private const float PacketIntervalBlendFactor = 0.35f;
        private const float MinInterpolationDurationSeconds = 0.04f;
        private const float MaxInterpolationDurationSeconds = 0.50f;
        private const float PositionEpsilon = 0.001f;

        private Vector3 targetPosition;
        private Vector3 timedMoveStartPosition;
        private Vector3 timedMoveTargetPosition;
        private float currentMoveSpeed;
        private float lastSnapshotReceivedAt = -1f;
        private float estimatedPacketInterval = DefaultPacketIntervalSeconds;
        private float timedMoveDurationSeconds;
        private float timedMoveElapsedSeconds;
        private int lastDecisionVersion = -1;
        private bool hasTargetPosition;
        private bool hasTimedMove;

        public event Action<Vector3, Vector3> PositionAdvanced;

        public Vector3 TargetPosition { get { return targetPosition; } }
        public bool HasPosition { get { return hasTargetPosition; } }
        public bool IsMoving { get; private set; }

        public bool IsCurrentMoveDecision(int decisionVersion)
        {
            return hasTargetPosition && decisionVersion == lastDecisionVersion;
        }

        public void SnapTo(Vector3 worldPosition)
        {
            targetPosition = worldPosition;
            hasTargetPosition = true;
            hasTimedMove = false;
            currentMoveSpeed = 0f;
            lastSnapshotReceivedAt = Time.unscaledTime;
            MoveTransform(worldPosition);
            IsMoving = false;
        }

        public bool FollowSnapshot(
            Vector3 worldPosition,
            bool snap,
            float smoothing,
            float snapDistance,
            float arrivalThreshold)
        {
            targetPosition = worldPosition;
            hasTargetPosition = true;
            hasTimedMove = false;

            var threshold = Mathf.Max(PositionEpsilon, arrivalThreshold);
            var shouldSnap = snap;
            if (!shouldSnap)
            {
                var teleportDistance = Mathf.Max(threshold, snapDistance);
                shouldSnap = Vector3.Distance(transform.position, targetPosition) >= teleportDistance;
            }

            if (shouldSnap)
            {
                currentMoveSpeed = 0f;
                lastSnapshotReceivedAt = Time.unscaledTime;
                MoveTransform(targetPosition);
                IsMoving = false;
                return true;
            }

            UpdateSnapshotMoveSpeed(smoothing);
            IsMoving = currentMoveSpeed > 0f && Vector3.Distance(transform.position, targetPosition) > threshold;
            return false;
        }

        public void ApplyMoveDecision(
            int decisionVersion,
            Vector3 authoritativeWorldPosition,
            Vector3 targetWorldPosition,
            float durationSeconds,
            float snapDistance,
            bool forceDecisionRefresh)
        {
            if (!forceDecisionRefresh && decisionVersion == lastDecisionVersion)
                return;

            if (!hasTargetPosition)
                MoveTransform(authoritativeWorldPosition);

            hasTargetPosition = true;
            var distanceToAuthoritative = Vector3.Distance(transform.position, authoritativeWorldPosition);
            if (distanceToAuthoritative >= Mathf.Max(PositionEpsilon, snapDistance))
                MoveTransform(authoritativeWorldPosition);

            lastDecisionVersion = decisionVersion;
            currentMoveSpeed = 0f;
            hasTimedMove = false;
            timedMoveElapsedSeconds = 0f;
            timedMoveDurationSeconds = 0f;
            targetPosition = targetWorldPosition;

            if (durationSeconds <= PositionEpsilon)
            {
                MoveTransform(targetWorldPosition);
                IsMoving = false;
                return;
            }

            timedMoveStartPosition = transform.position;
            timedMoveTargetPosition = targetWorldPosition;
            timedMoveDurationSeconds = durationSeconds;
            hasTimedMove = true;
            IsMoving = true;
        }

        public void StopAt(Vector3 authoritativeWorldPosition)
        {
            targetPosition = authoritativeWorldPosition;
            hasTargetPosition = true;
            hasTimedMove = false;
            currentMoveSpeed = 0f;
            MoveTransform(authoritativeWorldPosition);
            IsMoving = false;
        }

        private void Update()
        {
            if (!hasTargetPosition)
                return;

            if (hasTimedMove)
            {
                AdvanceTimedMove();
                return;
            }

            if (currentMoveSpeed <= 0f)
            {
                IsMoving = false;
                return;
            }

            AdvanceSnapshotMove();
        }

        private void AdvanceTimedMove()
        {
            var previousPosition = transform.position;
            timedMoveElapsedSeconds += Time.deltaTime;
            var progress = timedMoveDurationSeconds <= 0f
                ? 1f
                : Mathf.Clamp01(timedMoveElapsedSeconds / timedMoveDurationSeconds);
            var nextPosition = Vector3.Lerp(timedMoveStartPosition, timedMoveTargetPosition, progress);
            MoveTransform(nextPosition);

            IsMoving = progress < 1f;
            if (!IsMoving)
                hasTimedMove = false;

            NotifyAdvanced(previousPosition, transform.position);
        }

        private void AdvanceSnapshotMove()
        {
            var previousPosition = transform.position;
            var nextPosition = Vector3.MoveTowards(
                previousPosition,
                targetPosition,
                currentMoveSpeed * Time.deltaTime);

            if ((targetPosition - nextPosition).sqrMagnitude <= PositionEpsilon * PositionEpsilon)
            {
                nextPosition = targetPosition;
                currentMoveSpeed = 0f;
            }

            MoveTransform(nextPosition);
            IsMoving = currentMoveSpeed > 0f && Vector3.Distance(transform.position, targetPosition) > PositionEpsilon;
            NotifyAdvanced(previousPosition, transform.position);
        }

        private void UpdateSnapshotMoveSpeed(float smoothing)
        {
            var now = Time.unscaledTime;
            if (lastSnapshotReceivedAt > 0f)
            {
                var measuredInterval = Mathf.Clamp(
                    now - lastSnapshotReceivedAt,
                    MinPacketIntervalSeconds,
                    MaxPacketIntervalSeconds);
                estimatedPacketInterval = Mathf.Lerp(
                    estimatedPacketInterval,
                    measuredInterval,
                    PacketIntervalBlendFactor);
            }
            else
            {
                estimatedPacketInterval = DefaultPacketIntervalSeconds;
            }

            lastSnapshotReceivedAt = now;

            var speedScale = Mathf.Clamp(14f / Mathf.Max(0.01f, smoothing), 0.35f, 2f);
            var interpolationDuration = Mathf.Clamp(
                estimatedPacketInterval * speedScale,
                MinInterpolationDurationSeconds,
                MaxInterpolationDurationSeconds);
            var distanceToTarget = Vector3.Distance(transform.position, targetPosition);
            currentMoveSpeed = interpolationDuration > Mathf.Epsilon
                ? distanceToTarget / interpolationDuration
                : 0f;
        }

        private void MoveTransform(Vector3 position)
        {
            transform.position = position;
        }

        private void NotifyAdvanced(Vector3 previousPosition, Vector3 nextPosition)
        {
            if ((nextPosition - previousPosition).sqrMagnitude <= PositionEpsilon * PositionEpsilon)
                return;

            PositionAdvanced?.Invoke(previousPosition, nextPosition);
        }
    }
}
