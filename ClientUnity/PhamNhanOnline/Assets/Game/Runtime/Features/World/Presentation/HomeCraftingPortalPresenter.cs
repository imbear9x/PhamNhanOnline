using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Features.Targeting.Application;
using PhamNhanOnline.Client.UI.World;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    [DisallowMultipleComponent]
    public sealed class HomeCraftingPortalPresenter : WorldSceneBehaviour
    {
        private const string PortalTargetId = "local-home-crafting-portal";
        [Header("References")]
        [SerializeField] private WorldTargetable worldTargetable;
        [SerializeField] private Collider2D interactionCollider;

        [Header("Config")]
        [SerializeField] private bool hideWhenOutsidePrivateHome = true;
        [SerializeField] private bool hideCraftingPanelWhenLeavingPrivateHome = true;

        private WorldTargetHandle PortalHandle => new WorldTargetHandle(WorldTargetKind.Npc, PortalTargetId);

        private void Awake()
        {
            AutoWireReferences();
            ConfigureTargetable();
        }

        private void Start()
        {
            AutoWireReferences();
            ConfigureTargetable();
            InitializeWorldSceneBehaviour();
            ActivateWorldSceneReadiness();
            TryBindRuntimeEvents();
            RefreshPortalAvailability();
        }

        private void OnEnable()
        {
            AutoWireReferences();
            ConfigureTargetable();
            InitializeWorldSceneBehaviour();
            ActivateWorldSceneReadiness();
            TryBindRuntimeEvents();
            RefreshPortalAvailability();
        }

        private void OnDisable()
        {
            UnbindRuntimeEvents();
            DeactivateWorldSceneReadiness();
        }

        private void OnDestroy()
        {
            UnbindRuntimeEvents();
            DeactivateWorldSceneReadiness();
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

            if (!ClientRuntime.World.CurrentMapIsPrivatePerPlayer &&
                hideCraftingPanelWhenLeavingPrivateHome &&
                WorldUiController.Instance != null)
            {
                WorldUiController.Instance.HideCraftingPanelIfVisible();
            }
        }

        private void HandleInteractionRequested(WorldTargetHandle handle)
        {
            if (!handle.Equals(PortalHandle))
                return;

            if (!ClientRuntime.IsInitialized || !ClientRuntime.World.CurrentMapIsPrivatePerPlayer)
                return;

            if (WorldUiController.Instance != null)
                WorldUiController.Instance.ShowCraftingPanel();
        }

        private void HandleMapChanged()
        {
            RefreshPortalAvailability();
        }

        private void TryBindRuntimeEvents()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            ClientRuntime.World.MapChanged -= HandleMapChanged;
            ClientRuntime.World.MapChanged += HandleMapChanged;

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
        }

        private void ConfigureTargetable()
        {
            if (worldTargetable == null)
                return;

            worldTargetable.Configure(PortalHandle);
        }

        private void RefreshPortalAvailability()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            var shouldBeVisible = !hideWhenOutsidePrivateHome || ClientRuntime.World.CurrentMapIsPrivatePerPlayer;
            if (gameObject.activeSelf != shouldBeVisible)
                gameObject.SetActive(shouldBeVisible);
        }
    }
}
