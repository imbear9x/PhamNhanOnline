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

        private GameObject activeMapInstance;
        private ClientMapView activeMapView;
        private Bounds cachedPlayableBounds;
        private bool hasCachedPlayableBounds;
        private string activeClientMapKey = string.Empty;

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

        private void HandleMapChanged()
        {
            RebuildActiveMap(ClientRuntime.World.CurrentClientMapKey);
        }

        private void RebuildActiveMap(string clientMapKey)
        {
            if (string.Equals(activeClientMapKey, clientMapKey, StringComparison.Ordinal) && activeMapInstance != null)
                return;

            ClearActiveMap();
            activeClientMapKey = clientMapKey ?? string.Empty;

            if (string.IsNullOrWhiteSpace(activeClientMapKey))
                return;

            if (mapCatalog == null)
            {
                ClientLog.Warn($"WorldMapPresenter has no {nameof(ClientMapCatalog)} assigned.");
                return;
            }

            GameObject mapPrefab;
            if (!mapCatalog.TryGetMapPrefab(activeClientMapKey, out mapPrefab))
            {
                ClientLog.Warn($"No map prefab is registered for ClientMapKey '{activeClientMapKey}'.");
                return;
            }

            var parent = activeMapRoot != null ? activeMapRoot : transform;
            activeMapInstance = Instantiate(mapPrefab, parent, false);
            activeMapInstance.name = mapPrefab.name;
            CachePlayableBounds();
            ClientLog.Info($"Spawned map '{activeClientMapKey}' into World scene.");
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

            if (activeMapView != null && activeMapView.TryGetPlayableBounds(out cachedPlayableBounds))
            {
                hasCachedPlayableBounds = true;
                return;
            }

            if (TryResolveFallbackPlayableBounds(out cachedPlayableBounds))
            {
                hasCachedPlayableBounds = true;
                return;
            }

            ClientLog.Warn("WorldMapPresenter could not resolve playable bounds for the active map.");
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
    }
}