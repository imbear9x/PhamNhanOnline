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

        [SerializeField] private PlayerView playerView;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Animator animator;
        [SerializeField] private WorldTargetable targetable;
        [SerializeField] private CharacterSkillPresenter skillPresenter;
        [SerializeField] private WorldEntityMovementView movementView;
        [SerializeField] private LocalCharacterActionController localActionController;
        [SerializeField] private CharacterActionInputSource[] localInputSourcesToDisable;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Collider2D bodyCollider;
        [SerializeField] private bool visualFacesLeftByDefault = true;
        [SerializeField] private float moveSmoothing = 14f;
        [SerializeField] private float animationMoveThreshold = 0.02f;
        [SerializeField] private float animationHoldDuration = 0.12f;
        private Vector3 lastObservedPosition;
        private float visualDefaultScaleX = 1f;
        private bool facingLeft = true;
        private bool hasMoveSpeedParameter;
        private int moveSpeedParameterHash;
        private bool warnedPositionMapping;
        private float moveAnimationTimer;
        private bool hasObservedPosition;
        private bool warnedMissingSkillPresenter;
        private bool warnedMissingMovementView;
        private bool warnedMissingTargetable;
        private bool warnedMissingVisualRoot;
        private bool warnedMissingAnimator;
        private float teleportSnapDistance = 3f;

        public void Initialize(float smoothing, float snapDistance)
        {
            moveSmoothing = Mathf.Max(0.01f, smoothing);
            teleportSnapDistance = Mathf.Max(0.1f, snapDistance);
            ValidateRequiredReferences();
            DisableLocalOnlyComponents();
            CacheAnimatorParameters();

            var resolvedVisualRoot = ResolveVisualRoot();
            if (resolvedVisualRoot != null)
                visualDefaultScaleX = resolvedVisualRoot.localScale.x;

            facingLeft = visualFacesLeftByDefault;
            ApplyFacing();
        }

        public void ApplySnapshot(ObservedCharacterModel observedCharacter, WorldMapPresenter worldMapPresenter, bool snap)
        {
            if (!ValidateRequiredReferences())
                return;

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
            ValidateRequiredReferences();
            DisableLocalOnlyComponents();
            CacheAnimatorParameters();

            var resolvedVisualRoot = ResolveVisualRoot();
            if (resolvedVisualRoot != null)
                visualDefaultScaleX = resolvedVisualRoot.localScale.x;

            facingLeft = visualFacesLeftByDefault;
            ApplyFacing();
        }

        private void Update()
        {
            if (movementView == null || !movementView.HasPosition)
                return;

            var currentPosition = transform.position;
            if (!hasObservedPosition)
            {
                lastObservedPosition = currentPosition;
                hasObservedPosition = true;
            }

            UpdateFacingAndAnimation(lastObservedPosition, currentPosition);
            lastObservedPosition = currentPosition;
        }

        private void SetTargetPosition(Vector2 worldPosition, bool snap)
        {
            if (movementView == null)
            {
                LogMissingMovementView();
                return;
            }

            var newTargetPosition = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
            var movementDelta = movementView.HasPosition
                ? newTargetPosition - movementView.TargetPosition
                : newTargetPosition - transform.position;

            if (movementDelta.x > animationMoveThreshold)
                facingLeft = false;
            else if (movementDelta.x < -animationMoveThreshold)
                facingLeft = true;

            if (movementDelta.sqrMagnitude > animationMoveThreshold * animationMoveThreshold)
                moveAnimationTimer = animationHoldDuration;

            var snapped = movementView.FollowSnapshot(
                newTargetPosition,
                snap,
                moveSmoothing,
                teleportSnapDistance,
                animationMoveThreshold);
            if (snapped)
            {
                moveAnimationTimer = 0f;
                SyncMoveAnimation(false);
                lastObservedPosition = transform.position;
                hasObservedPosition = true;
            }
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
            var remainingDistance = movementView != null
                ? Vector3.Distance(nextPosition, movementView.TargetPosition)
                : 0f;
            var isChasingTarget = remainingDistance > animationMoveThreshold;

            if (isMovingThisFrame || isChasingTarget)
                moveAnimationTimer = animationHoldDuration;
            else if (moveAnimationTimer > 0f)
                moveAnimationTimer = Mathf.Max(0f, moveAnimationTimer - Time.deltaTime);

            SyncMoveAnimation(isMovingThisFrame || isChasingTarget || moveAnimationTimer > 0f);
        }

        private void SyncMoveAnimation(bool isMoving)
        {
            var resolvedAnimator = ResolveAnimator();
            if (resolvedAnimator == null || !hasMoveSpeedParameter)
                return;

            resolvedAnimator.SetFloat(moveSpeedParameterHash, isMoving ? 1f : 0f);
        }

        private void ApplyFacing()
        {
            var resolvedVisualRoot = ResolveVisualRoot();
            if (resolvedVisualRoot == null)
                return;

            if (Mathf.Approximately(visualDefaultScaleX, 0f))
                visualDefaultScaleX = 1f;

            var targetScaleX = facingLeft == visualFacesLeftByDefault
                ? visualDefaultScaleX
                : -visualDefaultScaleX;

            var scale = resolvedVisualRoot.localScale;
            scale.x = targetScaleX;
            resolvedVisualRoot.localScale = scale;
        }

        private void DisableLocalOnlyComponents()
        {
            if (localActionController != null)
                localActionController.enabled = false;

            if (localInputSourcesToDisable != null)
            {
                for (var i = 0; i < localInputSourcesToDisable.Length; i++)
                {
                    if (localInputSourcesToDisable[i] != null)
                        localInputSourcesToDisable[i].enabled = false;
                }
            }

            var resolvedBody = body != null ? body : playerView != null ? playerView.Body : null;
            if (resolvedBody != null)
            {
                resolvedBody.velocity = Vector2.zero;
                resolvedBody.angularVelocity = 0f;
                // Keep the rigidbody in the 2D physics world so trigger-based target
                // colliders can still be hit by OverlapPoint queries.
                resolvedBody.bodyType = RigidbodyType2D.Kinematic;
                resolvedBody.gravityScale = 0f;
                resolvedBody.simulated = true;
            }

            var resolvedBodyCollider = bodyCollider != null ? bodyCollider : playerView != null ? playerView.BodyCollider : null;
            if (resolvedBodyCollider != null)
                resolvedBodyCollider.enabled = false;
        }

        private void CacheAnimatorParameters()
        {
            var resolvedAnimator = ResolveAnimator();
            if (resolvedAnimator == null)
            {
                hasMoveSpeedParameter = false;
                moveSpeedParameterHash = 0;
                return;
            }

            moveSpeedParameterHash = Animator.StringToHash(MoveSpeedParameterName);
            hasMoveSpeedParameter = false;
            var parameters = resolvedAnimator.parameters;
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

        private void ConfigureTargetable(ObservedCharacterModel observedCharacter)
        {
            if (targetable == null)
            {
                LogMissingTargetable();
                return;
            }

            var handle = WorldTargetHandle.CreateObservedCharacter(observedCharacter.Character.CharacterId);
            targetable.Configure(handle);

            if (skillPresenter == null)
            {
                if (!warnedMissingSkillPresenter)
                {
                    ClientLog.Error(
                        $"RemoteCharacterPresenter requires CharacterSkillPresenter on prefab '{gameObject.name}'. Assign the reference on the prefab.");
                    warnedMissingSkillPresenter = true;
                }

                return;
            }

            skillPresenter.ConfigureCharacter(observedCharacter.Character.CharacterId);
            skillPresenter.ConfigureTargetHandle(handle);
        }

        private bool ValidateRequiredReferences()
        {
            var valid = true;
            if (targetable == null)
            {
                LogMissingTargetable();
                valid = false;
            }

            if (movementView == null)
            {
                LogMissingMovementView();
                valid = false;
            }

            if (skillPresenter == null)
            {
                if (!warnedMissingSkillPresenter)
                {
                    ClientLog.Error(
                        $"RemoteCharacterPresenter requires CharacterSkillPresenter on prefab '{gameObject.name}'. Add the component to the remote player prefab and assign the reference.");
                    warnedMissingSkillPresenter = true;
                }

                valid = false;
            }

            if (ResolveVisualRoot() == null)
            {
                if (!warnedMissingVisualRoot)
                {
                    ClientLog.Error($"RemoteCharacterPresenter on '{gameObject.name}' requires Visual Root reference or PlayerView.VisualRoot.");
                    warnedMissingVisualRoot = true;
                }

                valid = false;
            }

            if (ResolveAnimator() == null)
            {
                if (!warnedMissingAnimator)
                {
                    ClientLog.Error($"RemoteCharacterPresenter on '{gameObject.name}' requires Animator reference or PlayerView.Animator.");
                    warnedMissingAnimator = true;
                }

                valid = false;
            }

            return valid;
        }

        private Transform ResolveVisualRoot()
        {
            return visualRoot != null ? visualRoot : playerView != null ? playerView.VisualRoot : null;
        }

        private Animator ResolveAnimator()
        {
            return animator != null ? animator : playerView != null ? playerView.Animator : null;
        }

        private void LogMissingTargetable()
        {
            if (warnedMissingTargetable)
                return;

            ClientLog.Error($"RemoteCharacterPresenter on '{gameObject.name}' requires WorldTargetable reference.");
            warnedMissingTargetable = true;
        }

        private void LogMissingMovementView()
        {
            if (warnedMissingMovementView)
                return;

            ClientLog.Error($"RemoteCharacterPresenter on '{gameObject.name}' requires WorldEntityMovementView reference.");
            warnedMissingMovementView = true;
        }
    }
}
