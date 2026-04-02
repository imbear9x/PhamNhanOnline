using System;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Features.Character.Presentation;
using PhamNhanOnline.Client.Features.Character.Application;
using PhamNhanOnline.Client.Features.Skills.Application;
using PhamNhanOnline.Client.Features.Targeting.Application;
using PhamNhanOnline.Client.Features.World.Application;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    public sealed class WorldTargetActionController : WorldSceneBehaviour
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
            if (!ClientRuntime.IsInitialized || !target.IsValid)
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

        private void ExecutePendingAction(PendingTargetAction action)
        {
            CancelMovementOnly();

            switch (action.Mode)
            {
                case WorldTargetInteractionMode.HostileAttack:
                    if (!CanUseBasicSkillNow())
                    {
                        CompletePendingAction(clearAutoPin: true);
                        return;
                    }

                    if (worldLocalMovementSyncController != null)
                        worldLocalMovementSyncController.TryForceSyncCurrentPosition();

                    ClientRuntime.CombatService.TryUseBasicSkillOnTarget(action.Target);
                    CompletePendingAction(clearAutoPin: true);
                    break;

                case WorldTargetInteractionMode.ContextOnly:
                    var handler = InteractionRequested;
                    if (handler != null)
                        handler(action.Target);

                    if (logInteractionPlaceholder)
                    {
                        ClientLog.Info($"Target interaction requested for {action.Target.Kind}/{action.Target.TargetId}.");
                        WorldTravelDebugController.SetExternalCharacterStatsDebugLine(
                            $"Da vao tam tuong tac voi {action.Target.Kind}/{action.Target.TargetId}.");
                    }

                    // Keep the manual pin on context interactions such as portals so the
                    // selected-highlight stays stable until the user changes target or the map changes.
                    CompletePendingAction(clearAutoPin: false);
                    break;
            }
        }

        private void CompletePendingAction(bool clearAutoPin)
        {
            pendingAction = null;
            if (clearAutoPin && autoPinApplied)
            {
                ClientRuntime.Target.ClearPin();
                autoPinApplied = false;
            }
        }

        private void CancelPendingAction(bool clearPin)
        {
            CancelMovementOnly();
            pendingAction = null;
            if (clearPin && autoPinApplied)
            {
                ClientRuntime.Target.ClearPin();
                autoPinApplied = false;
            }
        }

        private void CancelMovementOnly()
        {
            var localActionController = ResolveLocalActionController();
            if (localActionController != null)
                localActionController.ClearExternalMoveOverride();
        }

        private float ResolveRequiredRangeServerUnits(PendingTargetAction action)
        {
            GameShared.Models.MapPortalModel portal;
            if (action.Mode == WorldTargetInteractionMode.ContextOnly &&
                ClientRuntime.World.TryGetPortal(action.Target, out portal))
            {
                return Mathf.Max(0f, portal.InteractionRadius);
            }

            if (action.Mode == WorldTargetInteractionMode.HostileAttack)
            {
                PlayerSkillModel skill;
                if (ClientRuntime.Skills.TryGetLoadoutSkill(BasicSkillSlotIndex, out skill))
                    return Mathf.Max(0f, skill.CastRange);
            }

            return Mathf.Max(0f, interactionRangeServerUnits);
        }

        private float ResolveRangeBufferServerUnits(PendingTargetAction action)
        {
            GameShared.Models.MapPortalModel portal;
            if (action.Mode == WorldTargetInteractionMode.ContextOnly &&
                ClientRuntime.World.TryGetPortal(action.Target, out portal))
            {
                return Mathf.Max(0f, portalActionRangeBufferServerUnits);
            }

            return Mathf.Max(0f, actionRangeBufferServerUnits);
        }

        private bool CanUseBasicSkillNow()
        {
            if (!ClientRuntime.IsInitialized)
                return false;

            if (IsLocalCharacterDead())
                return false;

            var utcNow = DateTime.UtcNow;
            if (ClientRuntime.Combat.HasPendingAttackRequest || ClientRuntime.Combat.IsLocalCastActive(utcNow))
                return false;

            PlayerSkillModel basicSkill;
            if (!ClientRuntime.Skills.TryGetLoadoutSkill(BasicSkillSlotIndex, out basicSkill))
                return false;

            float _;
            int __;
            int ___;
            return !ClientRuntime.Combat.TryGetCooldownForSlot(
                BasicSkillSlotIndex,
                basicSkill.PlayerSkillId,
                utcNow,
                out _,
                out __,
                out ___);
        }

        private bool TryResolveLocalPlayerWorldPosition(out Vector2 worldPosition)
        {
            worldPosition = default;

            if (worldLocalPlayerPresenter == null || worldLocalPlayerPresenter.CurrentPlayerTransform == null)
                return false;

            var position = worldLocalPlayerPresenter.CurrentPlayerTransform.position;
            worldPosition = new Vector2(position.x, position.y);
            return true;
        }

        private bool TryResolveTargetWorldPosition(WorldTargetHandle target, out Vector2 worldPosition)
        {
            MapPortalModel portal;
            if (ClientRuntime.World.TryGetPortal(target, out portal))
            {
                if (worldPortalPresenter != null &&
                    worldPortalPresenter.TryResolvePortalWorldPosition(portal, out worldPosition))
                {
                    return true;
                }

                return worldMapPresenter.TryMapServerPositionToWorld(
                    new Vector2(portal.SourceX, portal.SourceY),
                    out worldPosition);
            }

            WorldTargetable targetable;
            if (WorldTargetableRegistry.TryGet(target, out targetable) &&
                targetable != null &&
                targetable.isActiveAndEnabled &&
                targetable.TryGetWorldSelectionPosition(out worldPosition))
            {
                return true;
            }

            switch (target.Kind)
            {
                case WorldTargetKind.Player:
                    Guid characterId;
                    if (!Guid.TryParse(target.TargetId, out characterId))
                        break;

                    GameShared.Models.ObservedCharacterModel observedCharacter;
                    if (ClientRuntime.World.TryGetObservedCharacter(characterId, out observedCharacter))
                    {
                        return worldMapPresenter.TryMapServerPositionToWorld(
                            new Vector2(observedCharacter.CurrentState.CurrentPosX, observedCharacter.CurrentState.CurrentPosY),
                            out worldPosition);
                    }

                    break;

                case WorldTargetKind.Enemy:
                case WorldTargetKind.Boss:
                    int runtimeId;
                    if (!int.TryParse(target.TargetId, out runtimeId))
                        break;

                    GameShared.Models.EnemyRuntimeModel enemy;
                    if (ClientRuntime.World.TryGetEnemy(runtimeId, out enemy))
                    {
                        return worldMapPresenter.TryMapServerPositionToWorld(
                            new Vector2(enemy.PosX, enemy.PosY),
                            out worldPosition);
                    }

                    break;

                case WorldTargetKind.GroundReward:
                    int rewardId;
                    if (!ClientWorldState.TryParseGroundRewardTargetId(target.TargetId, out rewardId))
                        break;

                    GameShared.Models.GroundRewardModel reward;
                    if (ClientRuntime.World.TryGetGroundReward(rewardId, out reward))
                    {
                        return worldMapPresenter.TryMapServerPositionToWorld(
                            new Vector2(reward.PosX, reward.PosY),
                            out worldPosition);
                    }

                    break;
            }

            worldPosition = default;
            return false;
        }

        private bool TryResolveDistanceServerUnits(Vector2 playerWorldPosition, Vector2 targetWorldPosition, out float distanceServerUnits)
        {
            distanceServerUnits = 0f;
            if (worldMapPresenter == null)
                return false;

            Vector2 playerServerPosition;
            Vector2 targetServerPosition;
            if (!worldMapPresenter.TryMapWorldPositionToServer(playerWorldPosition, out playerServerPosition) ||
                !worldMapPresenter.TryMapWorldPositionToServer(targetWorldPosition, out targetServerPosition))
            {
                return false;
            }

            distanceServerUnits = Vector2.Distance(playerServerPosition, targetServerPosition);
            return true;
        }

        private bool TryResolvePreferredApproachMoveOverride(
            PendingTargetAction action,
            Vector2 playerWorldPosition,
            Vector2 targetWorldPosition,
            float requiredRangeServerUnits,
            out Vector2 moveOverride)
        {
            moveOverride = default;
            if (worldMapPresenter == null)
                return false;

            Vector2 playerServerPosition;
            Vector2 targetServerPosition;
            if (!worldMapPresenter.TryMapWorldPositionToServer(playerWorldPosition, out playerServerPosition) ||
                !worldMapPresenter.TryMapWorldPositionToServer(targetWorldPosition, out targetServerPosition))
            {
                return false;
            }

            var stopRangeServerUnits = Mathf.Max(0f, requiredRangeServerUnits) + ResolveRangeBufferServerUnits(action);
            var deltaServer = targetServerPosition - playerServerPosition;

            if (Mathf.Abs(deltaServer.x) > stopRangeServerUnits)
                return TryResolveWorldMoveOverrideFromServerDirection(new Vector2(Mathf.Sign(deltaServer.x), 0f), out moveOverride);

            if (Mathf.Abs(deltaServer.y) > Mathf.Epsilon)
                return TryResolveWorldMoveOverrideFromServerDirection(new Vector2(0f, Mathf.Sign(deltaServer.y)), out moveOverride);

            return false;
        }

        private bool TryResolveWorldMoveOverrideFromServerDirection(Vector2 serverDirection, out Vector2 moveOverride)
        {
            moveOverride = default;
            if (serverDirection.sqrMagnitude <= Mathf.Epsilon)
                return false;

            if (worldMapPresenter != null &&
                worldMapPresenter.TryGetWorldUnitsPerServerUnit(out var worldUnitsPerServerUnit))
            {
                var worldDirection = new Vector2(
                    serverDirection.x * Mathf.Max(worldUnitsPerServerUnit.x, Mathf.Epsilon),
                    serverDirection.y * Mathf.Max(worldUnitsPerServerUnit.y, Mathf.Epsilon));
                if (worldDirection.sqrMagnitude <= Mathf.Epsilon)
                    return false;

                moveOverride = worldDirection.normalized;
                return true;
            }

            moveOverride = serverDirection.normalized;
            return true;
        }

        private LocalCharacterActionController ResolveLocalActionController()
        {
            return worldLocalPlayerPresenter != null
                ? worldLocalPlayerPresenter.CurrentLocalActionController
                : null;
        }

        private void AutoWireReferences()
        {
            InitializeWorldSceneBehaviour(ref worldMapPresenter);
            if (worldPortalPresenter == null)
                worldPortalPresenter = GetComponent<WorldPortalPresenter>();

            if (worldLocalPlayerPresenter == null)
                worldLocalPlayerPresenter = SceneController != null ? SceneController.WorldLocalPlayerPresenter : GetComponent<WorldLocalPlayerPresenter>();

            if (worldLocalMovementSyncController == null)
                worldLocalMovementSyncController = GetComponent<WorldLocalMovementSyncController>();
        }

        private void LogMissingCriticalDependenciesIfNeeded()
        {
            if (worldMapPresenter == null && !loggedMissingWorldMapPresenter)
            {
                ClientLog.Error("WorldTargetActionController could not resolve WorldMapPresenter.");
                loggedMissingWorldMapPresenter = true;
            }

            if (worldLocalPlayerPresenter == null && !loggedMissingLocalPlayerPresenter)
            {
                ClientLog.Error("WorldTargetActionController could not resolve WorldLocalPlayerPresenter.");
                loggedMissingLocalPlayerPresenter = true;
            }
        }

        private bool IsActionRuntimeReady()
        {
            return AreReady(WorldSceneReadyKey.MapVisual, WorldSceneReadyKey.LocalPlayer);
        }

        private void TryBindRuntimeEvents()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            ClientRuntime.Target.CurrentTargetChanged -= HandleCurrentTargetChanged;
            ClientRuntime.Target.CurrentTargetChanged += HandleCurrentTargetChanged;
        }

        private void UnbindRuntimeEvents()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            ClientRuntime.Target.CurrentTargetChanged -= HandleCurrentTargetChanged;
        }

        private void HandleCurrentTargetChanged()
        {
            if (!pendingAction.HasValue)
                return;

            var currentTarget = ClientRuntime.Target.CurrentTarget;
            if (currentTarget.HasValue && currentTarget.Value.Equals(pendingAction.Value.Target))
                return;

            CancelPendingAction(clearPin: true);
        }

        private static bool IsLocalCharacterDead()
        {
            var currentState = ClientRuntime.Character.CurrentState;
            return currentState.HasValue &&
                   (currentState.Value.IsDead ||
                    ClientCharacterRuntimeStateCodes.IsCombatDead(currentState.Value.CurrentState) ||
                    ClientCharacterRuntimeStateCodes.IsPermanentlyDead(currentState.Value.CurrentState));
        }
    }
}


