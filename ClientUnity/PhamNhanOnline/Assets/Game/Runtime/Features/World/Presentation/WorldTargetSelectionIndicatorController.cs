using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Features.Targeting.Application;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    public sealed class WorldTargetSelectionIndicatorController : WorldSceneBehaviour
    {
        [Header("References")]
        [SerializeField] private WorldMapPresenter worldMapPresenter;
        [SerializeField] private Transform whiteIndicator;
        [SerializeField] private Transform redIndicator;

        [Header("Placement")]
        [SerializeField] private float indicatorHeightOffset = 0.25f;
        [SerializeField] private float fallbackWorldHeightOffset = 1.25f;
        private bool runtimeEventsBound;
        private WorldTargetHandle? trackedTarget;
        private WorldTargetable trackedTargetable;
        private WorldTargetInteractionMode trackedInteractionMode = WorldTargetInteractionMode.None;

        private void Start()
        {
            AutoWireReferences();
            LogMissingCriticalWorldSceneDependenciesIfNeeded();
            TryBindRuntimeEvents();
            RefreshTrackedTarget();
        }

        private void OnEnable()
        {
            AutoWireReferences();
            TryBindRuntimeEvents();
            RefreshTrackedTarget();
        }

        private void OnDisable()
        {
            UnbindRuntimeEvents();
            ClearTracking();
        }

        private void OnDestroy()
        {
            UnbindRuntimeEvents();
        }

        private void LateUpdate()
        {
            if (!ClientRuntime.IsInitialized || !trackedTarget.HasValue || !trackedTarget.Value.IsValid)
            {
                SetIndicatorsVisible(false, false);
                return;
            }

            if (!IsIndicatorRuntimeReady())
            {
                SetIndicatorsVisible(false, false);
                return;
            }

            Vector2 worldPosition;
            if (!TryResolveIndicatorWorldPosition(trackedTarget.Value, out worldPosition))
            {
                SetIndicatorsVisible(false, false);
                return;
            }

            var showRed = trackedInteractionMode == WorldTargetInteractionMode.HostileAttack;
            var showWhite = trackedInteractionMode == WorldTargetInteractionMode.ContextOnly &&
                            !IsPortalTarget(trackedTarget.Value);
            SetIndicatorsVisible(showWhite, showRed);
            ApplyPosition(whiteIndicator, worldPosition);
            ApplyPosition(redIndicator, worldPosition);
        }

        private bool TryResolveIndicatorWorldPosition(WorldTargetHandle handle, out Vector2 worldPosition)
        {
            if (trackedTargetable != null &&
                trackedTargetable.isActiveAndEnabled &&
                trackedTargetable.Handle.Equals(handle) &&
                trackedTargetable.TryGetIndicatorAnchorPosition(indicatorHeightOffset, out worldPosition))
            {
                return true;
            }

            if (TryResolveTrackedTargetable(handle) &&
                trackedTargetable != null &&
                trackedTargetable.isActiveAndEnabled &&
                trackedTargetable.TryGetIndicatorAnchorPosition(indicatorHeightOffset, out worldPosition))
            {
                return true;
            }

            WorldTargetSnapshot snapshot;
            if (!ClientRuntime.World.TryBuildTargetSnapshot(handle, out snapshot))
            {
                worldPosition = default;
                return false;
            }

            switch (handle.Kind)
            {
                case WorldTargetKind.Player:
                    return TryResolveObservedCharacterPosition(handle.TargetId, out worldPosition);
                case WorldTargetKind.Enemy:
                case WorldTargetKind.Boss:
                    return TryResolveEnemyPosition(handle.TargetId, out worldPosition);
                default:
                    worldPosition = default;
                    return false;
            }
        }

        private bool TryResolveObservedCharacterPosition(string targetId, out Vector2 worldPosition)
        {
            worldPosition = default;

            System.Guid characterId;
            if (!System.Guid.TryParse(targetId, out characterId))
                return false;

            GameShared.Models.ObservedCharacterModel observedCharacter;
            if (!ClientRuntime.World.TryGetObservedCharacter(characterId, out observedCharacter))
                return false;

            return TryMapServerPositionToWorld(
                new Vector2(observedCharacter.CurrentState.CurrentPosX, observedCharacter.CurrentState.CurrentPosY),
                out worldPosition);
        }

        private bool TryResolveEnemyPosition(string targetId, out Vector2 worldPosition)
        {
            worldPosition = default;

            int runtimeId;
            if (!int.TryParse(targetId, out runtimeId))
                return false;

            GameShared.Models.EnemyRuntimeModel enemy;
            if (!ClientRuntime.World.TryGetEnemy(runtimeId, out enemy))
                return false;

            return TryMapServerPositionToWorld(
                new Vector2(enemy.PosX, enemy.PosY),
                out worldPosition);
        }

        private bool TryMapServerPositionToWorld(Vector2 serverPosition, out Vector2 worldPosition)
        {
            worldPosition = default;
            if (worldMapPresenter == null)
                return false;

            if (!worldMapPresenter.TryMapServerPositionToWorld(serverPosition, out worldPosition))
                return false;

            worldPosition += new Vector2(0f, fallbackWorldHeightOffset);
            return true;
        }

        private void SetIndicatorsVisible(bool showWhite, bool showRed)
        {
            if (whiteIndicator != null && whiteIndicator.gameObject.activeSelf != showWhite)
                whiteIndicator.gameObject.SetActive(showWhite);

            if (redIndicator != null && redIndicator.gameObject.activeSelf != showRed)
                redIndicator.gameObject.SetActive(showRed);
        }

        private static void ApplyPosition(Transform indicator, Vector2 worldPosition)
        {
            if (indicator == null || !indicator.gameObject.activeSelf)
                return;

            var currentPosition = indicator.position;
            indicator.position = new Vector3(worldPosition.x, worldPosition.y, currentPosition.z);
        }

        private void TryBindRuntimeEvents()
        {
            if (runtimeEventsBound || !ClientRuntime.IsInitialized)
                return;

            ClientRuntime.Target.CurrentTargetChanged += HandleCurrentTargetChanged;
            runtimeEventsBound = true;
        }

        private void UnbindRuntimeEvents()
        {
            if (!runtimeEventsBound || !ClientRuntime.IsInitialized)
                return;

            ClientRuntime.Target.CurrentTargetChanged -= HandleCurrentTargetChanged;
            runtimeEventsBound = false;
        }

        private void HandleCurrentTargetChanged()
        {
            RefreshTrackedTarget();
        }

        private void RefreshTrackedTarget()
        {
            if (!ClientRuntime.IsInitialized)
            {
                ClearTracking();
                return;
            }

            var currentTarget = ClientRuntime.Target.CurrentTarget;
            if (!currentTarget.HasValue || !currentTarget.Value.IsValid)
            {
                ClearTracking();
                return;
            }

            trackedTarget = currentTarget.Value;
            trackedInteractionMode = WorldTargetInteractionRules.Resolve(currentTarget.Value);
            TryResolveTrackedTargetable(currentTarget.Value);
        }

        private bool TryResolveTrackedTargetable(WorldTargetHandle handle)
        {
            WorldTargetable targetable;
            if (WorldTargetableRegistry.TryGet(handle, out targetable) &&
                targetable != null &&
                targetable.isActiveAndEnabled)
            {
                trackedTargetable = targetable;
                return true;
            }

            trackedTargetable = null;
            return false;
        }

        private void ClearTracking()
        {
            trackedTarget = null;
            trackedTargetable = null;
            trackedInteractionMode = WorldTargetInteractionMode.None;
            SetIndicatorsVisible(false, false);
        }

        private void AutoWireReferences()
        {
            InitializeWorldSceneBehaviour(ref worldMapPresenter);
        }

        private bool IsIndicatorRuntimeReady()
        {
            return IsReady(WorldSceneReadyKey.MapVisual);
        }

        private static bool IsPortalTarget(WorldTargetHandle handle)
        {
            if (handle.Kind == WorldTargetKind.Npc &&
                !string.IsNullOrWhiteSpace(handle.TargetId) &&
                handle.TargetId.StartsWith("local-home-", System.StringComparison.Ordinal) &&
                handle.TargetId.EndsWith("-portal", System.StringComparison.Ordinal))
            {
                return true;
            }

            GameShared.Models.MapPortalModel _;
            return ClientRuntime.IsInitialized && ClientRuntime.World.TryGetPortal(handle, out _);
        }
    }
}

