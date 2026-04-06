using GameShared.Models;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Features.Combat.Presentation;
using PhamNhanOnline.Client.Features.Targeting.Application;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    [DisallowMultipleComponent]
    public sealed class EnemyPresenter : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private WorldTargetable targetable;
        [SerializeField] private bool hideWhenDead;
        [SerializeField] private GroundSnapBindings groundSnapBindings;
        [Header("Grounding")]
        [SerializeField] private bool snapToGround = true;
        [SerializeField] private LayerMask groundLayerMask;
        [SerializeField] private float groundProbeHeight = 3f;
        [SerializeField] private float groundProbeDistance = 12f;
        [SerializeField] private float groundContactOffset = 0f;
        [SerializeField] private bool logGroundingDiagnostics;

        private int runtimeId;
        private bool hasResolvedWorldPosition;
        private CharacterSkillPresenter skillPresenter;
        private bool warnedMissingSkillPresenter;
        private string enemyCode = string.Empty;

        public int RuntimeId { get { return runtimeId; } }

        public void ApplySnapshot(EnemyRuntimeModel enemy, WorldMapPresenter worldMapPresenter)
        {
            runtimeId = enemy.RuntimeId;
            enemyCode = enemy.Code ?? string.Empty;
            AutoWireReferences();
            ConfigureTargetable(enemy);
            UpdateWorldPosition(enemy, worldMapPresenter);
            UpdateLifeState(enemy);
        }

        private void Awake()
        {
            AutoWireReferences();
        }

        private void AutoWireReferences()
        {
            if (visualRoot == null)
                visualRoot = transform;

            if (targetable == null)
                targetable = GetComponent<WorldTargetable>();

            if (skillPresenter == null)
                skillPresenter = GetComponent<CharacterSkillPresenter>();

            if (groundSnapBindings == null)
                groundSnapBindings = GetComponentInChildren<GroundSnapBindings>(true);
        }

        private void ConfigureTargetable(EnemyRuntimeModel enemy)
        {
            if (targetable == null)
                targetable = gameObject.AddComponent<WorldTargetable>();

            var handle = WorldTargetHandle.CreateEnemy(enemy.RuntimeId, enemy.Kind == 3);
            targetable.Configure(handle);

            if (skillPresenter == null)
            {
                if (!warnedMissingSkillPresenter)
                {
                    ClientLog.Error(
                        $"EnemyPresenter requires CharacterSkillPresenter on enemy prefab '{gameObject.name}'. Add the component to the enemy prefab instead of relying on runtime AddComponent.");
                    warnedMissingSkillPresenter = true;
                }

                return;
            }

            skillPresenter.ConfigureTargetHandle(handle);
        }
        private void UpdateWorldPosition(EnemyRuntimeModel enemy, WorldMapPresenter worldMapPresenter)
        {
            Vector2 worldPosition;
            var serverPosition = new Vector2(enemy.PosX, enemy.PosY);

            if (worldMapPresenter != null && worldMapPresenter.TryMapServerPositionToWorld(serverPosition, out worldPosition))
            {
                LogGrounding(
                    $"mapped serverPos={serverPosition} to worldPos={worldPosition} " +
                    $"mapReady={worldMapPresenter != null}");
                ApplyWorldPosition(worldPosition);
                hasResolvedWorldPosition = true;
                return;
            }

            LogGrounding(
                $"failed to map serverPos={serverPosition}. " +
                $"worldMapPresenterAssigned={worldMapPresenter != null}");
            if (!hasResolvedWorldPosition)
                return;
        }

        private void ApplyWorldPosition(Vector2 worldPosition)
        {
            var targetPosition = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
            if (!snapToGround)
            {
                transform.position = targetPosition;
                LogGrounding($"applied without snap finalPos={transform.position}");
                return;
            }

            float bottomOffset;
            if (!TryResolveBottomOffset(out bottomOffset))
            {
                transform.position = targetPosition;
                LogGrounding($"no bottom offset resolved. finalPos={transform.position}");
                return;
            }

            var rayOrigin = new Vector2(
                targetPosition.x,
                targetPosition.y + Mathf.Max(groundProbeHeight, Mathf.Abs(bottomOffset) + 0.25f));
            var rayDistance = Mathf.Max(0.5f, groundProbeHeight + groundProbeDistance + Mathf.Abs(bottomOffset));
            var layerMask = GroundSnapUtility.ResolveGroundLayerMask(groundLayerMask);
            RaycastHit2D hit;
            if (!GroundSnapUtility.TryFindGroundHit(rayOrigin, rayDistance, layerMask, LogGrounding, out hit))
            {
                transform.position = targetPosition;
                LogGrounding(
                    $"ray miss origin={rayOrigin} distance={rayDistance} bottomOffset={bottomOffset} " +
                    $"layerMask={layerMask} finalPos={transform.position}");
                return;
            }

            targetPosition.y = hit.point.y - bottomOffset + groundContactOffset;
            transform.position = targetPosition;
            LogGrounding(
                $"ray hit collider={hit.collider.name} point={hit.point} normal={hit.normal} " +
                $"bottomOffset={bottomOffset} contactOffset={groundContactOffset} finalPos={transform.position}");
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

            Bounds bounds;
            if (TryGetLocalPresentationBounds(out bounds))
            {
                bottomOffset = bounds.min.y - transform.position.y;
                return true;
            }

            return false;
        }

        private Transform ResolveGroundContactAnchor()
        {
            if (groundSnapBindings != null && groundSnapBindings.GroundContactAnchor != null)
                return groundSnapBindings.GroundContactAnchor;

            return transform.Find("GroundContactAnchor");
        }

        private bool TryGetLocalPresentationBounds(out Bounds bounds)
        {
            var colliders = GetComponentsInChildren<Collider2D>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                var collider = colliders[i];
                if (collider == null || !collider.enabled)
                    continue;

                bounds = collider.bounds;
                return true;
            }

            var renderers = GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                    continue;

                bounds = renderer.bounds;
                return true;
            }

            bounds = default;
            return false;
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
