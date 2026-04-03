using GameShared.Models;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Features.Combat.Presentation;
using PhamNhanOnline.Client.Features.Character.Presentation;
using PhamNhanOnline.Client.Features.Targeting.Application;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    [DisallowMultipleComponent]
    public sealed class RemoteCharacterPresenter : MonoBehaviour
    {
        private const string MoveSpeedParameterName = "MoveSpeed";
        private const float DefaultPacketIntervalSeconds = 0.10f;
        private const float MinPacketIntervalSeconds = 0.05f;
        private const float MaxPacketIntervalSeconds = 0.60f;
        private const float PacketIntervalBlendFactor = 0.35f;
        private const float MinInterpolationDurationSeconds = 0.04f;
        private const float MaxInterpolationDurationSeconds = 0.50f;

        [SerializeField] private PlayerView playerView;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Animator animator;
        [SerializeField] private bool visualFacesLeftByDefault = true;
        [SerializeField] private float moveSmoothing = 14f;
        [SerializeField] private float animationMoveThreshold = 0.02f;
        [SerializeField] private float animationHoldDuration = 0.12f;
        private Vector3 targetPosition;
        private float currentMoveSpeed;
        private float lastSnapshotReceivedAt = -1f;
        private float estimatedPacketInterval = DefaultPacketIntervalSeconds;
        private bool hasTargetPosition;
        private float visualDefaultScaleX = 1f;
        private bool facingLeft = true;
        private bool hasMoveSpeedParameter;
        private int moveSpeedParameterHash;
        private bool warnedPositionMapping;
        private float moveAnimationTimer;
        private WorldTargetable targetable;
        private CharacterSkillPresenter skillPresenter;
        private bool warnedMissingSkillPresenter;
        private float teleportSnapDistance = 3f;

        public void Initialize(float smoothing, float snapDistance)
        {
            moveSmoothing = Mathf.Max(0.01f, smoothing);
            teleportSnapDistance = Mathf.Max(0.1f, snapDistance);
            AutoWireReferences();
            DisableLocalOnlyComponents();
            CacheAnimatorParameters();

            if (visualRoot != null)
                visualDefaultScaleX = visualRoot.localScale.x;

            facingLeft = visualFacesLeftByDefault;
            ApplyFacing();
        }

        public void ApplySnapshot(ObservedCharacterModel observedCharacter, WorldMapPresenter worldMapPresenter, bool snap)
        {
            AutoWireReferences();
            ConfigureTargetable(observedCharacter);

            Vector2 worldPosition;
            var serverPosition = new Vector2(
                observedCharacter.CurrentState.CurrentPosX,
                observedCharacter.CurrentState.CurrentPosY);

            if (worldMapPresenter != null && worldMapPresenter.TryMapServerPositionToWorld(serverPosition, out worldPosition))
            {
                SetTargetPosition(worldPosition, snap);
                warnedPositionMapping = false;
            }
            else
            {
                if (!warnedPositionMapping)
                {
                    ClientLog.Warn($"RemoteCharacterPresenter on {name} could not map server position into Unity world space. Falling back to raw coordinates.");
                    warnedPositionMapping = true;
                }

                SetTargetPosition(serverPosition, snap);
            }
        }

        private void Awake()
        {
            AutoWireReferences();
            DisableLocalOnlyComponents();
            CacheAnimatorParameters();

            if (visualRoot != null)
                visualDefaultScaleX = visualRoot.localScale.x;

            facingLeft = visualFacesLeftByDefault;
            ApplyFacing();
        }

        private void Update()
        {
            if (!hasTargetPosition)
                return;

            var currentPosition = transform.position;
            var nextPosition = Vector3.MoveTowards(
                currentPosition,
                targetPosition,
                currentMoveSpeed * Time.deltaTime);

            if ((targetPosition - nextPosition).sqrMagnitude <= animationMoveThreshold * animationMoveThreshold)
            {
                nextPosition = targetPosition;
                currentMoveSpeed = 0f;
            }

            transform.position = nextPosition;
            UpdateFacingAndAnimation(currentPosition, nextPosition);
        }

        private void SetTargetPosition(Vector2 worldPosition, bool snap)
        {
            var newTargetPosition = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
            var movementDelta = hasTargetPosition
                ? newTargetPosition - targetPosition
                : newTargetPosition - transform.position;

            if (movementDelta.x > animationMoveThreshold)
                facingLeft = false;
            else if (movementDelta.x < -animationMoveThreshold)
                facingLeft = true;

            targetPosition = newTargetPosition;
            hasTargetPosition = true;

            if (movementDelta.sqrMagnitude > animationMoveThreshold * animationMoveThreshold)
                moveAnimationTimer = animationHoldDuration;

            var shouldSnap = snap;
            if (!shouldSnap)
            {
                var teleportDistance = Mathf.Max(animationMoveThreshold, teleportSnapDistance);
                var currentToTargetDistance = Vector3.Distance(transform.position, targetPosition);
                shouldSnap = currentToTargetDistance >= teleportDistance;
            }

            if (!shouldSnap)
            {
                UpdateMoveSpeedForCurrentTarget();
                return;
            }

            transform.position = targetPosition;
            currentMoveSpeed = 0f;
            lastSnapshotReceivedAt = Time.unscaledTime;
            moveAnimationTimer = 0f;
            SyncMoveAnimation(false);
        }

        private void UpdateMoveSpeedForCurrentTarget()
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

            var speedScale = Mathf.Clamp(14f / Mathf.Max(0.01f, moveSmoothing), 0.35f, 2f);
            var interpolationDuration = Mathf.Clamp(
                estimatedPacketInterval * speedScale,
                MinInterpolationDurationSeconds,
                MaxInterpolationDurationSeconds);
            var distanceToTarget = Vector3.Distance(transform.position, targetPosition);
            currentMoveSpeed = interpolationDuration > Mathf.Epsilon
                ? distanceToTarget / interpolationDuration
                : 0f;
        }

        private void UpdateFacingAndAnimation(Vector3 previousPosition, Vector3 nextPosition)
        {
            var deltaX = nextPosition.x - previousPosition.x;
            if (deltaX > animationMoveThreshold)
                facingLeft = false;
            else if (deltaX < -animationMoveThreshold)
                facingLeft = true;

            ApplyFacing();

            var isMovingThisFrame = Mathf.Abs(deltaX) > animationMoveThreshold;
            var remainingDistance = Vector3.Distance(nextPosition, targetPosition);
            var isChasingTarget = remainingDistance > animationMoveThreshold;

            if (isMovingThisFrame || isChasingTarget)
                moveAnimationTimer = animationHoldDuration;
            else if (moveAnimationTimer > 0f)
                moveAnimationTimer = Mathf.Max(0f, moveAnimationTimer - Time.deltaTime);

            SyncMoveAnimation(isMovingThisFrame || isChasingTarget || moveAnimationTimer > 0f);
        }

        private void SyncMoveAnimation(bool isMoving)
        {
            if (animator == null || !hasMoveSpeedParameter)
                return;

            animator.SetFloat(moveSpeedParameterHash, isMoving ? 1f : 0f);
        }

        private void ApplyFacing()
        {
            if (visualRoot == null)
                return;

            if (Mathf.Approximately(visualDefaultScaleX, 0f))
                visualDefaultScaleX = 1f;

            var targetScaleX = facingLeft == visualFacesLeftByDefault
                ? visualDefaultScaleX
                : -visualDefaultScaleX;

            var scale = visualRoot.localScale;
            scale.x = targetScaleX;
            visualRoot.localScale = scale;
        }

        private void DisableLocalOnlyComponents()
        {
            var localActionController = GetComponent<LocalCharacterActionController>();
            if (localActionController != null)
                localActionController.enabled = false;

            var inputSources = GetComponents<CharacterActionInputSource>();
            for (var i = 0; i < inputSources.Length; i++)
                inputSources[i].enabled = false;

            var body = playerView != null ? playerView.Body : GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.velocity = Vector2.zero;
                body.angularVelocity = 0f;
                // Keep the rigidbody in the 2D physics world so trigger-based target
                // colliders can still be hit by OverlapPoint queries.
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;
                body.simulated = true;
            }

            var bodyCollider = playerView != null ? playerView.BodyCollider : GetComponent<Collider2D>();
            if (bodyCollider != null)
                bodyCollider.enabled = false;
        }

        private void CacheAnimatorParameters()
        {
            if (animator == null)
            {
                hasMoveSpeedParameter = false;
                moveSpeedParameterHash = 0;
                return;
            }

            moveSpeedParameterHash = Animator.StringToHash(MoveSpeedParameterName);
            hasMoveSpeedParameter = false;
            var parameters = animator.parameters;
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (parameter.type == AnimatorControllerParameterType.Float && parameter.name == MoveSpeedParameterName)
                {
                    hasMoveSpeedParameter = true;
                    break;
                }
            }
        }

        private void AutoWireReferences()
        {
            if (targetable == null)
                targetable = GetComponent<WorldTargetable>();

            if (skillPresenter == null)
                skillPresenter = GetComponent<CharacterSkillPresenter>();

            if (playerView == null)
                playerView = GetComponent<PlayerView>();

            if (playerView != null)
            {
                if (visualRoot == null)
                    visualRoot = playerView.VisualRoot;
                if (animator == null)
                    animator = playerView.Animator;
            }

            if (visualRoot == null)
            {
                var child = transform.Find("VisualRoot");
                visualRoot = child != null ? child : transform;
            }

            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);

        }

        private void ConfigureTargetable(ObservedCharacterModel observedCharacter)
        {
            if (targetable == null)
                targetable = gameObject.AddComponent<WorldTargetable>();

            var handle = WorldTargetHandle.CreateObservedCharacter(observedCharacter.Character.CharacterId);
            targetable.Configure(handle);

            if (skillPresenter == null)
            {
                if (!warnedMissingSkillPresenter)
                {
                    ClientLog.Error(
                        $"RemoteCharacterPresenter requires CharacterSkillPresenter on prefab '{gameObject.name}'. Add the component to the remote player prefab instead of relying on runtime AddComponent.");
                    warnedMissingSkillPresenter = true;
                }

                return;
            }

            skillPresenter.ConfigureCharacter(observedCharacter.Character.CharacterId);
            skillPresenter.ConfigureTargetHandle(handle);
        }
    }
}
