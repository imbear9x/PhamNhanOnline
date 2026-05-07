using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
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
        [SerializeField] private bool autoCreateInteractionCollider = false;
        [SerializeField] private Vector2 autoColliderPadding = new Vector2(0.15f, 0.15f);
        private bool loggedMissingInteractionCollider;

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
            ValidateInteractionCollider();
        }

        public void Configure(WorldTargetHandle handle, string displayName)
        {
            targetKind = handle.Kind;
            targetId = handle.TargetId;
            displayNameOverride = displayName ?? string.Empty;
            ValidateInteractionCollider();
        }

        public void BindInteractionCollider(Collider2D collider)
        {
            interactionCollider = collider;
            ValidateInteractionCollider();
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
            ValidateInteractionCollider();
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
            ValidateInteractionCollider();
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
            ValidateInteractionCollider();
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

        private void ValidateInteractionCollider()
        {
            if (interactionCollider != null && interactionCollider.enabled)
            {
                loggedMissingInteractionCollider = false;
                return;
            }

            if (loggedMissingInteractionCollider)
                return;

            ClientLog.Error(
                $"WorldTargetable on '{name}' is missing an enabled interactionCollider. " +
                $"autoCreateInteractionCollider={autoCreateInteractionCollider} is ignored by client project rule; " +
                "assign a prefab/scene collider or bind one explicitly from the runtime owner.");
            WorldTravelDebugController.SetExternalCharacterStatsDebugLine(
                $"Targetable {name}: missing interaction collider.");
            loggedMissingInteractionCollider = true;
        }
    }
}
