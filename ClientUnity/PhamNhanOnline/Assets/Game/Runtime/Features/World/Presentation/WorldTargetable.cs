using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Features.Character.Presentation;
using PhamNhanOnline.Client.Features.Targeting.Application;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    internal static class WorldTargetableRegistry
    {
        private static readonly System.Collections.Generic.HashSet<WorldTargetable> Registered =
            new System.Collections.Generic.HashSet<WorldTargetable>();

        public static void Register(WorldTargetable targetable)
        {
            if (targetable == null)
                return;

            Registered.Add(targetable);
        }

        public static void Unregister(WorldTargetable targetable)
        {
            if (targetable == null)
                return;

            Registered.Remove(targetable);
        }

        public static WorldTargetable[] GetSnapshot()
        {
            if (Registered.Count == 0)
                return System.Array.Empty<WorldTargetable>();

            var result = new WorldTargetable[Registered.Count];
            Registered.CopyTo(result);
            return result;
        }

        public static bool TryGet(WorldTargetHandle handle, out WorldTargetable targetable)
        {
            foreach (var entry in Registered)
            {
                if (entry == null || !entry.isActiveAndEnabled)
                    continue;

                if (!entry.Handle.Equals(handle))
                    continue;

                targetable = entry;
                return true;
            }

            targetable = null;
            return false;
        }
    }

    [DisallowMultipleComponent]
    public sealed class WorldTargetable : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private WorldTargetKind targetKind = WorldTargetKind.None;
        [SerializeField] private string targetId = string.Empty;
        [SerializeField] private string displayNameOverride = string.Empty;

        [Header("Hit Detection")]
        [SerializeField] private Collider2D interactionCollider;
        [SerializeField] private bool autoCreateInteractionCollider = true;
        [SerializeField] private Vector2 autoColliderPadding = new Vector2(0.15f, 0.15f);

        public WorldTargetHandle Handle
        {
            get { return new WorldTargetHandle(targetKind, targetId); }
        }

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(displayNameOverride))
                    return displayNameOverride;

                return !string.IsNullOrWhiteSpace(targetId) ? targetId : name;
            }
        }

        public void Configure(WorldTargetHandle handle)
        {
            targetKind = handle.Kind;
            targetId = handle.TargetId;
            EnsureInteractionCollider();
        }

        public void Select()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            var handle = Handle;
            if (!handle.IsValid)
            {
                WorldTravelDebugController.SetExternalCharacterStatsDebugLine(
                    $"Targetable {name}: invalid handle, selection ignored.");
                return;
            }

            ClientRuntime.Target.Select(handle);
            WorldTravelDebugController.SetExternalCharacterStatsDebugLine(
                $"Selected target {name}: {handle.Kind}/{handle.TargetId}");
        }

        private void Awake()
        {
            EnsureInteractionCollider();
        }

        private void OnEnable()
        {
            WorldTargetableRegistry.Register(this);
        }

        private void OnDisable()
        {
            WorldTargetableRegistry.Unregister(this);
        }

        public bool TryGetWorldSelectionPosition(out Vector2 position)
        {
            EnsureInteractionCollider();
            if (interactionCollider != null && interactionCollider.enabled)
            {
                position = interactionCollider.bounds.center;
                return true;
            }

            position = transform.position;
            return true;
        }

        public bool TryGetIndicatorAnchorPosition(float additionalHeight, out Vector2 position)
        {
            EnsureInteractionCollider();
            if (interactionCollider != null && interactionCollider.enabled)
            {
                var bounds = interactionCollider.bounds;
                position = new Vector2(bounds.center.x, bounds.max.y + Mathf.Max(0f, additionalHeight));
                return true;
            }

            position = (Vector2)transform.position + new Vector2(0f, Mathf.Max(0f, additionalHeight));
            return true;
        }

        public bool TryBuildFallbackSnapshot(out WorldTargetSnapshot snapshot)
        {
            var handle = Handle;
            if (!handle.IsValid)
            {
                snapshot = default;
                return false;
            }

            snapshot = new WorldTargetSnapshot(
                handle.Kind,
                handle.TargetId,
                DisplayName,
                0,
                0,
                false,
                0,
                0,
                false,
                false);
            return true;
        }

        private void EnsureInteractionCollider()
        {
            if (interactionCollider != null && interactionCollider.enabled)
                return;

            interactionCollider = ResolveEnabledLocalCollider();
            if (interactionCollider != null)
            {
                WorldTravelDebugController.SetExternalCharacterStatsDebugLine(
                    $"Targetable {name}: using existing collider {interactionCollider.GetType().Name}.");
                return;
            }

            if (!autoCreateInteractionCollider)
                return;

            interactionCollider = CreateAutoInteractionCollider();
            WorldTravelDebugController.SetExternalCharacterStatsDebugLine(
                interactionCollider != null
                    ? $"Targetable {name}: created {interactionCollider.GetType().Name}."
                    : $"Targetable {name}: no collider could be created.");
        }

        private Collider2D CreateAutoInteractionCollider()
        {
            var sourceCollider = ResolveSourceCollider();
            if (sourceCollider is BoxCollider2D sourceBox)
            {
                var box = gameObject.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.offset = sourceBox.offset;
                box.size = sourceBox.size + (autoColliderPadding * 2f);
                return box;
            }

            if (sourceCollider is CircleCollider2D sourceCircle)
            {
                var circle = gameObject.AddComponent<CircleCollider2D>();
                circle.isTrigger = true;
                circle.offset = sourceCircle.offset;
                circle.radius = sourceCircle.radius + Mathf.Max(autoColliderPadding.x, autoColliderPadding.y);
                return circle;
            }

            if (sourceCollider is CapsuleCollider2D sourceCapsule)
            {
                var capsule = gameObject.AddComponent<CapsuleCollider2D>();
                capsule.isTrigger = true;
                capsule.offset = sourceCapsule.offset;
                capsule.size = sourceCapsule.size + (autoColliderPadding * 2f);
                capsule.direction = sourceCapsule.direction;
                return capsule;
            }

            if (sourceCollider != null)
            {
                return CreateBoxColliderFromBounds(sourceCollider.bounds);
            }

            Bounds rendererBounds;
            if (TryGetRendererBounds(out rendererBounds))
            {
                return CreateBoxColliderFromBounds(rendererBounds);
            }

            return null;
        }

        private Collider2D ResolveEnabledLocalCollider()
        {
            var colliders = GetComponents<Collider2D>();
            for (var i = 0; i < colliders.Length; i++)
            {
                var candidate = colliders[i];
                if (candidate == null || !candidate.enabled)
                    continue;

                return candidate;
            }

            return null;
        }

        private Collider2D ResolveSourceCollider()
        {
            var playerView = GetComponent<PlayerView>();
            if (playerView != null && playerView.BodyCollider != null)
                return playerView.BodyCollider;

            var colliders = GetComponentsInChildren<Collider2D>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                var candidate = colliders[i];
                if (candidate == null || candidate == interactionCollider)
                    continue;

                return candidate;
            }

            return null;
        }

        private BoxCollider2D CreateBoxColliderFromBounds(Bounds bounds)
        {
            var box = gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger = true;

            var localCenter = transform.InverseTransformPoint(bounds.center);
            box.offset = new Vector2(localCenter.x, localCenter.y);

            var lossyScale = transform.lossyScale;
            var safeScaleX = Mathf.Approximately(lossyScale.x, 0f) ? 1f : Mathf.Abs(lossyScale.x);
            var safeScaleY = Mathf.Approximately(lossyScale.y, 0f) ? 1f : Mathf.Abs(lossyScale.y);
            box.size = new Vector2(
                (bounds.size.x / safeScaleX) + (autoColliderPadding.x * 2f),
                (bounds.size.y / safeScaleY) + (autoColliderPadding.y * 2f));
            return box;
        }

        private bool TryGetRendererBounds(out Bounds bounds)
        {
            bounds = default;
            var renderers = GetComponentsInChildren<Renderer>(true);
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
    }
}
