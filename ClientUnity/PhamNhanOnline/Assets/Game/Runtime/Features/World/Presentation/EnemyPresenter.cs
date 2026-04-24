using GameShared.Models;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Features.Combat.Presentation;
using PhamNhanOnline.Client.Features.Character.Presentation;
using PhamNhanOnline.Client.Features.Targeting.Application;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    [DisallowMultipleComponent]
    public sealed class EnemyPresenter : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private WorldTargetable targetable;
        [SerializeField] private WorldEntityMovementView movementView;
        [SerializeField] private CharacterSkillPresenter skillPresenter;
        [SerializeField] private LocalCharacterActionConfig movementConfig;
        [SerializeField] private bool hideWhenDead;
        [SerializeField] private GroundSnapBindings groundSnapBindings;
        [Header("Grounding")]
        [SerializeField] private bool snapToGround = true;
        [SerializeField] private LayerMask groundLayerMask;
        [SerializeField] private float groundProbeHeight = 3f;
        [SerializeField] private float groundProbeDistance = 12f;
        [SerializeField] private float groundContactOffset = 0f;
        [SerializeField] private bool logGroundingDiagnostics;
        [Header("Movement")]
        [SerializeField] private float movementSnapDistanceWorldUnits = 0.75f;

        private int runtimeId;
        private bool hasResolvedWorldPosition;
        private bool warnedMissingSkillPresenter;
        private bool warnedMissingVisualRoot;
        private bool warnedMissingTargetable;
        private bool warnedMissingMovementView;
        private bool warnedMissingGroundSnapBindings;
        private bool warnedMissingGroundContactAnchor;
        private string enemyCode = string.Empty;

        public int RuntimeId { get { return runtimeId; } }

        public void ConfigureMovementConfig(LocalCharacterActionConfig config)
        {
            if (config != null)
                movementConfig = config;
        }

        public void ApplySnapshot(EnemyRuntimeModel enemy, WorldMapPresenter worldMapPresenter)
        {
            runtimeId = enemy.RuntimeId;
            enemyCode = enemy.Code ?? string.Empty;
            if (!ValidateRequiredReferences())
                return;

            ConfigureTargetable(enemy);
            UpdateWorldPosition(enemy, worldMapPresenter, forceDecisionRefresh: false);
            UpdateLifeState(enemy);
        }

        private void Awake()
        {
            ValidateRequiredReferences();
        }

        private void ConfigureTargetable(EnemyRuntimeModel enemy)
        {
            if (targetable == null)
            {
                LogMissingTargetable();
                return;
            }

            var handle = WorldTargetHandle.CreateEnemy(enemy.RuntimeId, enemy.Kind == 3);
            targetable.Configure(handle);

            if (skillPresenter == null)
            {
                if (!warnedMissingSkillPresenter)
                {
                    ClientLog.Error(
                        $"EnemyPresenter requires CharacterSkillPresenter on enemy prefab '{gameObject.name}'. Assign the reference on the prefab.");
                    warnedMissingSkillPresenter = true;
                }

                return;
            }

            skillPresenter.ConfigureTargetHandle(handle);
        }
        private void UpdateWorldPosition(EnemyRuntimeModel enemy, WorldMapPresenter worldMapPresenter, bool forceDecisionRefresh)
        {
            Vector2 worldPosition;
            var serverPosition = new Vector2(enemy.PosX, enemy.PosY);

            if (worldMapPresenter != null && worldMapPresenter.TryMapServerPositionToWorld(serverPosition, out worldPosition))
            {
                LogGrounding(
                    $"mapped serverPos={serverPosition} to worldPos={worldPosition} " +
                    $"mapReady={worldMapPresenter != null}");
                ApplyWorldPosition(enemy, worldPosition, worldMapPresenter, forceDecisionRefresh);
                hasResolvedWorldPosition = true;
                return;
            }

            LogGrounding(
                $"failed to map serverPos={serverPosition}. " +
                $"worldMapPresenterAssigned={worldMapPresenter != null}");
            if (!hasResolvedWorldPosition)
                return;
        }

        private void ApplyWorldPosition(
            EnemyRuntimeModel enemy,
            Vector2 worldPosition,
            WorldMapPresenter worldMapPresenter,
            bool forceDecisionRefresh)
        {
            var targetPosition = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
            var presentationPosition = ResolveGroundedPresentationPosition(targetPosition);
            ApplyMovementDecision(enemy, presentationPosition, worldMapPresenter, forceDecisionRefresh);
            LogGrounding($"applied finalPos={transform.position}");
        }

        private Vector3 ResolveGroundedPresentationPosition(Vector3 targetPosition)
        {
            if (!snapToGround)
            {
                LogGrounding($"ground snap disabled position={targetPosition}");
                return targetPosition;
            }

            float bottomOffset;
            if (!TryResolveBottomOffset(out bottomOffset))
            {
                LogGrounding($"no bottom offset resolved position={targetPosition}");
                return targetPosition;
            }

            var rayOrigin = new Vector2(
                targetPosition.x,
                targetPosition.y + Mathf.Max(groundProbeHeight, Mathf.Abs(bottomOffset) + 0.25f));
            var rayDistance = Mathf.Max(0.5f, groundProbeHeight + groundProbeDistance + Mathf.Abs(bottomOffset));
            var layerMask = GroundSnapUtility.ResolveGroundLayerMask(groundLayerMask);
            RaycastHit2D hit;
            if (!GroundSnapUtility.TryFindGroundHit(rayOrigin, rayDistance, layerMask, LogGrounding, out hit))
            {
                LogGrounding(
                    $"ray miss origin={rayOrigin} distance={rayDistance} bottomOffset={bottomOffset} " +
                    $"layerMask={layerMask} position={targetPosition}");
                return targetPosition;
            }

            targetPosition.y = hit.point.y - bottomOffset + groundContactOffset;
            LogGrounding(
                $"ray hit collider={hit.collider.name} point={hit.point} normal={hit.normal} " +
                $"bottomOffset={bottomOffset} contactOffset={groundContactOffset} position={targetPosition}");
            return targetPosition;
        }

        private void ApplyMovementDecision(
            EnemyRuntimeModel enemy,
            Vector3 authoritativeWorldPosition,
            WorldMapPresenter worldMapPresenter,
            bool forceDecisionRefresh)
        {
            if (movementView == null)
            {
                LogMissingMovementView();
                return;
            }

            var isDuplicateMoveDecision =
                enemy.MovementMode == 1 &&
                enemy.MovementSpeed > 0f &&
                !forceDecisionRefresh &&
                movementView.IsCurrentMoveDecision(enemy.MovementDecisionVersion);
            var distanceToAuthoritative = Vector3.Distance(transform.position, authoritativeWorldPosition);
            if (!isDuplicateMoveDecision &&
                (!hasResolvedWorldPosition || distanceToAuthoritative >= movementSnapDistanceWorldUnits))
            {
                movementView.SnapTo(authoritativeWorldPosition);
            }

            if (enemy.MovementMode != 1 || enemy.MovementSpeed <= 0f)
            {
                movementView.StopAt(authoritativeWorldPosition);
                return;
            }

            Vector2 targetWorldPosition2D;
            var targetServerPosition = new Vector2(enemy.MovementTargetPosX, enemy.MovementTargetPosY);
            if (worldMapPresenter == null || !worldMapPresenter.TryMapServerPositionToWorld(targetServerPosition, out targetWorldPosition2D))
            {
                movementView.StopAt(authoritativeWorldPosition);
                return;
            }

            var serverDistance = Vector2.Distance(
                new Vector2(enemy.PosX, enemy.PosY),
                targetServerPosition);
            if (serverDistance <= 0.001f)
            {
                movementView.StopAt(authoritativeWorldPosition);
                return;
            }

            var targetWorldPosition = ResolveGroundedPresentationPosition(
                new Vector3(targetWorldPosition2D.x, targetWorldPosition2D.y, transform.position.z));
            var movementDurationSeconds = ResolveMovementDurationSeconds(
                authoritativeWorldPosition,
                targetWorldPosition,
                serverDistance,
                enemy.MovementSpeed);
            movementView.ApplyMoveDecision(
                enemy.MovementDecisionVersion,
                authoritativeWorldPosition,
                targetWorldPosition,
                movementDurationSeconds,
                movementSnapDistanceWorldUnits,
                forceDecisionRefresh);
        }

        private float ResolveMovementDurationSeconds(
            Vector3 authoritativeWorldPosition,
            Vector3 targetWorldPosition,
            float serverDistance,
            float serverMoveSpeed)
        {
            if (serverMoveSpeed <= 0f)
                return 0f;

            if (movementConfig == null)
                return serverDistance / serverMoveSpeed;

            var worldMoveSpeed = movementConfig.ConvertServerUnitsToWorldUnits(serverMoveSpeed);
            if (worldMoveSpeed <= 0f)
                return serverDistance / serverMoveSpeed;

            var worldDistance = Vector3.Distance(authoritativeWorldPosition, targetWorldPosition);
            return worldDistance / worldMoveSpeed;
        }

        private bool TryResolveBottomOffset(out float bottomOffset)
        {
            bottomOffset = 0f;

            var groundContactAnchor = ResolveGroundContactAnchor();
            if (groundContactAnchor != null)
            {
                bottomOffset = groundContactAnchor.position.y - transform.position.y;
                return true;
            }

            return false;
        }

        private Transform ResolveGroundContactAnchor()
        {
            if (groundSnapBindings == null)
            {
                if (snapToGround && !warnedMissingGroundSnapBindings)
                {
                    ClientLog.Error($"EnemyPresenter on '{gameObject.name}' requires GroundSnapBindings because snapToGround is enabled.");
                    warnedMissingGroundSnapBindings = true;
                }

                return null;
            }

            if (groundSnapBindings.GroundContactAnchor != null)
                return groundSnapBindings.GroundContactAnchor;

            if (snapToGround && !warnedMissingGroundContactAnchor)
            {
                ClientLog.Error($"EnemyPresenter on '{gameObject.name}' requires GroundSnapBindings.GroundContactAnchor.");
                warnedMissingGroundContactAnchor = true;
            }

            return null;
        }

        private bool ValidateRequiredReferences()
        {
            var valid = true;
            if (visualRoot == null)
            {
                if (!warnedMissingVisualRoot)
                {
                    ClientLog.Error($"EnemyPresenter on '{gameObject.name}' requires Visual Root reference.");
                    warnedMissingVisualRoot = true;
                }

                valid = false;
            }

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
                        $"EnemyPresenter requires CharacterSkillPresenter on enemy prefab '{gameObject.name}'. Add the component to the enemy prefab and assign the reference.");
                    warnedMissingSkillPresenter = true;
                }

                valid = false;
            }

            if (snapToGround && groundSnapBindings == null)
            {
                if (!warnedMissingGroundSnapBindings)
                {
                    ClientLog.Error($"EnemyPresenter on '{gameObject.name}' requires GroundSnapBindings because snapToGround is enabled.");
                    warnedMissingGroundSnapBindings = true;
                }

                valid = false;
            }

            return valid;
        }

        private void LogMissingTargetable()
        {
            if (warnedMissingTargetable)
                return;

            ClientLog.Error($"EnemyPresenter on '{gameObject.name}' requires WorldTargetable reference.");
            warnedMissingTargetable = true;
        }

        private void LogMissingMovementView()
        {
            if (warnedMissingMovementView)
                return;

            ClientLog.Error($"EnemyPresenter on '{gameObject.name}' requires WorldEntityMovementView reference.");
            warnedMissingMovementView = true;
        }

        private void LogGrounding(string message)
        {
            if (!logGroundingDiagnostics)
                return;

            ClientLog.Info(
                $"[EnemyGrounding] name={name} code={enemyCode} runtimeId={runtimeId} " +
                $"snapPoint={(ResolveGroundContactAnchor() != null ? ResolveGroundContactAnchor().name : "null")} {message}");
        }

        private void UpdateLifeState(EnemyRuntimeModel enemy)
        {
            var isAlive = enemy.CurrentHp > 0 && enemy.RuntimeState != 4;
            if (targetable != null && targetable.enabled != isAlive)
                targetable.enabled = isAlive;

            if (visualRoot != null)
                visualRoot.gameObject.SetActive(hasResolvedWorldPosition && (!hideWhenDead || isAlive));
        }
    }
}
