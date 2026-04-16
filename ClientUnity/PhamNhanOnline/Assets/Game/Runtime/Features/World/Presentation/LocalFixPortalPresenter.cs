using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Features.Targeting.Application;
using PhamNhanOnline.Client.UI.World;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    [DisallowMultipleComponent]
    public sealed class LocalFixPortalPresenter : WorldSceneBehaviour
    {
        private static readonly System.Collections.Generic.HashSet<LocalFixPortalPresenter> Registered =
            new System.Collections.Generic.HashSet<LocalFixPortalPresenter>();

        private const string AlchemyPortalTargetId = "local-home-crafting-portal";
        private const string SmithingPortalTargetId = "local-home-smithing-portal";
        private const string TalismanPortalTargetId = "local-home-talisman-portal";

        [Header("References")]
        [SerializeField] private WorldTargetable worldTargetable;
        [SerializeField] private Collider2D interactionCollider;
        [SerializeField] private PortalVisualInstance portalVisualInstance;

        [Header("Config")]
        [SerializeField] private CraftingStationType stationType = CraftingStationType.Alchemy;
        [SerializeField] private string panelTitleOverride;

        private WorldTargetHandle PortalHandle => new WorldTargetHandle(WorldTargetKind.Npc, ResolvePortalTargetId());

        private void Awake()
        {
            AutoWireReferences();
            ConfigureTargetable();
            ApplyTargetableLayer();
        }

        private void Start()
        {
            AutoWireReferences();
            ConfigureTargetable();
            ApplyTargetableLayer();
            InitializeWorldSceneBehaviour();
            ActivateWorldSceneReadiness();
            TryBindRuntimeEvents();
            RefreshPortalAvailability();
        }

        private void OnEnable()
        {
            Registered.Add(this);
            AutoWireReferences();
            ConfigureTargetable();
            ApplyTargetableLayer();
            InitializeWorldSceneBehaviour();
            ActivateWorldSceneReadiness();
            TryBindRuntimeEvents();
            RefreshPortalAvailability();
        }

        private void OnDisable()
        {
            Registered.Remove(this);
            UnbindRuntimeEvents();
            RefreshSelectionVisual();
            DeactivateWorldSceneReadiness();
        }

        private void OnDestroy()
        {
            Registered.Remove(this);
            UnbindRuntimeEvents();
            DeactivateWorldSceneReadiness();
        }

        public static bool TryResolveActionWorldPosition(WorldTargetHandle handle, out Vector2 worldPosition)
        {
            foreach (var presenter in Registered)
            {
                if (presenter == null || !presenter.isActiveAndEnabled)
                    continue;

                if (!presenter.PortalHandle.Equals(handle))
                    continue;

                var position = presenter.transform.position;
                worldPosition = new Vector2(position.x, position.y);
                return true;
            }

            worldPosition = default;
            return false;
        }

        protected override void OnWorldReadyReported(int loadVersion, WorldSceneReadyKey key)
        {
            if (key == WorldSceneReadyKey.MapVisual)
                RefreshPortalAvailability();
        }

        private void Update()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            RefreshPortalAvailability();
        }

        private void HandleInteractionRequested(WorldTargetHandle handle)
        {
            if (!handle.Equals(PortalHandle))
                return;

            if (!ClientRuntime.IsInitialized)
                return;

            if (WorldUIController.Instance != null)
            {
                WorldUIController.Instance.ShowCraftingPanel(stationType, panelTitleOverride);
                return;
            }
        }

        private void HandleMapChanged()
        {
            RefreshPortalAvailability();
            RefreshSelectionVisual();
        }

        private void HandleCurrentTargetChanged()
        {
            RefreshSelectionVisual();
        }

        private void TryBindRuntimeEvents()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            ClientRuntime.World.MapChanged -= HandleMapChanged;
            ClientRuntime.World.MapChanged += HandleMapChanged;
            ClientRuntime.Target.CurrentTargetChanged -= HandleCurrentTargetChanged;
            ClientRuntime.Target.CurrentTargetChanged += HandleCurrentTargetChanged;

            var worldTargetActionController = WorldSceneController.Instance != null
                ? WorldSceneController.Instance.TryResolveWorldTargetActionController()
                : null;
            if (worldTargetActionController != null)
            {
                worldTargetActionController.InteractionRequested -= HandleInteractionRequested;
                worldTargetActionController.InteractionRequested += HandleInteractionRequested;
            }
        }

        private void UnbindRuntimeEvents()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            ClientRuntime.World.MapChanged -= HandleMapChanged;
            ClientRuntime.Target.CurrentTargetChanged -= HandleCurrentTargetChanged;
            var worldTargetActionController = WorldSceneController.Instance != null
                ? WorldSceneController.Instance.TryResolveWorldTargetActionController()
                : null;
            if (worldTargetActionController != null)
                worldTargetActionController.InteractionRequested -= HandleInteractionRequested;
        }

        private void AutoWireReferences()
        {
            if (worldTargetable == null)
                worldTargetable = GetComponent<WorldTargetable>();

            if (interactionCollider == null)
                interactionCollider = GetComponent<Collider2D>();

            if (portalVisualInstance == null)
                portalVisualInstance = GetComponent<PortalVisualInstance>();
        }

        private void ConfigureTargetable()
        {
            if (worldTargetable == null)
                return;

            worldTargetable.Configure(PortalHandle);
        }

        private void ApplyTargetableLayer()
        {
            var targetableLayer = ResolveTargetableLayer();
            if (targetableLayer < 0)
                return;

            if (worldTargetable != null)
                SetLayerRecursively(worldTargetable.gameObject, targetableLayer);

            if (interactionCollider != null)
                SetLayerRecursively(interactionCollider.gameObject, targetableLayer);
        }

        private string ResolvePortalTargetId()
        {
            switch (stationType)
            {
                case CraftingStationType.Smithing:
                    return SmithingPortalTargetId;
                case CraftingStationType.Talisman:
                    return TalismanPortalTargetId;
                default:
                    return AlchemyPortalTargetId;
            }
        }

        private void RefreshPortalAvailability()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            RefreshSelectionVisual();
        }

        private void RefreshSelectionVisual()
        {
            if (portalVisualInstance == null)
                return;

            var isSelected = ClientRuntime.IsInitialized &&
                             ClientRuntime.Target.CurrentTarget.HasValue &&
                             ClientRuntime.Target.CurrentTarget.Value.Equals(PortalHandle);
            portalVisualInstance.SetSelected(isSelected);
        }

        private static int ResolveTargetableLayer()
        {
            return LayerMask.NameToLayer("Targetable");
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            if (root == null || layer < 0)
                return;

            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
                transforms[i].gameObject.layer = layer;
        }
    }
}
