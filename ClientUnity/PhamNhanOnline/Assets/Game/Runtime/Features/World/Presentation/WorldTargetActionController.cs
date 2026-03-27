using System;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Features.Character.Presentation;
using PhamNhanOnline.Client.Features.Skills.Application;
using PhamNhanOnline.Client.Features.Targeting.Application;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    public sealed class WorldTargetActionController : MonoBehaviour
    {
        private const int BasicSkillSlotIndex = 1;

        private struct PendingTargetAction
        {
            public WorldTargetHandle Target;
            public WorldTargetInteractionMode Mode;
        }

        [Header("References")]
        [SerializeField] private WorldMapPresenter worldMapPresenter;
        [SerializeField] private WorldLocalPlayerPresenter worldLocalPlayerPresenter;
        [SerializeField] private WorldLocalMovementSyncController worldLocalMovementSyncController;

        [Header("Ranges")]
        [SerializeField] private float interactionRangeServerUnits = 30f;
        [SerializeField] private float actionRangeBufferServerUnits = 2f;
        [SerializeField] private float arrivalDeadZoneWorldUnits = 0.05f;

        [Header("Behavior")]
        [SerializeField] private bool pinTargetWhileApproaching = true;
        [SerializeField] private bool logInteractionPlaceholder = true;

        private PendingTargetAction? pendingAction;
        private bool autoPinApplied;

        public event Action<WorldTargetHandle> InteractionRequested;

        private void Awake()
        {
            AutoWireReferences();
        }

        private void Start()
        {
            AutoWireReferences();
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
                if (distanceServerUnits <= requiredRange + Mathf.Max(0f, actionRangeBufferServerUnits))
                {
                    localActionController.ClearExternalMoveOverride();
                    ExecutePendingAction(action);
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
                        CompletePendingAction();
                        return;
                    }

                    if (worldLocalMovementSyncController != null)
                        worldLocalMovementSyncController.TryForceSyncCurrentPosition();

                    ClientRuntime.CombatService.TryUseBasicSkillOnTarget(action.Target);
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

                    break;
            }

            CompletePendingAction();
        }

        private void CompletePendingAction()
        {
            pendingAction = null;
            if (autoPinApplied)
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
            if (action.Mode == WorldTargetInteractionMode.HostileAttack)
            {
                PlayerSkillModel skill;
                if (ClientRuntime.Skills.TryGetLoadoutSkill(BasicSkillSlotIndex, out skill))
                    return Mathf.Max(0f, skill.CastRange);
            }

            return Mathf.Max(0f, interactionRangeServerUnits);
        }

        private bool CanUseBasicSkillNow()
        {
            if (!ClientRuntime.IsInitialized)
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
            if (worldLocalPlayerPresenter == null)
                worldLocalPlayerPresenter = GetComponentInChildren<WorldLocalPlayerPresenter>(true);

            if (worldLocalPlayerPresenter == null || worldLocalPlayerPresenter.CurrentPlayerTransform == null)
                return false;

            var position = worldLocalPlayerPresenter.CurrentPlayerTransform.position;
            worldPosition = new Vector2(position.x, position.y);
            return true;
        }

        private bool TryResolveTargetWorldPosition(WorldTargetHandle target, out Vector2 worldPosition)
        {
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

        private LocalCharacterActionController ResolveLocalActionController()
        {
            if (worldLocalPlayerPresenter == null)
                worldLocalPlayerPresenter = GetComponentInChildren<WorldLocalPlayerPresenter>(true);

            return worldLocalPlayerPresenter != null
                ? worldLocalPlayerPresenter.CurrentLocalActionController
                : null;
        }

        private void AutoWireReferences()
        {
            if (worldMapPresenter == null)
                worldMapPresenter = GetComponentInChildren<WorldMapPresenter>(true);

            if (worldLocalPlayerPresenter == null)
                worldLocalPlayerPresenter = GetComponentInChildren<WorldLocalPlayerPresenter>(true);

            if (worldLocalMovementSyncController == null)
                worldLocalMovementSyncController = GetComponentInChildren<WorldLocalMovementSyncController>(true);
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
    }
}
