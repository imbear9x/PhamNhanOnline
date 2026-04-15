using System;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Features.Character.Application;
using PhamNhanOnline.Client.Features.Character.Presentation;
using PhamNhanOnline.Client.Features.Skills.Application;
using PhamNhanOnline.Client.Features.Targeting.Application;
using PhamNhanOnline.Client.Features.World.Application;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    public sealed partial class WorldTargetActionController
    {
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
            MapPortalModel portal;
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
            MapPortalModel portal;
            if (action.Mode == WorldTargetInteractionMode.ContextOnly &&
                ClientRuntime.World.TryGetPortal(action.Target, out portal))
            {
                return Mathf.Max(0f, portalActionRangeBufferServerUnits);
            }

            return Mathf.Max(0f, actionRangeBufferServerUnits);
        }

        public bool TryGetBasicSkillCastRangeServerUnits(out float castRange)
        {
            castRange = 0f;
            if (!ClientRuntime.IsInitialized)
                return false;

            PlayerSkillModel basicSkill;
            if (!ClientRuntime.Skills.TryGetLoadoutSkill(BasicSkillSlotIndex, out basicSkill))
                return false;

            castRange = Mathf.Max(0f, basicSkill.CastRange);
            return true;
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

            if (LocalFixPortalPresenter.TryResolveActionWorldPosition(target, out worldPosition))
                return true;

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

                    ObservedCharacterModel observedCharacter;
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

                    EnemyRuntimeModel enemy;
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

                    GroundRewardModel reward;
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
            return ClientCharacterRuntimeStateCodes.IsDefeated(currentState);
        }
    }
}
