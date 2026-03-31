using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Features.Character.Presentation;
using PhamNhanOnline.Client.Features.Targeting.Application;
using PhamNhanOnline.Client.Features.World.Application;
using TMPro;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    public sealed class WorldPortalPresenter : MonoBehaviour
    {
        private const int TouchInteractionMode = 1;
        private const int InteractInteractionMode = 2;
        private const string DefaultPortalVisualResourcePath = "World/Portals/PortalVisual_Default";
        private sealed class PortalRuntime
        {
            public MapPortalModel Portal;
            public GameObject RootObject;
            public PortalVisualInstance VisualInstance;
            public Collider2D TriggerCollider;
            public bool WasTouchingLastFrame;
        }

        [Header("References")]
        [SerializeField] private WorldSceneController worldSceneController;
        [SerializeField] private WorldMapPresenter worldMapPresenter;
        [SerializeField] private WorldSceneReadinessService readinessService;
        [SerializeField] private WorldTargetActionController worldTargetActionController;
        [SerializeField] private WorldLocalMovementSyncController worldLocalMovementSyncController;
        [SerializeField] private Transform portalRoot;
        [SerializeField] private GameObject portalVisualPrefab;

        [Header("Visuals")]
        [SerializeField] private int labelSortingOrder = 25;
        [SerializeField] private float edgePortalThresholdNormalized = 0.15f;
        [SerializeField] private Vector2 labelTargetPaddingWorldUnits = new Vector2(0.4f, 0.25f);
        [SerializeField] private Vector2 minLabelTargetSizeWorldUnits = new Vector2(2.2f, 0.9f);

        [Header("Behavior")]
        [SerializeField] private bool onlyShowEnabledPortals = true;
        [SerializeField] private float touchPortalRearmDelaySeconds = 0.75f;
        [SerializeField] private float rebuildRetryIntervalSeconds = 0.35f;
        [SerializeField] private float touchPortalHorizontalIntentDeadZone = 0.05f;

        private readonly Dictionary<int, PortalRuntime> spawnedPortals = new Dictionary<int, PortalRuntime>();
        private bool runtimeEventsBound;
        private bool usePortalInFlight;
        private float touchPortalSuppressedUntilTime;
        private bool rebuildRetryPending;
        private float nextRebuildRetryTime;
        private GameObject resolvedPortalVisualPrefab;

        private void Awake()
        {
            AutoWireReferences();
        }

        private void Start()
        {
            AutoWireReferences();
            TryBindRuntimeEvents();
            TryRebuildPortalsIfReady();
        }

        private void OnEnable()
        {
            AutoWireReferences();
            TryBindRuntimeEvents();
            TryRebuildPortalsIfReady();
        }

        private void Update()
        {
            TryRetryDeferredRebuild();
            PollTouchPortals();
        }

        private void OnDisable()
        {
            UnbindRuntimeEvents();
            ClearPortals();
        }

        private void OnDestroy()
        {
            UnbindRuntimeEvents();
            ClearPortals();
        }

        private void HandleMapVisualReady()
        {
            RebuildPortals();
        }

        private void HandleCurrentTargetChanged()
        {
            RefreshPortalSelectionVisuals();
        }

        private void HandleInteractionRequested(WorldTargetHandle handle)
        {
            MapPortalModel portal;
            if (!ClientRuntime.IsInitialized || !ClientRuntime.World.TryGetPortal(handle, out portal))
                return;

            _ = UsePortalAsync(portal);
        }

        private async Task UsePortalAsync(MapPortalModel portal)
        {
            if (usePortalInFlight)
                return;

            usePortalInFlight = true;
            try
            {
                if (worldLocalMovementSyncController != null)
                    worldLocalMovementSyncController.TryForceSyncCurrentPosition();

                var result = await ClientRuntime.WorldTravelService.UsePortalAsync(portal.Id);
                if (result.Success)
                {
                    ClientLog.Info(
                        string.Format(
                            "Portal {0} used successfully. TargetMap={1}, TargetSpawnPoint={2}.",
                            portal.Id,
                            result.TargetMapId,
                            result.TargetSpawnPointId));
                    return;
                }

                ClientLog.Warn(
                    string.Format(
                        "Portal {0} use failed: {1} ({2}).",
                        portal.Id,
                        result.Code,
                        result.Message));
            }
            catch (Exception ex)
            {
                ClientLog.Warn(string.Format("Portal {0} use exception: {1}", portal.Id, ex.Message));
            }
            finally
            {
                usePortalInFlight = false;
            }
        }

        private void PollTouchPortals()
        {
            if (!ClientRuntime.IsInitialized || spawnedPortals.Count == 0)
                return;

            var allowTouchTravel = !usePortalInFlight &&
                                   Time.unscaledTime >= touchPortalSuppressedUntilTime;
            foreach (var runtime in spawnedPortals.Values)
            {
                if (runtime == null || runtime.TriggerCollider == null)
                    continue;

                var isTouchPortal = IsTouchPortal(runtime.Portal);
                var isColliderOverlap = IsPortalTouchingLocalPlayer(runtime);
                var hasPortalEntryIntent = HasPortalEntryIntent(runtime.Portal);
                var isTouching = isTouchPortal &&
                                 runtime.Portal.IsEnabled &&
                                 isColliderOverlap &&
                                 hasPortalEntryIntent;

                if (allowTouchTravel && isTouching && !runtime.WasTouchingLastFrame)
                {
                    _ = UsePortalAsync(runtime.Portal);
                }

                runtime.WasTouchingLastFrame = isTouching;
            }
        }

        private bool HasPortalEntryIntent(MapPortalModel portal)
        {
            var side = ResolveTouchTriggerSide(portal);
            if (side == TouchTriggerSide.None)
                return true;

            var horizontalIntent = ResolveHorizontalMoveIntent();
            var deadZone = Mathf.Max(0f, touchPortalHorizontalIntentDeadZone);
            switch (side)
            {
                case TouchTriggerSide.Left:
                    return horizontalIntent < -deadZone;

                case TouchTriggerSide.Right:
                    return horizontalIntent > deadZone;

                default:
                    return true;
            }
        }

        private float ResolveHorizontalMoveIntent()
        {
            WorldLocalPlayerPresenter localPlayerPresenter = null;
            if (worldSceneController != null)
                localPlayerPresenter = worldSceneController.WorldLocalPlayerPresenter;
            if (localPlayerPresenter == null)
                localPlayerPresenter = GetComponent<WorldLocalPlayerPresenter>();

            var localActionController = localPlayerPresenter != null
                ? localPlayerPresenter.CurrentLocalActionController
                : null;
            if (localActionController != null)
            {
                var input = localActionController.CurrentHorizontalMoveInput;
                if (Mathf.Abs(input) > Mathf.Epsilon)
                    return input;
            }

            var playerTransform = localPlayerPresenter != null
                ? localPlayerPresenter.CurrentPlayerTransform
                : null;
            if (playerTransform != null)
            {
                var playerView = playerTransform.GetComponent<PlayerView>();
                if (playerView != null && playerView.Body != null)
                {
                    var velocityX = playerView.Body.velocity.x;
                    if (Mathf.Abs(velocityX) > Mathf.Epsilon)
                        return velocityX;
                }
            }

            return 0f;
        }

        private bool IsPortalTouchingLocalPlayer(PortalRuntime runtime)
        {
            if (runtime == null || runtime.TriggerCollider == null || !runtime.TriggerCollider.enabled)
                return false;

            Collider2D playerCollider;
            if (TryResolveLocalPlayerCollider(out playerCollider) && playerCollider != null && playerCollider.enabled)
            {
                return runtime.TriggerCollider.Distance(playerCollider).isOverlapped;
            }

            if (!TryResolveCurrentLocalPlayerServerPosition(out var playerServerPosition))
                return false;

            var portalServerPosition = ResolvePortalServerPosition(runtime.Portal);
            return Vector2.Distance(playerServerPosition, portalServerPosition) <= Mathf.Max(0f, runtime.Portal.InteractionRadius);
        }

        private void RebuildPortals()
        {
            ClearPortals();
            rebuildRetryPending = false;

            if (!ClientRuntime.IsInitialized || worldMapPresenter == null)
                return;

            var parent = ResolvePortalRoot();
            if (parent == null)
                return;

            foreach (var portal in ClientRuntime.World.CurrentPortals)
            {
                if (onlyShowEnabledPortals && !portal.IsEnabled)
                    continue;

                var portalServerPosition = ResolvePortalServerPosition(portal);
                Vector2 worldPosition;
                if (!worldMapPresenter.TryMapServerPositionToWorld(portalServerPosition, out worldPosition))
                {
                    ScheduleDeferredRebuild();
                    continue;
                }

                var portalRuntime = BuildPortalObject(parent, portal, worldPosition);
                spawnedPortals[portal.Id] = portalRuntime;
            }

            RefreshPortalSelectionVisuals();
            touchPortalSuppressedUntilTime = Time.unscaledTime + Mathf.Max(0f, touchPortalRearmDelaySeconds);
        }

        private void TryRetryDeferredRebuild()
        {
            if (!rebuildRetryPending || !ClientRuntime.IsInitialized || Time.unscaledTime < nextRebuildRetryTime)
                return;

            if (worldMapPresenter == null || worldMapPresenter.CurrentMapTransform == null)
            {
                nextRebuildRetryTime = Time.unscaledTime + Mathf.Max(0.1f, rebuildRetryIntervalSeconds);
                return;
            }

            Bounds _;
            if (!worldMapPresenter.TryGetPlayableBounds(out _))
            {
                nextRebuildRetryTime = Time.unscaledTime + Mathf.Max(0.1f, rebuildRetryIntervalSeconds);
                return;
            }

            RebuildPortals();
        }

        private void ScheduleDeferredRebuild()
        {
            rebuildRetryPending = true;
            nextRebuildRetryTime = Time.unscaledTime + Mathf.Max(0.1f, rebuildRetryIntervalSeconds);
        }

        private void TryRebuildPortalsIfReady()
        {
            if (readinessService != null && !readinessService.IsReady(WorldSceneReadyKey.MapVisual))
                return;

            RebuildPortals();
        }

        private PortalRuntime BuildPortalObject(Transform parent, MapPortalModel portal, Vector2 worldPosition)
        {
            var portalObject = CreatePortalVisualRoot(parent, portal, worldPosition);

            var visualInstance = portalObject.GetComponent<PortalVisualInstance>();
            if (visualInstance != null)
                visualInstance.Apply(ResolvePortalLabel(portal));

            var labelObject = visualInstance != null
                ? visualInstance.LabelObject
                : CreateFallbackLabelChild(portalObject.transform, portal);
            ConfigureInteractiveLabel(labelObject, portal);
            var collider = ConfigureTouchTrigger(visualInstance, portal);

            return new PortalRuntime
            {
                Portal = portal,
                RootObject = portalObject,
                VisualInstance = visualInstance,
                TriggerCollider = collider,
                WasTouchingLastFrame = false
            };
        }

        private GameObject CreatePortalVisualRoot(Transform parent, MapPortalModel portal, Vector2 worldPosition)
        {
            var prefab = ResolvePortalVisualPrefab();
            GameObject portalObject;
            if (prefab != null)
            {
                portalObject = Instantiate(prefab, parent, false);
            }
            else
            {
                portalObject = new GameObject("PortalVisual");
                portalObject.transform.SetParent(parent, false);
                portalObject.AddComponent<PortalVisualInstance>();
            }

            portalObject.name = string.IsNullOrWhiteSpace(portal.Name) ? "Portal_" + portal.Id : portal.Name;
            portalObject.transform.position = new Vector3(worldPosition.x, worldPosition.y, 0f);
            return portalObject;
        }

        private GameObject ResolvePortalVisualPrefab()
        {
            if (portalVisualPrefab != null)
                return portalVisualPrefab;

            if (resolvedPortalVisualPrefab == null)
                resolvedPortalVisualPrefab = Resources.Load<GameObject>(DefaultPortalVisualResourcePath);

            return resolvedPortalVisualPrefab;
        }

        private GameObject CreateFallbackLabelChild(Transform parent, MapPortalModel portal)
        {
            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            labelObject.transform.localScale = Vector3.one * 0.12f;

            var text = labelObject.AddComponent<TextMeshPro>();
            text.text = ResolvePortalLabel(portal);
            text.fontSize = 5f;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            text.color = Color.white;
            text.outlineWidth = 0.18f;
            text.raycastTarget = false;
            text.ForceMeshUpdate();

            var renderer = labelObject.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sortingOrder = labelSortingOrder;

            return labelObject;
        }

        private void ConfigureInteractiveLabel(GameObject labelObject, MapPortalModel portal)
        {
            if (labelObject == null)
                return;

            var text = labelObject.GetComponentInChildren<TextMeshPro>(true);
            if (text == null)
                return;

            text.ForceMeshUpdate();
            var labelCollider = labelObject.GetComponent<Collider2D>();
            if (labelCollider == null)
            {
                labelCollider = labelObject.GetComponentInChildren<Collider2D>(true);
                if (labelCollider != null)
                    labelObject = labelCollider.gameObject;
            }

            if (labelCollider == null)
            {
                ClientLog.Warn($"Portal '{portal.Id}' is missing an interaction collider on its label prefab object.");
                return;
            }

            labelCollider.isTrigger = true;

            Bounds rendererBounds;
            if (TryResolveLabelBounds(labelObject.transform, out rendererBounds))
            {
                var localCenter = labelObject.transform.InverseTransformPoint(rendererBounds.center);
                var lossyScale = labelObject.transform.lossyScale;
                var safeScaleX = Mathf.Approximately(lossyScale.x, 0f) ? 1f : Mathf.Abs(lossyScale.x);
                var safeScaleY = Mathf.Approximately(lossyScale.y, 0f) ? 1f : Mathf.Abs(lossyScale.y);
                if (labelCollider is BoxCollider2D labelBoxCollider)
                {
                    labelBoxCollider.offset = new Vector2(localCenter.x, localCenter.y);
                    labelBoxCollider.size = new Vector2(
                        Mathf.Max(minLabelTargetSizeWorldUnits.x, (rendererBounds.size.x / safeScaleX) + (labelTargetPaddingWorldUnits.x * 2f)),
                        Mathf.Max(minLabelTargetSizeWorldUnits.y, (rendererBounds.size.y / safeScaleY) + (labelTargetPaddingWorldUnits.y * 2f)));
                }
            }
            else
            {
                var textBounds = text.textBounds;
                if (labelCollider is BoxCollider2D labelBoxCollider)
                {
                    labelBoxCollider.offset = new Vector2(textBounds.center.x, textBounds.center.y);
                    labelBoxCollider.size = new Vector2(
                        Mathf.Max(minLabelTargetSizeWorldUnits.x, textBounds.size.x + (labelTargetPaddingWorldUnits.x * 2f)),
                        Mathf.Max(minLabelTargetSizeWorldUnits.y, textBounds.size.y + (labelTargetPaddingWorldUnits.y * 2f)));
                }
            }

            var targetable = labelObject.GetComponent<WorldTargetable>();
            if (targetable == null)
                targetable = labelObject.AddComponent<WorldTargetable>();
            targetable.Configure(new WorldTargetHandle(
                WorldTargetKind.Npc,
                ClientWorldState.BuildPortalTargetId(portal.Id)));

            SetLayerRecursively(labelObject, ResolveTargetableLayer());
        }

        private Collider2D ConfigureTouchTrigger(PortalVisualInstance visualInstance, MapPortalModel portal)
        {
            if (visualInstance == null)
                return null;

            var leftCollider = visualInstance.TouchTriggerLeftCollider;
            var rightCollider = visualInstance.TouchTriggerRightCollider;
            SetTouchTriggerEnabled(leftCollider, false);
            SetTouchTriggerEnabled(rightCollider, false);

            if (!IsTouchPortal(portal))
                return null;

            switch (ResolveTouchTriggerSide(portal))
            {
                case TouchTriggerSide.Left:
                    SetTouchTriggerEnabled(leftCollider, true);
                    return leftCollider;

                case TouchTriggerSide.Right:
                    SetTouchTriggerEnabled(rightCollider, true);
                    return rightCollider;

                default:
                    return null;
            }
        }

        private static bool TryResolveLabelBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            if (root == null)
                return false;

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var hasBounds = false;
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                    continue;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(renderer.bounds);
            }

            return hasBounds;
        }

        private Transform ResolvePortalRoot()
        {
            if (portalRoot != null)
                return portalRoot;

            if (worldSceneController != null && worldSceneController.MapRoot != null)
                return worldSceneController.MapRoot;

            return transform;
        }

        private bool TryResolveLocalPlayerCollider(out Collider2D playerCollider)
        {
            playerCollider = null;

            WorldLocalPlayerPresenter localPlayerPresenter = null;
            if (worldSceneController != null)
                localPlayerPresenter = worldSceneController.WorldLocalPlayerPresenter;
            if (localPlayerPresenter == null)
                localPlayerPresenter = GetComponent<WorldLocalPlayerPresenter>();
            if (localPlayerPresenter == null || localPlayerPresenter.CurrentPlayerTransform == null)
                return false;

            var playerTransform = localPlayerPresenter.CurrentPlayerTransform;
            var playerView = playerTransform.GetComponent<PlayerView>();
            if (playerView != null && playerView.BodyCollider != null && playerView.BodyCollider.enabled)
            {
                playerCollider = playerView.BodyCollider;
                return true;
            }

            playerCollider = playerTransform.GetComponent<Collider2D>();
            return playerCollider != null && playerCollider.enabled;
        }

        private static bool IsTouchPortal(MapPortalModel portal)
        {
            return portal.InteractionMode == 0 || portal.InteractionMode == TouchInteractionMode;
        }

        private static string ResolvePortalLabel(MapPortalModel portal)
        {
            if (!string.IsNullOrWhiteSpace(portal.TargetMapName))
                return portal.TargetMapName;

            if (!string.IsNullOrWhiteSpace(portal.Name))
                return portal.Name;

            return "Portal";
        }

        private void ClearPortals()
        {
            foreach (var portalRuntime in spawnedPortals.Values)
            {
                if (portalRuntime != null && portalRuntime.RootObject != null)
                    Destroy(portalRuntime.RootObject);
            }

            spawnedPortals.Clear();
        }

        private void RefreshPortalSelectionVisuals()
        {
            WorldTargetHandle? currentTarget = null;
            if (ClientRuntime.IsInitialized)
                currentTarget = ClientRuntime.Target.CurrentTarget;

            foreach (var runtime in spawnedPortals.Values)
            {
                if (runtime == null || runtime.VisualInstance == null)
                    continue;

                var isSelected = currentTarget.HasValue &&
                                 currentTarget.Value.IsValid &&
                                 ClientWorldState.TryParsePortalTargetId(currentTarget.Value.TargetId, out var portalId) &&
                                 portalId == runtime.Portal.Id;
                runtime.VisualInstance.SetSelected(isSelected);
            }
        }

        private void AutoWireReferences()
        {
            if (worldSceneController == null)
                worldSceneController = GetComponent<WorldSceneController>();

            if (worldMapPresenter == null)
                worldMapPresenter = GetComponent<WorldMapPresenter>();

            if (readinessService == null)
                readinessService = GetComponent<WorldSceneReadinessService>();

            if (worldTargetActionController == null)
                worldTargetActionController = GetComponent<WorldTargetActionController>();

            if (worldLocalMovementSyncController == null)
                worldLocalMovementSyncController = GetComponent<WorldLocalMovementSyncController>();
        }

        private void TryBindRuntimeEvents()
        {
            if (runtimeEventsBound || !ClientRuntime.IsInitialized)
                return;

            if (readinessService != null)
            {
                readinessService.LoadCycleStarted += HandleLoadCycleStarted;
                readinessService.ReadyReported += HandleReadyReported;
            }
            ClientRuntime.Target.CurrentTargetChanged += HandleCurrentTargetChanged;
            if (worldTargetActionController != null)
                worldTargetActionController.InteractionRequested += HandleInteractionRequested;
            runtimeEventsBound = true;
        }

        private void UnbindRuntimeEvents()
        {
            if (!runtimeEventsBound || !ClientRuntime.IsInitialized)
                return;

            if (readinessService != null)
            {
                readinessService.LoadCycleStarted -= HandleLoadCycleStarted;
                readinessService.ReadyReported -= HandleReadyReported;
            }
            ClientRuntime.Target.CurrentTargetChanged -= HandleCurrentTargetChanged;
            if (worldTargetActionController != null)
                worldTargetActionController.InteractionRequested -= HandleInteractionRequested;
            runtimeEventsBound = false;
        }

        private void HandleLoadCycleStarted(int loadVersion, string mapKey)
        {
            rebuildRetryPending = false;
            ClearPortals();
        }

        private void HandleReadyReported(int loadVersion, WorldSceneReadyKey key)
        {
            if (key != WorldSceneReadyKey.MapVisual)
                return;

            HandleMapVisualReady();
        }

        private static int ResolveTargetableLayer()
        {
            var layer = LayerMask.NameToLayer("Targetable");
            return layer >= 0 ? layer : 0;
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            if (root == null)
                return;

            root.layer = layer;
            var transform = root.transform;
            for (var i = 0; i < transform.childCount; i++)
                SetLayerRecursively(transform.GetChild(i).gameObject, layer);
        }

        public bool TryResolvePortalWorldPosition(MapPortalModel portal, out Vector2 worldPosition)
        {
            worldPosition = default;
            if (portal.Equals(default(MapPortalModel)) || worldMapPresenter == null)
                return false;

            return worldMapPresenter.TryMapServerPositionToWorld(ResolvePortalServerPosition(portal), out worldPosition);
        }

        public Vector2 ResolvePortalServerPosition(MapPortalModel portal)
        {
            var portalServerPosition = new Vector2(portal.SourceX, portal.SourceY);
            var prefab = ResolvePortalVisualPrefab();
            if (prefab == null)
                return portalServerPosition;

            var visualInstance = prefab.GetComponent<PortalVisualInstance>();
            if (visualInstance == null)
                return portalServerPosition;

            var resolvedX = portalServerPosition.x;
            var edgeOffsetX = Mathf.Max(0f, visualInstance.VisualEdgeOffsetXServerUnits);
            switch (ResolveTouchTriggerSide(portal))
            {
                case TouchTriggerSide.Left:
                    resolvedX += edgeOffsetX;
                    break;

                case TouchTriggerSide.Right:
                    resolvedX -= edgeOffsetX;
                    break;
            }

            return new Vector2(
                resolvedX,
                portalServerPosition.y + visualInstance.VisualOffsetYServerUnits);
        }

        private bool TryResolveCurrentLocalPlayerServerPosition(out Vector2 playerServerPosition)
        {
            playerServerPosition = default;

            if (!ClientRuntime.IsInitialized)
                return false;

            if (worldMapPresenter != null)
            {
                WorldLocalPlayerPresenter localPlayerPresenter = null;
                if (worldSceneController != null)
                    localPlayerPresenter = worldSceneController.WorldLocalPlayerPresenter;
                if (localPlayerPresenter == null)
                    localPlayerPresenter = GetComponent<WorldLocalPlayerPresenter>();

                var playerTransform = localPlayerPresenter != null
                    ? localPlayerPresenter.CurrentPlayerTransform
                    : null;
                if (playerTransform != null &&
                    worldMapPresenter.TryMapWorldPositionToServer(playerTransform.position, out playerServerPosition))
                {
                    return true;
                }
            }

            playerServerPosition = ClientRuntime.World.LocalPlayerPosition;
            return true;
        }

        private TouchTriggerSide ResolveTouchTriggerSide(MapPortalModel portal)
        {
            var mapWidth = ClientRuntime.World.CurrentMapWidth;
            if (mapWidth <= Mathf.Epsilon)
                return TouchTriggerSide.None;

            var normalizedX = Mathf.Clamp01(portal.SourceX / mapWidth);
            var edgeThreshold = Mathf.Clamp(edgePortalThresholdNormalized, 0.01f, 0.49f);
            if (normalizedX <= edgeThreshold)
                return TouchTriggerSide.Left;

            if (normalizedX >= 1f - edgeThreshold)
                return TouchTriggerSide.Right;

            return TouchTriggerSide.None;
        }

        private static void SetTouchTriggerEnabled(Collider2D collider, bool enabled)
        {
            if (collider == null)
                return;

            collider.isTrigger = true;
            collider.enabled = enabled;
        }

        private enum TouchTriggerSide
        {
            None = 0,
            Left = 1,
            Right = 2
        }
    }
}
