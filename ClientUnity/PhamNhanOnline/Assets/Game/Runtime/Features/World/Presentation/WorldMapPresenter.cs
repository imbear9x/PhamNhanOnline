using System;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    public sealed class WorldMapPresenter : MonoBehaviour
    {
        private const string PlayableBoundsObjectName = "PlayableBounds";

        [SerializeField] private ClientMapCatalog mapCatalog;
        [SerializeField] private Transform activeMapRoot;
        [SerializeField] private WorldSceneReadinessService readinessService;

        private GameObject activeMapInstance;
        private ClientMapView activeMapView;
        private Bounds cachedPlayableBounds;
        private bool hasCachedPlayableBounds;
        private string activeClientMapKey = string.Empty;

        public event Action ActiveMapVisualChanged;

        public Transform CurrentMapTransform
        {
            get { return activeMapInstance != null ? activeMapInstance.transform : null; }
        }

        private void Start()
        {
            if (!ClientRuntime.IsInitialized)
            {
                ClientLog.Warn("WorldMapPresenter started before ClientRuntime initialization.");
                return;
            }

            AutoWireReferences();
            if (readinessService != null)
                readinessService.EnsureLoadCycleForCurrentMapState();

            ClientRuntime.World.MapChanged += HandleMapChanged;
            RebuildActiveMap(ClientRuntime.World.CurrentClientMapKey);
        }

        private void OnDestroy()
        {
            if (ClientRuntime.IsInitialized)
                ClientRuntime.World.MapChanged -= HandleMapChanged;

            ClearActiveMap();
        }

        public void Refresh()
        {
            if (!ClientRuntime.IsInitialized)
            {
                ClientLog.Warn("WorldMapPresenter cannot refresh before ClientRuntime initialization.");
                return;
            }

            RebuildActiveMap(ClientRuntime.World.CurrentClientMapKey);
        }

        public bool TryGetPlayableBounds(out Bounds bounds)
        {
            if (!EnsurePlayableBoundsCached())
            {
                bounds = default;
                return false;
            }

            bounds = cachedPlayableBounds;
            return true;
        }

        public bool TryGetWorldUnitsPerServerUnit(out Vector2 worldUnitsPerServerUnit)
        {
            worldUnitsPerServerUnit = default;

            if (!ClientRuntime.IsInitialized)
                return false;

            var mapWidth = ClientRuntime.World.CurrentMapWidth;
            var mapHeight = ClientRuntime.World.CurrentMapHeight;
            if (mapWidth <= 0f || mapHeight <= 0f)
                return false;

            if (!EnsurePlayableBoundsCached())
                return false;

            var boundsWidth = cachedPlayableBounds.size.x;
            var boundsHeight = cachedPlayableBounds.size.y;
            if (boundsWidth <= Mathf.Epsilon || boundsHeight <= Mathf.Epsilon)
                return false;

            worldUnitsPerServerUnit = new Vector2(boundsWidth / mapWidth, boundsHeight / mapHeight);
            return true;
        }

        public bool TryMapServerPositionToWorld(Vector2 serverPosition, out Vector2 worldPosition)
        {
            worldPosition = default;

            if (!ClientRuntime.IsInitialized)
                return false;

            var mapWidth = ClientRuntime.World.CurrentMapWidth;
            var mapHeight = ClientRuntime.World.CurrentMapHeight;
            if (mapWidth <= 0f || mapHeight <= 0f)
                return false;

            if (!EnsurePlayableBoundsCached())
                return false;

            var normalizedX = Mathf.Clamp01(serverPosition.x / mapWidth);
            var normalizedY = Mathf.Clamp01(serverPosition.y / mapHeight);
            worldPosition = new Vector2(
                Mathf.Lerp(cachedPlayableBounds.min.x, cachedPlayableBounds.max.x, normalizedX),
                Mathf.Lerp(cachedPlayableBounds.min.y, cachedPlayableBounds.max.y, normalizedY));
            return true;
        }

        public bool TryMapWorldPositionToServer(Vector2 worldPosition, out Vector2 serverPosition)
        {
            serverPosition = default;

            if (!ClientRuntime.IsInitialized)
                return false;

            var mapWidth = ClientRuntime.World.CurrentMapWidth;
            var mapHeight = ClientRuntime.World.CurrentMapHeight;
            if (mapWidth <= 0f || mapHeight <= 0f)
                return false;

            if (!EnsurePlayableBoundsCached())
                return false;

            var boundsWidth = cachedPlayableBounds.size.x;
            var boundsHeight = cachedPlayableBounds.size.y;
            if (boundsWidth <= Mathf.Epsilon || boundsHeight <= Mathf.Epsilon)
                return false;

            var normalizedX = Mathf.Clamp01(Mathf.InverseLerp(cachedPlayableBounds.min.x, cachedPlayableBounds.max.x, worldPosition.x));
            var normalizedY = Mathf.Clamp01(Mathf.InverseLerp(cachedPlayableBounds.min.y, cachedPlayableBounds.max.y, worldPosition.y));
            serverPosition = new Vector2(normalizedX * mapWidth, normalizedY * mapHeight);
            return true;
        }

        private void HandleMapChanged()
        {
            if (readinessService != null)
                readinessService.EnsureLoadCycleForCurrentMapState();

            RebuildActiveMap(ClientRuntime.World.CurrentClientMapKey);
        }

        private void RebuildActiveMap(string clientMapKey)
        {
            if (string.Equals(activeClientMapKey, clientMapKey, StringComparison.Ordinal) && activeMapInstance != null)
                return;

            ClearActiveMap();
            activeClientMapKey = clientMapKey ?? string.Empty;

            if (string.IsNullOrWhiteSpace(activeClientMapKey))
            {
                NotifyActiveMapVisualChanged();
                return;
            }

            if (mapCatalog == null)
            {
                ClientLog.Warn($"WorldMapPresenter has no {nameof(ClientMapCatalog)} assigned.");
                NotifyActiveMapVisualChanged();
                return;
            }

            GameObject mapPrefab;
            if (!mapCatalog.TryGetMapPrefab(activeClientMapKey, out mapPrefab))
            {
                ClientLog.Warn($"No map prefab is registered for ClientMapKey '{activeClientMapKey}'.");
                NotifyActiveMapVisualChanged();
                return;
            }

            var parent = activeMapRoot != null ? activeMapRoot : transform;
            activeMapInstance = Instantiate(mapPrefab, parent, false);
            activeMapInstance.name = mapPrefab.name;
            CachePlayableBounds();
            if (readinessService != null && hasCachedPlayableBounds)
                readinessService.ReportReady(WorldSceneReadyKey.MapVisual);
            ClientLog.Info($"Spawned map '{activeClientMapKey}' into World scene.");
            NotifyActiveMapVisualChanged();
        }

        private bool EnsurePlayableBoundsCached()
        {
            if (hasCachedPlayableBounds)
                return true;

            CachePlayableBounds();
            return hasCachedPlayableBounds;
        }

        private void CachePlayableBounds()
        {
            activeMapView = activeMapInstance != null ? activeMapInstance.GetComponent<ClientMapView>() : null;
            hasCachedPlayableBounds = false;
            cachedPlayableBounds = default;

            if (activeMapInstance == null || string.IsNullOrWhiteSpace(activeClientMapKey))
                return;

            if (activeMapView != null && activeMapView.TryGetPlayableBounds(out cachedPlayableBounds))
            {
                hasCachedPlayableBounds = true;
                return;
            }

            if (TryResolveFallbackPlayableBounds(out cachedPlayableBounds))
            {
                hasCachedPlayableBounds = true;
                ClientLog.Info(
                    $"WorldMapPresenter resolved playable bounds via fallback for map '{activeClientMapKey}' " +
                    $"on prefab '{(activeMapInstance != null ? activeMapInstance.name : "null")}'. " +
                    $"Bounds center={cachedPlayableBounds.center}, size={cachedPlayableBounds.size}.");
                return;
            }

            var mapViewState = activeMapView != null
                ? activeMapView.DescribePlayableBoundsSources()
                : "ClientMapView=null";
            var explicitBoundsRoot = activeMapInstance != null
                ? FindChildRecursive(activeMapInstance.transform, PlayableBoundsObjectName)
                : null;
            var explicitBoundsState = explicitBoundsRoot != null
                ? $"explicitBoundsRoot='{explicitBoundsRoot.name}', hasCollider={explicitBoundsRoot.GetComponent<Collider2D>() != null}, hasRenderer={explicitBoundsRoot.GetComponent<Renderer>() != null}, activeInHierarchy={explicitBoundsRoot.gameObject.activeInHierarchy}"
                : "explicitBoundsRoot=null";
            var colliderCount = activeMapInstance != null
                ? activeMapInstance.GetComponentsInChildren<Collider2D>(true).Length
                : 0;
            var rendererCount = activeMapInstance != null
                ? activeMapInstance.GetComponentsInChildren<Renderer>(true).Length
                : 0;
            ClientLog.Warn(
                $"WorldMapPresenter could not resolve playable bounds for map '{activeClientMapKey}' " +
                $"on prefab '{(activeMapInstance != null ? activeMapInstance.name : "null")}'. " +
                $"{mapViewState}. {explicitBoundsState}. " +
                $"colliderCount={colliderCount}, rendererCount={rendererCount}.");
        }

        private bool TryResolveFallbackPlayableBounds(out Bounds bounds)
        {
            if (activeMapInstance == null)
            {
                bounds = default;
                return false;
            }

            var explicitBoundsRoot = FindChildRecursive(activeMapInstance.transform, PlayableBoundsObjectName);
            if (explicitBoundsRoot != null)
            {
                var explicitCollider = explicitBoundsRoot.GetComponent<Collider2D>();
                if (explicitCollider != null)
                {
                    bounds = explicitCollider.bounds;
                    return true;
                }

                var explicitRenderer = explicitBoundsRoot.GetComponent<Renderer>();
                if (explicitRenderer != null)
                {
                    bounds = explicitRenderer.bounds;
                    return true;
                }
            }

            var colliders = activeMapInstance.GetComponentsInChildren<Collider2D>(true);
            if (TryEncapsulateColliderBounds(colliders, out bounds))
                return true;

            var renderers = activeMapInstance.GetComponentsInChildren<Renderer>(true);
            if (TryEncapsulateRendererBounds(renderers, out bounds))
                return true;

            bounds = default;
            return false;
        }

        private static bool TryEncapsulateColliderBounds(Collider2D[] colliders, out Bounds bounds)
        {
            bounds = default;
            var hasBounds = false;

            for (var i = 0; i < colliders.Length; i++)
            {
                var collider = colliders[i];
                if (collider == null || !collider.enabled)
                    continue;

                if (!hasBounds)
                {
                    bounds = collider.bounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(collider.bounds);
            }

            return hasBounds;
        }

        private static bool TryEncapsulateRendererBounds(Renderer[] renderers, out Bounds bounds)
        {
            bounds = default;
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

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null)
                return null;

            if (string.Equals(root.name, childName, StringComparison.Ordinal))
                return root;

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindChildRecursive(root.GetChild(i), childName);
                if (found != null)
                    return found;
            }

            return null;
        }

        private void ClearActiveMap()
        {
            if (activeMapInstance != null)
                Destroy(activeMapInstance);

            activeMapInstance = null;
            activeMapView = null;
            hasCachedPlayableBounds = false;
            cachedPlayableBounds = default;
            activeClientMapKey = string.Empty;
        }

        private void AutoWireReferences()
        {
            if (readinessService == null)
                readinessService = GetComponent<WorldSceneReadinessService>();
        }

        private void NotifyActiveMapVisualChanged()
        {
            var handler = ActiveMapVisualChanged;
            if (handler != null)
                handler();
        }
    }
}
