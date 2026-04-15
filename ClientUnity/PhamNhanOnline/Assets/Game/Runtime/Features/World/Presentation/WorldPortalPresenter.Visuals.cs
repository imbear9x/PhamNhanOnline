using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Features.Targeting.Application;
using PhamNhanOnline.Client.Features.World.Application;
using TMPro;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    public sealed partial class WorldPortalPresenter
    {
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

                var portalServerPosition = new Vector2(portal.SourceX, portal.SourceY);
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

            if (logTouchPortalDiagnostics)
                ClientLog.Info($"[PortalTouch] rebuilt {spawnedPortals.Count} portals for map={ClientRuntime.World.CurrentMapId}.");
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
            if (!IsReady(WorldSceneReadyKey.MapVisual))
                return;

            RebuildPortals();
        }

        private PortalRuntime BuildPortalObject(Transform parent, MapPortalModel portal, Vector2 worldPosition)
        {
            var portalObject = CreatePortalVisualRoot(parent, portal, worldPosition);

            var visualInstance = portalObject.GetComponent<PortalVisualInstance>();
            if (visualInstance != null)
            {
                visualInstance.Apply(ResolvePortalLabel(portal));
                ApplyPortalVisualLayout(visualInstance, portal);
            }

            var labelObject = visualInstance != null
                ? visualInstance.LabelObject
                : CreateFallbackLabelChild(portalObject.transform, portal);
            ConfigureInteractiveLabel(labelObject, portal);
            var collider = ConfigureTouchTrigger(visualInstance, portal);

            if (logTouchPortalDiagnostics)
            {
                var triggerName = collider != null ? collider.name : "<none>";
                ClientLog.Info($"[PortalTouch] built portal={portal.Id} name='{ResolvePortalLabel(portal)}' side={ResolveTouchTriggerSide(portal)} interactionMode={portal.InteractionMode} enabled={portal.IsEnabled} trigger={triggerName}.");
            }

            return new PortalRuntime
            {
                Portal = portal,
                RootObject = portalObject,
                VisualInstance = visualInstance,
                TriggerCollider = collider,
                WasTouchingLastFrame = false,
                LastTouchDiagnosticKey = string.Empty
            };
        }

        private void ApplyPortalVisualLayout(PortalVisualInstance visualInstance, MapPortalModel portal)
        {
            if (visualInstance == null)
                return;

            var side = ResolveTouchTriggerSide(portal);
            var signedOffset = visualInstance.ResolveSignedEdgeVisualOffsetX(
                side == TouchTriggerSide.Left,
                side == TouchTriggerSide.Right);
            visualInstance.ApplyEdgeVisualOffset(signedOffset);
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

            if (SceneController != null && SceneController.MapRoot != null)
                return SceneController.MapRoot;

            return transform;
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
            InitializeWorldSceneBehaviour(ref worldMapPresenter);

            if (worldTargetActionController == null)
                worldTargetActionController = GetComponent<WorldTargetActionController>();

            if (worldLocalMovementSyncController == null)
                worldLocalMovementSyncController = GetComponent<WorldLocalMovementSyncController>();
        }

        private void LogMissingCriticalDependenciesIfNeeded()
        {
            if (worldMapPresenter == null && !loggedMissingWorldMapPresenter)
            {
                ClientLog.Error("WorldPortalPresenter could not resolve WorldMapPresenter.");
                loggedMissingWorldMapPresenter = true;
            }

            if (worldTargetActionController == null && !loggedMissingTargetActionController)
            {
                ClientLog.Error("WorldPortalPresenter could not resolve WorldTargetActionController.");
                loggedMissingTargetActionController = true;
            }
        }

        private void TryBindRuntimeEvents()
        {
            AutoWireReferences();
            if (runtimeEventsBound || !ClientRuntime.IsInitialized || worldTargetActionController == null)
                return;

            ClientRuntime.Target.CurrentTargetChanged += HandleCurrentTargetChanged;
            worldTargetActionController.InteractionRequested += HandleInteractionRequested;
            runtimeEventsBound = true;
        }

        private void UnbindRuntimeEvents()
        {
            if (!runtimeEventsBound || !ClientRuntime.IsInitialized)
                return;

            ClientRuntime.Target.CurrentTargetChanged -= HandleCurrentTargetChanged;
            if (worldTargetActionController != null)
                worldTargetActionController.InteractionRequested -= HandleInteractionRequested;
            runtimeEventsBound = false;
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

        private static void SetTouchTriggerEnabled(Collider2D collider, bool enabled)
        {
            if (collider == null)
                return;

            collider.isTrigger = true;
            collider.enabled = enabled;
        }
    }
}
