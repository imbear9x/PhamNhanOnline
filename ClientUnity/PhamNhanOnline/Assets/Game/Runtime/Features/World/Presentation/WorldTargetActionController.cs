using System;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
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
        private WorldMapPresenter worldMapPresenter;
        private WorldPortalPresenter worldPortalPresenter;
        private WorldLocalPlayerPresenter worldLocalPlayerPresenter;
        private WorldLocalMovementSyncController worldLocalMovementSyncController;

        [Header("Ranges")]
        [SerializeField] private float interactionRangeServerUnits = 30f;
        [SerializeField] private float actionRangeBufferServerUnits = 2f;
        [SerializeField] private float portalActionRangeBufferServerUnits = 4f;
        [SerializeField] private float arrivalDeadZoneWorldUnits = 0.05f;

        [Header("Behavior")]
        [SerializeField] private bool pinTargetWhileApproaching = true;
        [SerializeField] private bool logInteractionPlaceholder = true;

        [Header("Diagnostics")]
        [SerializeField] private bool logTargetActionDiagnostics = true;
        [SerializeField] private float targetActionDiagnosticIntervalSeconds = 0.75f;

        private PendingTargetAction? pendingAction;
        private bool autoPinApplied;
        private bool loggedMissingWorldMapPresenter;
        private bool loggedMissingLocalPlayerPresenter;
        private float nextTargetActionDiagnosticTime;

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
            {
                LogTargetActionDiagnostic(
                    $"waiting-dependencies target={DescribeTarget(action.Target)} localAction={(localActionController != null)} worldMap={(worldMapPresenter != null)}",
                    throttle: true);
                return;
            }

            if (ClientRuntime.Combat.HasPendingAttackRequest || ClientRuntime.Combat.IsLocalCastActive(DateTime.UtcNow))
            {
                LogTargetActionDiagnostic(
                    $"waiting-combat target={DescribeTarget(action.Target)} pendingAttack={ClientRuntime.Combat.HasPendingAttackRequest}",
                    throttle: true);
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
            {
                LogTargetActionDiagnostic($"waiting-player-position target={DescribeTarget(action.Target)}", throttle: true);
                return;
            }

            Vector2 targetWorldPosition;
            if (!TryResolveTargetWorldPosition(action.Target, out targetWorldPosition))
            {
                LogTargetActionDiagnostic(
                    $"cancel-no-target-position target={DescribeTarget(action.Target)} mode={action.Mode}",
                    throttle: false);
                CancelPendingAction(clearPin: true);
                return;
            }

            var requiredRange = ResolveRequiredRangeServerUnits(action);
            var rangeBuffer = ResolveRangeBufferServerUnits(action);
            Vector2 deltaServer;
            float distanceServerUnits;
            float horizontalRangeServerUnits;
            float verticalRangeServerUnits;
            if (TryResolveTargetRangeServerUnits(
                    playerWorldPosition,
                    targetWorldPosition,
                    requiredRange,
                    rangeBuffer,
                    out deltaServer,
                    out distanceServerUnits,
                    out horizontalRangeServerUnits,
                    out verticalRangeServerUnits))
            {
                if (IsWithinActionRange(deltaServer, horizontalRangeServerUnits, verticalRangeServerUnits))
                {
                    LogTargetActionDiagnostic(
                        $"execute-in-range target={DescribeTarget(action.Target)} mode={action.Mode} distanceServer={distanceServerUnits:0.##} deltaServer={FormatVector(deltaServer)} rangeX={horizontalRangeServerUnits:0.##} rangeY={verticalRangeServerUnits:0.##}",
                        throttle: false);
                    localActionController.ClearExternalMoveOverride();
                    ExecutePendingAction(action);
                    return;
                }

                Vector2 preferredMoveOverride;
                if (TryResolvePreferredApproachMoveOverride(
                        deltaServer,
                        horizontalRangeServerUnits,
                        verticalRangeServerUnits,
                        out preferredMoveOverride))
                {
                    LogTargetActionDiagnostic(
                        $"move-to-range target={DescribeTarget(action.Target)} mode={action.Mode} playerWorld={FormatVector(playerWorldPosition)} targetWorld={FormatVector(targetWorldPosition)} distanceServer={distanceServerUnits:0.##} deltaServer={FormatVector(deltaServer)} rangeX={horizontalRangeServerUnits:0.##} rangeY={verticalRangeServerUnits:0.##} move={FormatVector(preferredMoveOverride)}",
                        throttle: true);
                    localActionController.SetExternalMoveOverride(preferredMoveOverride);
                    return;
                }
            }

            var delta = targetWorldPosition - playerWorldPosition;
            if (delta.sqrMagnitude <= arrivalDeadZoneWorldUnits * arrivalDeadZoneWorldUnits)
            {
                LogTargetActionDiagnostic(
                    $"execute-world-deadzone target={DescribeTarget(action.Target)} mode={action.Mode} playerWorld={FormatVector(playerWorldPosition)} targetWorld={FormatVector(targetWorldPosition)}",
                    throttle: false);
                localActionController.ClearExternalMoveOverride();
                ExecutePendingAction(action);
                return;
            }

            LogTargetActionDiagnostic(
                $"move-world-direct target={DescribeTarget(action.Target)} mode={action.Mode} playerWorld={FormatVector(playerWorldPosition)} targetWorld={FormatVector(targetWorldPosition)} delta={FormatVector(delta)}",
                throttle: true);
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

            LogTargetActionDiagnostic(
                $"request target={DescribeTarget(target)} mode={mode}",
                throttle: false);

            autoPinApplied = false;
            if (pinTargetWhileApproaching && ClientRuntime.Target.PinMode == TargetPinMode.None)
                autoPinApplied = ClientRuntime.Target.PinCurrent(TargetPinMode.Manual);

            return true;
        }

        private void LogTargetActionDiagnostic(string message, bool throttle)
        {
            if (!logTargetActionDiagnostics)
                return;

            if (throttle)
            {
                var now = Time.unscaledTime;
                if (now < nextTargetActionDiagnosticTime)
                    return;

                nextTargetActionDiagnosticTime = now + Mathf.Max(0.1f, targetActionDiagnosticIntervalSeconds);
            }

            ClientLog.Info("[TargetAction] " + message);
            WorldTravelDebugController.SetExternalCharacterStatsDebugLine("[TargetAction] " + message);
        }

        private static string DescribeTarget(WorldTargetHandle target)
        {
            return target.Kind + "/" + target.TargetId;
        }

        private static string FormatVector(Vector2 value)
        {
            return value.x.ToString("0.##") + "," + value.y.ToString("0.##");
        }
    }
}
