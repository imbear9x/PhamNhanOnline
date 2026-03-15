using PhamNhanOnline.Client.Core.Logging;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.Character.Presentation
{
    [DisallowMultipleComponent]
    public sealed class LocalCharacterActionController : MonoBehaviour
    {
        private static readonly string[] AttackStateNames = { "Attack", "Attack2" };
        private const string MoveSpeedParameterName = "MoveSpeed";

        private enum MovementPresentationPhase
        {
            Grounded,
            Takeoff,
            Flight,
            Falling,
        }

        [Header("Scene References")]
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Collider2D bodyCollider;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private Animator animator;

        [Header("Animation States")]
        [SerializeField] private string idleStateName = "Idle";
        [SerializeField] private string runStateName = "Run";
        [SerializeField] private string takeoffStateName = "Jump";
        [SerializeField] private string flightStateName = "Fly";
        [SerializeField] private string fallingStateName = "Fall";

        [Header("Presentation")]
        [SerializeField] private bool visualFacesLeftByDefault = true;

        private readonly Collider2D[] groundHits = new Collider2D[8];
        private readonly int[] attackStateHashes = new int[AttackStateNames.Length];
        private readonly bool[] hasAttackState = new bool[AttackStateNames.Length];

        private LocalCharacterActionConfig actionConfig;
        private PlayerView playerView;
        private float visualDefaultScaleX = 1f;
        private int speedStatPercent = 100;
        private float horizontalInput;
        private float verticalInput;
        private bool isGrounded;
        private bool wasGroundedLastPhysicsStep = true;
        private bool facingLeft = true;
        private float attackTimer;
        private float attackCooldownTimer;
        private int attackVariantIndex;
        private int currentAnimationHash;
        private bool hasCurrentAnimation;
        private bool hasAirborneAnchor;
        private bool hoverTriggeredForCurrentFlight;
        private bool mustLandBeforeFlyingAgain;
        private bool wasMovingHorizontallyInFlightLastPhysicsStep;
        private float airborneStartY;
        private float hoverTimer;
        private MovementPresentationPhase movementPresentationPhase = MovementPresentationPhase.Grounded;
        private Animator cachedAnimatorForStates;
        private int idleStateHash;
        private int runStateHash;
        private int takeoffStateHash;
        private int flightStateHash;
        private int fallingStateHash;
        private int moveSpeedParameterHash;
        private bool hasIdleState;
        private bool hasRunState;
        private bool hasTakeoffState;
        private bool hasFlightState;
        private bool hasFallingState;
        private bool hasMoveSpeedParameter;

        public void Initialize(LocalCharacterActionConfig config, int baseSpeedPercent)
        {
            actionConfig = config != null ? config : LocalCharacterActionConfig.CreateRuntimeDefaults();
            speedStatPercent = baseSpeedPercent > 0
                ? baseSpeedPercent
                : actionConfig.SpeedStatBaseline;
            AutoWireMissingReferences();
            ConfigureBodyForLocalSimulation();
            CacheAnimatorStatesIfNeeded(force: true);

            if (visualRoot != null)
                visualDefaultScaleX = visualRoot.localScale.x;

            facingLeft = visualFacesLeftByDefault;
            ApplyFacing();
            DeactivateFlightPresentation();
        }

        public void SetSpeedStatPercent(int baseSpeedPercent)
        {
            if (baseSpeedPercent > 0)
                speedStatPercent = baseSpeedPercent;
        }

        public bool ShouldApplyAuthoritativeWorldPosition(Vector2 worldPosition, bool forceSnap, float teleportDistanceThreshold)
        {
            if (forceSnap || body == null)
                return true;

            var threshold = Mathf.Max(0f, teleportDistanceThreshold);
            if (threshold <= 0f)
                return true;

            return Vector2.Distance(body.position, worldPosition) >= threshold;
        }

        public void ApplyAuthoritativeWorldPosition(Vector2 worldPosition)
        {
            if (body != null)
            {
                body.position = worldPosition;
                body.velocity = Vector2.zero;
            }
            else
            {
                var current = transform.position;
                transform.position = new Vector3(worldPosition.x, worldPosition.y, current.z);
            }

            ResetAirborneState();
        }

        private void Awake()
        {
            AutoWireMissingReferences();
            ConfigureBodyForLocalSimulation();
            CacheAnimatorStatesIfNeeded(force: true);

            if (visualRoot != null)
                visualDefaultScaleX = visualRoot.localScale.x;

            facingLeft = visualFacesLeftByDefault;
            ApplyFacing();
            DeactivateFlightPresentation();
        }

        private void Update()
        {
            EnsureInitialized();
            if (actionConfig == null)
                return;

            horizontalInput = Input.GetAxisRaw("Horizontal");
            verticalInput = Input.GetAxisRaw("Vertical");

            if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.J)) && attackCooldownTimer <= 0f)
                StartAttack();

            if (attackTimer > 0f)
                attackTimer -= Time.deltaTime;
            if (attackCooldownTimer > 0f)
                attackCooldownTimer -= Time.deltaTime;

            UpdateFacing(horizontalInput);
            UpdateAnimation();
        }

        private void FixedUpdate()
        {
            EnsureInitialized();
            if (actionConfig == null || body == null)
                return;

            RefreshGrounded();
            HandleGroundedTransitions();

            var speedMultiplier = Mathf.Max(0.01f, speedStatPercent / Mathf.Max(1f, actionConfig.SpeedStatBaseline));
            var movementMultiplier = attackTimer > 0f ? actionConfig.AttackMovementMultiplier : 1f;
            var moveSpeed = actionConfig.BaseMoveSpeed * speedMultiplier * movementMultiplier;

            UpdateAirborneState();

            var velocity = body.velocity;
            velocity.x = horizontalInput * moveSpeed;

            var targetVerticalVelocity = ResolveVerticalVelocity(speedMultiplier);
            var verticalChangeRate = Mathf.Max(0.01f, actionConfig.VerticalVelocityChangeRate * speedMultiplier);
            velocity.y = Mathf.MoveTowards(velocity.y, targetVerticalVelocity, verticalChangeRate * Time.fixedDeltaTime);

            body.gravityScale = 0f;
            body.velocity = velocity;
        }

        private void StartAttack()
        {
            attackCooldownTimer = actionConfig != null ? actionConfig.AttackCooldown : 0.15f;
            attackTimer = actionConfig != null ? actionConfig.AttackDuration : 0.45f;

            if (hasAttackState[attackVariantIndex % AttackStateNames.Length])
            {
                var attackIndex = attackVariantIndex % AttackStateNames.Length;
                attackVariantIndex++;
                PlayCachedAnimation(attackStateHashes[attackIndex], true, hasAttackState[attackIndex]);
            }
        }

        private void HandleGroundedTransitions()
        {
            if (isGrounded)
            {
                if (!wasGroundedLastPhysicsStep)
                    EnterGroundedPresentation();
            }
            else if (wasGroundedLastPhysicsStep)
            {
                EnterTakeoffPresentation();
            }

            wasGroundedLastPhysicsStep = isGrounded;
        }

        private void UpdateAirborneState()
        {
            if (actionConfig == null || body == null)
                return;

            if (isGrounded)
            {
                ResetAirborneState();
                return;
            }

            if (mustLandBeforeFlyingAgain)
                return;

            var deadZone = actionConfig.MovementDeadZone;
            var pressingUp = verticalInput > deadZone;

            if (pressingUp && !hasAirborneAnchor)
            {
                hasAirborneAnchor = true;
                airborneStartY = body.position.y;
                hoverTriggeredForCurrentFlight = false;
                hoverTimer = 0f;
            }

            if (!hasAirborneAnchor || hoverTriggeredForCurrentFlight)
                return;

            var climbedHeight = body.position.y - airborneStartY;
            if (climbedHeight < actionConfig.HoverActivationHeight)
                return;

            if (CanUseFlight())
            {
                hoverTriggeredForCurrentFlight = true;
                hoverTimer = actionConfig.HoverDuration;
                EnterFlightPresentation();
                return;
            }

            hoverTriggeredForCurrentFlight = true;
            EnterFallingPresentation();
        }

        private float ResolveVerticalVelocity(float speedMultiplier)
        {
            if (actionConfig == null)
                return 0f;

            var deadZone = actionConfig.MovementDeadZone;
            var pressingUp = verticalInput > deadZone;
            var pressingDown = verticalInput < -deadZone;
            var movingHorizontally = Mathf.Abs(horizontalInput) > deadZone;
            var canUseFlight = CanUseFlight();

            if (!mustLandBeforeFlyingAgain && pressingUp)
            {
                if (canUseFlight)
                    return actionConfig.FlyUpSpeed * speedMultiplier;

                if (!hasAirborneAnchor || !hoverTriggeredForCurrentFlight)
                    return actionConfig.FlyUpSpeed * speedMultiplier;
            }

            if (isGrounded)
                return 0f;

            if (pressingDown)
            {
                hoverTimer = 0f;
                wasMovingHorizontallyInFlightLastPhysicsStep = false;
                EnterFallingPresentation();
                return -actionConfig.FallSpeed * speedMultiplier;
            }

            if (canUseFlight && movementPresentationPhase == MovementPresentationPhase.Flight)
            {
                if (movingHorizontally && !mustLandBeforeFlyingAgain)
                {
                    wasMovingHorizontallyInFlightLastPhysicsStep = true;
                    return 0f;
                }

                if (wasMovingHorizontallyInFlightLastPhysicsStep)
                {
                    wasMovingHorizontallyInFlightLastPhysicsStep = false;
                    hoverTimer = actionConfig.HoverDuration;
                    return 0f;
                }
            }

            if (canUseFlight && hoverTimer > 0f)
            {
                hoverTimer = Mathf.Max(0f, hoverTimer - Time.fixedDeltaTime);
                return 0f;
            }

            EnterFallingPresentation();
            return -actionConfig.FallSpeed * speedMultiplier;
        }

        private bool CanUseFlight()
        {
            // Placeholder for later equipment/status checks such as a flight artifact.
            return true;
        }

        private void EnterGroundedPresentation()
        {
            movementPresentationPhase = MovementPresentationPhase.Grounded;
            DeactivateFlightPresentation();
            mustLandBeforeFlyingAgain = false;
            wasMovingHorizontallyInFlightLastPhysicsStep = false;
        }

        private void EnterTakeoffPresentation()
        {
            movementPresentationPhase = MovementPresentationPhase.Takeoff;
            DeactivateFlightPresentation();
            PlayOptionalCachedAnimation(takeoffStateHash, hasTakeoffState, true);
        }

        private void EnterFlightPresentation()
        {
            if (movementPresentationPhase == MovementPresentationPhase.Flight)
                return;

            movementPresentationPhase = MovementPresentationPhase.Flight;
            wasMovingHorizontallyInFlightLastPhysicsStep = false;
            ActivateFlightPresentation();
            PlayOptionalCachedAnimation(flightStateHash, hasFlightState, true);
            OnFlightPresentationActivated();
        }

        private void EnterFallingPresentation()
        {
            if (movementPresentationPhase == MovementPresentationPhase.Falling)
                return;

            movementPresentationPhase = MovementPresentationPhase.Falling;
            mustLandBeforeFlyingAgain = true;
            wasMovingHorizontallyInFlightLastPhysicsStep = false;
            DeactivateFlightPresentation();
            PlayOptionalCachedAnimation(fallingStateHash, hasFallingState, true);
            OnFallingPresentationActivated();
        }

        private void ActivateFlightPresentation()
        {
            // Placeholder for future flight-only presentation such as attaching a flying artifact or starting VFX.
        }

        private void DeactivateFlightPresentation()
        {
            // Placeholder for future cleanup when flight presentation ends.
        }

        private void OnFlightPresentationActivated()
        {
            // Placeholder for future flight-only logic such as enabling buffs or presentation state.
        }

        private void OnFallingPresentationActivated()
        {
            // Placeholder for future fall-only logic such as detaching flight state or stopping VFX.
        }

        private void UpdateAnimation()
        {
            if (animator == null || actionConfig == null)
                return;

            if (attackTimer > 0f)
                return;

            var hasHorizontalInput = Mathf.Abs(horizontalInput) > actionConfig.MovementDeadZone;
            var hasHorizontalVelocity = body != null && Mathf.Abs(body.velocity.x) > actionConfig.MovementDeadZone;
            var shouldRun = hasHorizontalInput || hasHorizontalVelocity;
            SyncLocomotionParameters(shouldRun ? 1f : 0f);

            switch (movementPresentationPhase)
            {
                case MovementPresentationPhase.Takeoff:
                    PlayOptionalCachedAnimation(takeoffStateHash, hasTakeoffState, false);
                    return;
                case MovementPresentationPhase.Flight:
                    PlayOptionalCachedAnimation(flightStateHash, hasFlightState, false);
                    return;
                case MovementPresentationPhase.Falling:
                    PlayOptionalCachedAnimation(fallingStateHash, hasFallingState, false);
                    return;
            }

            if (hasMoveSpeedParameter && hasIdleState && hasRunState)
                return;

            if (shouldRun)
                PlayCachedAnimation(runStateHash, false, hasRunState);
            else
                PlayCachedAnimation(idleStateHash, false, hasIdleState);
        }

        private void SyncLocomotionParameters(float moveSpeedValue)
        {
            if (animator == null || !hasMoveSpeedParameter)
                return;

            animator.SetFloat(moveSpeedParameterHash, moveSpeedValue);
        }

        private void UpdateFacing(float inputX)
        {
            if (visualRoot == null || actionConfig == null)
                return;

            if (inputX > actionConfig.MovementDeadZone)
                facingLeft = false;
            else if (inputX < -actionConfig.MovementDeadZone)
                facingLeft = true;

            ApplyFacing();
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

        private void RefreshGrounded()
        {
            if (groundCheck == null || actionConfig == null)
            {
                isGrounded = false;
                return;
            }

            var count = Physics2D.OverlapCircleNonAlloc(
                groundCheck.position,
                actionConfig.GroundCheckRadius,
                groundHits);

            isGrounded = false;
            for (var i = 0; i < count; i++)
            {
                var hit = groundHits[i];
                groundHits[i] = null;

                if (hit == null || hit.isTrigger)
                    continue;

                if (bodyCollider != null && hit == bodyCollider)
                    continue;

                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    continue;

                isGrounded = true;
                break;
            }
        }

        private void ConfigureBodyForLocalSimulation()
        {
            if (body == null)
                return;

            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.freezeRotation = true;
        }

        private void CacheAnimatorStatesIfNeeded(bool force = false)
        {
            if (animator == null)
            {
                cachedAnimatorForStates = null;
                ResetCachedAnimatorStates();
                return;
            }

            if (!force && cachedAnimatorForStates == animator)
                return;

            cachedAnimatorForStates = animator;
            moveSpeedParameterHash = Animator.StringToHash(MoveSpeedParameterName);
            hasMoveSpeedParameter = HasAnimatorParameter(MoveSpeedParameterName, AnimatorControllerParameterType.Float);
            CacheAnimatorState(idleStateName, out idleStateHash, out hasIdleState, true);
            CacheAnimatorState(runStateName, out runStateHash, out hasRunState, true);
            CacheAnimatorState(takeoffStateName, out takeoffStateHash, out hasTakeoffState, false);
            CacheAnimatorState(flightStateName, out flightStateHash, out hasFlightState, false);
            CacheAnimatorState(fallingStateName, out fallingStateHash, out hasFallingState, false);

            for (var i = 0; i < AttackStateNames.Length; i++)
                CacheAnimatorState(AttackStateNames[i], out attackStateHashes[i], out hasAttackState[i], true);
        }

        private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType expectedType)
        {
            if (animator == null)
                return false;

            var parameters = animator.parameters;
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (parameter.type == expectedType && parameter.name == parameterName)
                    return true;
            }

            return false;
        }

        private void CacheAnimatorState(string stateName, out int stateHash, out bool hasState, bool warnIfMissing)
        {
            stateHash = 0;
            hasState = false;

            if (animator == null || string.IsNullOrWhiteSpace(stateName))
                return;

            stateHash = Animator.StringToHash(stateName);
            hasState = animator.HasState(0, stateHash);

            if (!hasState && warnIfMissing)
                ClientLog.Warn($"Animator on {name} is missing state '{stateName}'.");
        }

        private void ResetCachedAnimatorStates()
        {
            idleStateHash = 0;
            runStateHash = 0;
            takeoffStateHash = 0;
            flightStateHash = 0;
            fallingStateHash = 0;
            moveSpeedParameterHash = 0;
            hasIdleState = false;
            hasRunState = false;
            hasTakeoffState = false;
            hasFlightState = false;
            hasFallingState = false;
            hasMoveSpeedParameter = false;
            hasCurrentAnimation = false;
            currentAnimationHash = 0;

            for (var i = 0; i < AttackStateNames.Length; i++)
            {
                attackStateHashes[i] = 0;
                hasAttackState[i] = false;
            }
        }

        private void PlayOptionalCachedAnimation(int stateHash, bool hasState, bool restart)
        {
            if (!hasState)
                return;

            PlayCachedAnimation(stateHash, restart, true);
        }

        private void PlayCachedAnimation(int stateHash, bool restart, bool hasState)
        {
            if (animator == null || !hasState)
                return;

            if (!restart && hasCurrentAnimation && currentAnimationHash == stateHash)
                return;

            animator.Play(stateHash, 0, 0f);
            currentAnimationHash = stateHash;
            hasCurrentAnimation = true;
        }

        private void EnsureInitialized()
        {
            if (actionConfig == null)
                actionConfig = LocalCharacterActionConfig.CreateRuntimeDefaults();

            AutoWireMissingReferences();
            ConfigureBodyForLocalSimulation();
            CacheAnimatorStatesIfNeeded();
        }

        private void AutoWireMissingReferences()
        {
            if (playerView == null)
                playerView = GetComponent<PlayerView>();

            if (playerView != null)
            {
                if (body == null)
                    body = playerView.Body;
                if (bodyCollider == null)
                    bodyCollider = playerView.BodyCollider;
                if (visualRoot == null)
                    visualRoot = playerView.VisualRoot;
                if (groundCheck == null)
                    groundCheck = playerView.GroundCheck;
                if (animator == null)
                    animator = playerView.Animator;
            }

            if (body == null)
                body = GetComponent<Rigidbody2D>();

            if (bodyCollider == null)
                bodyCollider = GetComponent<Collider2D>();

            if (visualRoot == null)
            {
                var child = transform.Find("VisualRoot");
                visualRoot = child != null ? child : transform;
            }

            if (groundCheck == null)
            {
                var child = transform.Find("GroundCheck");
                if (child != null)
                    groundCheck = child;
            }

            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);
        }

        private void ResetAirborneState()
        {
            hasAirborneAnchor = false;
            hoverTriggeredForCurrentFlight = false;
            mustLandBeforeFlyingAgain = false;
            wasMovingHorizontallyInFlightLastPhysicsStep = false;
            hoverTimer = 0f;
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck == null || actionConfig == null)
                return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(groundCheck.position, actionConfig.GroundCheckRadius);
        }
    }
}
