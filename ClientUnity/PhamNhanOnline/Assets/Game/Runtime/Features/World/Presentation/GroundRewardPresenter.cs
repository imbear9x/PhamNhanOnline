using System.Collections;
using GameShared.Models;
using PhamNhanOnline.Client.Features.Targeting.Application;
using PhamNhanOnline.Client.UI.Inventory;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    [DisallowMultipleComponent]
    public sealed class GroundRewardPresenter : MonoBehaviour
    {
        private static Sprite fallbackSprite;

        [SerializeField] private WorldTargetable targetable;

        private Transform visualRoot;
        private Transform scaleRoot;
        private SpriteRenderer iconRenderer;
        private SpriteRenderer[] outlineRenderers = new SpriteRenderer[4];
        private Renderer[] boundsRenderers = System.Array.Empty<Renderer>();
        private GroundRewardVisualBindings visualBindings;
        private GameObject visualInstance;
        private GameObject activeVisualPrefab;
        private Coroutine pickupAnimationCoroutine;
        private Coroutine spawnAnimationCoroutine;
        private Vector3 anchoredWorldPosition;
        private Vector3 currentAnimatedPosition;
        private float bobAmplitudeWorldUnits;
        private float bobSpeed;
        private float selectionScaleMultiplier = 1.1f;
        private float iconWorldSize = 0.75f;
        private bool usePrefabVisualScale;
        private Vector3 baseVisualScale = Vector3.one;
        private bool isSelected;
        private bool isCollecting;
        private bool isSpawning;
        private bool pendingDestroyAfterCollect;
        private float bobTimeOffset;
        private bool snapToGround = true;
        private LayerMask groundLayerMask;
        private float groundProbeHeight = 3f;
        private float groundProbeDistance = 12f;
        private float groundContactOffset;

        public int RewardId { get; private set; }
        public bool IsCollecting { get { return isCollecting; } }

        private void Awake()
        {
            EnsureVisualHierarchy(null);
            EnsureTargetable();
        }

        private void Update()
        {
            if (isCollecting)
                return;

            ApplyBaseScale();

            var basePosition = isSpawning ? currentAnimatedPosition : anchoredWorldPosition;
            transform.position = basePosition + new Vector3(
                0f,
                !isSpawning && bobAmplitudeWorldUnits > Mathf.Epsilon
                    ? Mathf.Sin((Time.unscaledTime + bobTimeOffset) * Mathf.Max(0f, bobSpeed)) * bobAmplitudeWorldUnits
                    : 0f,
                0f);
        }

        public void ApplySnapshot(
            GroundRewardModel reward,
            WorldMapPresenter worldMapPresenter,
            InventoryItemPresentationCatalog itemPresentationCatalog,
            string sortingLayerName,
            int sortingOrder,
            float configuredIconWorldSize,
            float outlineOffsetWorldUnits,
            Color outlineColor,
            float configuredBobAmplitudeWorldUnits,
            float configuredBobSpeed,
            float verticalOffsetWorldUnits,
            float configuredSelectionScaleMultiplier,
            GameObject visualPrefab,
            bool configuredSnapToGround,
            LayerMask configuredGroundLayerMask,
            float configuredGroundProbeHeight,
            float configuredGroundProbeDistance,
            float configuredGroundContactOffset)
        {
            RewardId = reward.RewardId;
            bobAmplitudeWorldUnits = Mathf.Max(0f, configuredBobAmplitudeWorldUnits);
            bobSpeed = Mathf.Max(0f, configuredBobSpeed);
            iconWorldSize = Mathf.Max(0.05f, configuredIconWorldSize);
            selectionScaleMultiplier = Mathf.Max(1f, configuredSelectionScaleMultiplier);
            bobTimeOffset = reward.RewardId * 0.173f;
            snapToGround = configuredSnapToGround;
            groundLayerMask = configuredGroundLayerMask;
            groundProbeHeight = Mathf.Max(0.1f, configuredGroundProbeHeight);
            groundProbeDistance = Mathf.Max(0.5f, configuredGroundProbeDistance);
            groundContactOffset = configuredGroundContactOffset;

            EnsureVisualHierarchy(visualPrefab);
            EnsureTargetable();
            ConfigureTargetable(reward.RewardId);
            ApplyLayers();
            ApplySprite(ResolveSprite(reward, itemPresentationCatalog));
            ApplySorting(sortingLayerName, sortingOrder);
            ApplyOutline(outlineOffsetWorldUnits, outlineColor);
            ApplyBaseScale();
            UpdateWorldPosition(reward, worldMapPresenter, verticalOffsetWorldUnits);
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
        }

        public void BeginPickupAnimation(Transform targetTransform, float durationSeconds, float endScaleMultiplier)
        {
            if (isCollecting)
                return;

            StopSpawnAnimation();

            EnsureTargetable();
            if (targetable != null)
                targetable.enabled = false;

            isCollecting = true;
            if (pickupAnimationCoroutine != null)
                StopCoroutine(pickupAnimationCoroutine);

            pickupAnimationCoroutine = StartCoroutine(PlayPickupAnimation(
                targetTransform,
                Mathf.Max(0.05f, durationSeconds),
                Mathf.Clamp01(endScaleMultiplier)));
        }

        public void MarkPendingDestroy()
        {
            if (isCollecting)
            {
                pendingDestroyAfterCollect = true;
                return;
            }

            Destroy(gameObject);
        }

        private IEnumerator PlayPickupAnimation(Transform targetTransform, float durationSeconds, float endScaleMultiplier)
        {
            var startPosition = transform.position;
            var startScale = scaleRoot != null ? scaleRoot.localScale : Vector3.one;
            var endScale = startScale * Mathf.Clamp(endScaleMultiplier, 0.01f, 1f);
            var elapsed = 0f;

            while (elapsed < durationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / durationSeconds);
                var eased = 1f - Mathf.Pow(1f - t, 3f);
                var targetPosition = ResolvePickupTargetPosition(targetTransform);
                transform.position = Vector3.LerpUnclamped(startPosition, targetPosition, eased);
                if (scaleRoot != null)
                    scaleRoot.localScale = Vector3.LerpUnclamped(startScale, endScale, eased);
                yield return null;
            }

            if (targetTransform != null)
                transform.position = ResolvePickupTargetPosition(targetTransform);

            if (scaleRoot != null)
                scaleRoot.localScale = endScale;

            isCollecting = false;
            pickupAnimationCoroutine = null;
            if (pendingDestroyAfterCollect)
            {
                Destroy(gameObject);
                yield break;
            }

            gameObject.SetActive(false);
        }

        public void BeginSpawnAnimation(float durationSeconds, float arcHeightWorldUnits, float horizontalOffsetWorldUnits)
        {
            StopSpawnAnimation();

            durationSeconds = Mathf.Max(0.05f, durationSeconds);
            arcHeightWorldUnits = Mathf.Max(0f, arcHeightWorldUnits);

            if (arcHeightWorldUnits <= Mathf.Epsilon && Mathf.Abs(horizontalOffsetWorldUnits) <= Mathf.Epsilon)
            {
                isSpawning = false;
                currentAnimatedPosition = anchoredWorldPosition;
                return;
            }

            spawnAnimationCoroutine = StartCoroutine(PlaySpawnAnimation(
                durationSeconds,
                arcHeightWorldUnits,
                horizontalOffsetWorldUnits));
        }

        private IEnumerator PlaySpawnAnimation(float durationSeconds, float arcHeightWorldUnits, float horizontalOffsetWorldUnits)
        {
            isSpawning = true;
            var endPosition = anchoredWorldPosition;
            var startPosition = endPosition + new Vector3(-horizontalOffsetWorldUnits, 0f, 0f);
            currentAnimatedPosition = startPosition;
            var elapsed = 0f;

            while (elapsed < durationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / durationSeconds);
                var horizontal = Vector3.LerpUnclamped(startPosition, endPosition, t);
                var arcOffset = Mathf.Sin(t * Mathf.PI) * arcHeightWorldUnits;
                currentAnimatedPosition = new Vector3(horizontal.x, horizontal.y + arcOffset, endPosition.z);
                yield return null;
            }

            currentAnimatedPosition = anchoredWorldPosition;
            isSpawning = false;
            spawnAnimationCoroutine = null;
        }

        private Vector3 ResolvePickupTargetPosition(Transform targetTransform)
        {
            if (targetTransform == null)
                return transform.position;

            var position = targetTransform.position;
            return new Vector3(position.x, position.y + 0.5f, transform.position.z);
        }

        private void UpdateWorldPosition(GroundRewardModel reward, WorldMapPresenter worldMapPresenter, float verticalOffsetWorldUnits)
        {
            var serverPosition = new Vector2(reward.PosX, reward.PosY);
            Vector2 worldPosition;
            var resolvedWorldPosition = worldMapPresenter != null && worldMapPresenter.TryMapServerPositionToWorld(serverPosition, out worldPosition)
                ? worldPosition
                : serverPosition;
            anchoredWorldPosition = ResolveAnchoredWorldPosition(resolvedWorldPosition, verticalOffsetWorldUnits);
            if (!isSpawning)
                currentAnimatedPosition = anchoredWorldPosition;
        }

        private Vector3 ResolveAnchoredWorldPosition(Vector2 worldPosition, float verticalOffsetWorldUnits)
        {
            if (!snapToGround)
                return new Vector3(worldPosition.x, worldPosition.y + verticalOffsetWorldUnits, 0f);

            float bottomOffset;
            if (!TryResolveBottomOffset(out bottomOffset))
                return new Vector3(worldPosition.x, worldPosition.y + verticalOffsetWorldUnits, 0f);

            var rayOrigin = new Vector2(worldPosition.x, worldPosition.y + Mathf.Max(groundProbeHeight, Mathf.Abs(bottomOffset) + 0.25f));
            var rayDistance = Mathf.Max(0.5f, groundProbeHeight + groundProbeDistance + Mathf.Abs(bottomOffset));
            var hit = Physics2D.Raycast(rayOrigin, Vector2.down, rayDistance, ResolveGroundLayerMask());
            if (hit.collider == null)
                return new Vector3(worldPosition.x, worldPosition.y + verticalOffsetWorldUnits, 0f);

            var resolvedY = hit.point.y - bottomOffset + groundContactOffset + verticalOffsetWorldUnits;
            return new Vector3(worldPosition.x, resolvedY, 0f);
        }

        private void ApplyBaseScale()
        {
            if (scaleRoot == null)
                return;

            if (usePrefabVisualScale)
            {
                scaleRoot.localScale = baseVisualScale * (isSelected ? Mathf.Max(1f, selectionScaleMultiplier) : 1f);
                return;
            }

            var scale = iconWorldSize * (isSelected ? Mathf.Max(1f, selectionScaleMultiplier) : 1f);
            scaleRoot.localScale = new Vector3(scale, scale, 1f);
        }

        private bool TryResolveBottomOffset(out float bottomOffset)
        {
            bottomOffset = 0f;

            Bounds bounds;
            if (!TryGetPresentationBounds(out bounds))
                return false;

            bottomOffset = bounds.min.y - transform.position.y;
            return true;
        }

        private bool TryGetPresentationBounds(out Bounds bounds)
        {
            bounds = default;
            var hasBounds = false;

            for (var i = 0; i < boundsRenderers.Length; i++)
            {
                var renderer = boundsRenderers[i];
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

        private int ResolveGroundLayerMask()
        {
            if (groundLayerMask.value != 0)
                return groundLayerMask.value;

            var worldMapLayer = LayerMask.NameToLayer("WorldMap");
            if (worldMapLayer >= 0)
                return 1 << worldMapLayer;

            return Physics2D.DefaultRaycastLayers;
        }

        private void ConfigureTargetable(int rewardId)
        {
            if (targetable == null)
                return;

            targetable.Configure(new WorldTargetHandle(
                WorldTargetKind.GroundReward,
                PhamNhanOnline.Client.Features.World.Application.ClientWorldState.BuildGroundRewardTargetId(rewardId)));
        }

        private void EnsureTargetable()
        {
            if (targetable == null)
                targetable = GetComponent<WorldTargetable>();

            if (targetable == null)
                targetable = gameObject.AddComponent<WorldTargetable>();
        }

        private void EnsureVisualHierarchy(GameObject visualPrefab)
        {
            if (visualRoot == null)
            {
                var visualRootTransform = transform.Find("RewardVisual");
                if (visualRootTransform == null)
                {
                    var visualRootObject = new GameObject("RewardVisual");
                    visualRootObject.transform.SetParent(transform, false);
                    visualRootTransform = visualRootObject.transform;
                }

                visualRoot = visualRootTransform;
            }

            if (visualInstance == null || activeVisualPrefab != visualPrefab)
                RebuildVisualInstance(visualPrefab);

            if (iconRenderer == null)
                iconRenderer = EnsureRenderer("Icon", visualRoot, 0);

            for (var i = 0; i < outlineRenderers.Length; i++)
            {
                if (outlineRenderers[i] == null)
                    outlineRenderers[i] = EnsureRenderer("Outline_" + i, visualRoot, -1);
            }

            if (boundsRenderers == null || boundsRenderers.Length == 0)
            {
                boundsRenderers = new Renderer[1 + outlineRenderers.Length];
                boundsRenderers[0] = iconRenderer;
                for (var i = 0; i < outlineRenderers.Length; i++)
                    boundsRenderers[i + 1] = outlineRenderers[i];
            }
        }

        private void StopSpawnAnimation()
        {
            if (spawnAnimationCoroutine != null)
            {
                StopCoroutine(spawnAnimationCoroutine);
                spawnAnimationCoroutine = null;
            }

            isSpawning = false;
            currentAnimatedPosition = anchoredWorldPosition;
        }

        private void RebuildVisualInstance(GameObject visualPrefab)
        {
            if (visualInstance != null)
            {
                if (UnityEngine.Application.isPlaying)
                    Destroy(visualInstance);
                else
                    DestroyImmediate(visualInstance);
            }

            activeVisualPrefab = visualPrefab;
            visualInstance = null;
            visualBindings = null;
            iconRenderer = null;
            outlineRenderers = new SpriteRenderer[4];
            boundsRenderers = System.Array.Empty<Renderer>();
            scaleRoot = visualRoot;
            usePrefabVisualScale = false;
            baseVisualScale = Vector3.one;

            if (visualRoot == null || visualPrefab == null)
                return;

            visualInstance = Instantiate(visualPrefab, visualRoot, false);
            visualInstance.name = visualPrefab.name;

            var bindings = visualInstance.GetComponent<GroundRewardVisualBindings>();
            visualBindings = bindings;
            if (bindings != null)
            {
                scaleRoot = bindings.ScaleRoot != null ? bindings.ScaleRoot : visualInstance.transform;
                iconRenderer = bindings.IconRenderer;
                var boundOutlineRenderers = bindings.OutlineRenderers;
                if (boundOutlineRenderers != null && boundOutlineRenderers.Length > 0)
                    outlineRenderers = boundOutlineRenderers;
                var boundBoundsRenderers = bindings.BoundsRenderers;
                if (boundBoundsRenderers != null && boundBoundsRenderers.Length > 0)
                    boundsRenderers = boundBoundsRenderers;
            }
            else
            {
                scaleRoot = visualInstance.transform;
                iconRenderer = visualInstance.GetComponentInChildren<SpriteRenderer>(true);
                var prefabRenderers = visualInstance.GetComponentsInChildren<SpriteRenderer>(true);
                if (prefabRenderers != null && prefabRenderers.Length > 0)
                    boundsRenderers = prefabRenderers;
            }

            if (scaleRoot == null)
                scaleRoot = visualInstance.transform;

            baseVisualScale = scaleRoot.localScale;
            usePrefabVisualScale = true;
        }

        private static SpriteRenderer EnsureRenderer(string childName, Transform parent, int sortingOrderOffset)
        {
            var child = parent.Find(childName);
            if (child == null)
            {
                var childObject = new GameObject(childName);
                childObject.transform.SetParent(parent, false);
                child = childObject.transform;
            }

            var renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null)
                renderer = child.gameObject.AddComponent<SpriteRenderer>();

            renderer.sortingOrder = sortingOrderOffset;
            return renderer;
        }

        private void ApplySprite(Sprite sprite)
        {
            if (visualBindings != null)
            {
                visualBindings.ApplySprite(sprite);
                iconRenderer = visualBindings.IconRenderer;
                return;
            }

            if (iconRenderer != null)
                iconRenderer.sprite = sprite;

            for (var i = 0; i < outlineRenderers.Length; i++)
            {
                if (outlineRenderers[i] != null)
                    outlineRenderers[i].sprite = sprite;
            }
        }

        private void ApplySorting(string sortingLayerName, int sortingOrder)
        {
            if (iconRenderer != null)
            {
                iconRenderer.sortingLayerName = sortingLayerName;
                iconRenderer.sortingOrder = sortingOrder;
            }

            for (var i = 0; i < outlineRenderers.Length; i++)
            {
                if (outlineRenderers[i] == null)
                    continue;

                outlineRenderers[i].sortingLayerName = sortingLayerName;
                outlineRenderers[i].sortingOrder = sortingOrder - 1;
            }
        }

        private void ApplyOutline(float outlineOffsetWorldUnits, Color outlineColor)
        {
            var outlineEnabled = outlineOffsetWorldUnits > Mathf.Epsilon;
            var offsets = new[]
            {
                new Vector3(-outlineOffsetWorldUnits, 0f, 0f),
                new Vector3(outlineOffsetWorldUnits, 0f, 0f),
                new Vector3(0f, -outlineOffsetWorldUnits, 0f),
                new Vector3(0f, outlineOffsetWorldUnits, 0f)
            };

            for (var i = 0; i < outlineRenderers.Length; i++)
            {
                var renderer = outlineRenderers[i];
                if (renderer == null)
                    continue;

                renderer.enabled = outlineEnabled;
                renderer.color = outlineColor;
                renderer.transform.localPosition = outlineEnabled ? offsets[i] : Vector3.zero;
            }

            if (iconRenderer != null)
            {
                iconRenderer.color = Color.white;
                iconRenderer.transform.localPosition = Vector3.zero;
            }
        }

        private void ApplyLayers()
        {
            var targetableLayer = LayerMask.NameToLayer("Targetable");
            if (targetableLayer >= 0)
                gameObject.layer = targetableLayer;

            var visualLayer = LayerMask.NameToLayer("GroundReward");
            if (visualLayer >= 0 && visualRoot != null)
                SetLayerRecursively(visualRoot.gameObject, visualLayer);
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            if (root == null)
                return;

            root.layer = layer;
            var rootTransform = root.transform;
            for (var i = 0; i < rootTransform.childCount; i++)
                SetLayerRecursively(rootTransform.GetChild(i).gameObject, layer);
        }

        private static Sprite ResolveSprite(GroundRewardModel reward, InventoryItemPresentationCatalog itemPresentationCatalog)
        {
            if (reward.Items != null && reward.Items.Count > 0 && itemPresentationCatalog != null)
            {
                var presentation = itemPresentationCatalog.Resolve(reward.Items[0]);
                if (presentation.IconSprite != null)
                    return presentation.IconSprite;
            }

            if (fallbackSprite == null)
            {
                var texture = Texture2D.whiteTexture;
                fallbackSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    texture.width);
            }

            return fallbackSprite;
        }
    }
}
