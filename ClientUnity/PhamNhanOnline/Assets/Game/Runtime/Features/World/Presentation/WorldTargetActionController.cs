using System;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Features.Targeting.Application;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    public sealed partial class WorldTargetActionController : WorldSceneBehaviour
    {
        private const int BasicSkillSlotIndex = 1;

        private struct PendingTargetAction
        {
            public WorldTargetHandle Target;
            public WorldTargetInteractionMode Mode;
        }

        [Header("References")]
        [SerializeField] private WorldMapPresenter worldMapPresenter;
        [SerializeField] private WorldPortalPresenter worldPortalPresenter;
        [SerializeField] private WorldLocalPlayerPresenter worldLocalPlayerPresenter;
        [SerializeField] private WorldLocalMovementSyncController worldLocalMovementSyncController;

        [Header("Ranges")]
        [SerializeField] private float interactionRangeServerUnits = 30f;
        [SerializeField] private float actionRangeBufferServerUnits = 2f;
        [SerializeField] private float portalActionRangeBufferServerUnits = 4f;
        [SerializeField] private float arrivalDeadZoneWorldUnits = 0.05f;

        [Header("Behavior")]
        [SerializeField] private bool pinTargetWhileApproaching = true;
        [SerializeField] private bool logInteractionPlaceholder = true;

        private PendingTargetAction? pendingAction;
        private bool autoPinApplied;
        private bool loggedMissingWorldMapPresenter;
        private bool loggedMissingLocalPlayerPresenter;

        public event Action<WorldTargetHandle> InteractionRequested;

        public float InteractionRangeServerUnits => Mathf.Max(0f, interactionRangeServerUnits);
        public float ActionRangeBufferServerUnits => Mathf.Max(0f, actionRangeBufferServerUnits);
        public float PortalActionRangeBufferServerUnits => Mathf.Max(0f, portalActionRangeBufferServerUnits);

        private void Awake()
        {
            AutoWireReferences();
        }

        private void Start()
        {
            AutoWireReferences();
            LogMissingCriticalWorldSceneDependenciesIfNeeded();
            LogMissingCriticalDependenciesIfNeeded();
            TryBindRuntimeEvents();
        }

        private void OnEnable()
        {
            TryBindRuntimeEvents();
        }

        private void OnDisable()
        {
            UnbindRuntimeEvents();
            CancelPendingAction(clearPin: true);
        }

        private void OnDestroy()
        {
            UnbindRuntimeEvents();
        }

        private void Update()
        {
            if (!pendingAction.HasValue || !ClientRuntime.IsInitialized)
                return;

            AutoWireReferences();
            if (!IsActionRuntimeReady())
                return;

            var action = pendingAction.Value;
            if (!action.Target.IsValid)
            {
                CancelPendingAction(clearPin: true);
                return;
            }

            var localActionController = ResolveLocalActionController();
            if (localActionController == null || worldMapPresenter == null)
                return;

            if (ClientRuntime.Combat.HasPendingAttackRequest || ClientRuntime.Combat.IsLocalCastActive(DateTime.UtcNow))
            {
                localActionController.ClearExternalMoveOverride();
                return;
            }

            if (IsLocalCharacterDead())
            {
                CancelPendingAction(clearPin: true);
                return;
            }

            Vector2 playerWorldPosition;
            if (!TryResolveLocalPlayerWorldPosition(out playerWorldPosition))
                return;

            Vector2 targetWorldPosition;
            if (!TryResolveTargetWorldPosition(action.Target, out targetWorldPosition))
            {
                CancelPendingAction(clearPin: true);
                return;
            }

            float distanceServerUnits;
            if (TryResolveDistanceServerUnits(playerWorldPosition, targetWorldPosition, out distanceServerUnits))
            {
                var requiredRange = ResolveRequiredRangeServerUnits(action);
                var rangeBuffer = ResolveRangeBufferServerUnits(action);
                if (distanceServerUnits <= requiredRange + rangeBuffer)
                {
                    localActionController.ClearExternalMoveOverride();
                    ExecutePendingAction(action);
                    return;
                }

                Vector2 preferredMoveOverride;
                if (TryResolvePreferredApproachMoveOverride(
                        action,
                        playerWorldPosition,
                        targetWorldPosition,
                        requiredRange,
                        out preferredMoveOverride))
                {
                    localActionController.SetExternalMoveOverride(preferredMoveOverride);
                    return;
                }
            }

            var delta = targetWorldPosition - playerWorldPosition;
            if (delta.sqrMagnitude <= arrivalDeadZoneWorldUnits * arrivalDeadZoneWorldUnits)
            {
                localActionController.ClearExternalMoveOverride();
                ExecutePendingAction(action);
                return;
            }

            localActionController.SetExternalMoveOverride(delta.normalized);
        }

        public bool RequestPrimaryAction(WorldTargetHandle target)
        {
            if (!ClientRuntime.IsInitialized)
                return false;

            if (!target.IsValid)
                return false;

            if (IsLocalCharacterDead())
                return false;

            AutoWireReferences();
            if (!IsActionRuntimeReady())
                return false;

            var mode = WorldTargetInteractionRules.Resolve(target);
            if (mode == WorldTargetInteractionMode.None)
                return false;

            if (mode == WorldTargetInteractionMode.HostileAttack && !CanUseBasicSkillNow())
                return false;

            ClientRuntime.Target.Select(target);
            pendingAction = new PendingTargetAction
            {
                Target = target,
                Mode = mode
            };

            autoPinApplied = false;
            if (pinTargetWhileApproaching && ClientRuntime.Target.PinMode == TargetPinMode.None)
                autoPinApplied = ClientRuntime.Target.PinCurrent(TargetPinMode.Manual);

            return true;
        }
    }
}
