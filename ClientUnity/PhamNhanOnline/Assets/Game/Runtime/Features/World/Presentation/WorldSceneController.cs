using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Features.Combat.Presentation;
using PhamNhanOnline.Client.Infrastructure.Config;
using PhamNhanOnline.Client.Infrastructure.Pooling;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    public sealed class WorldSceneController : MonoBehaviour
    {
        public static WorldSceneController Instance { get; private set; }

        [Header("Runtime")]
        [SerializeField] private ClientBootstrapSettings runtimeSettingsOverride;

        [SerializeField] private Transform mapRoot;
        [SerializeField] private Transform entitiesRoot;
        [SerializeField] private Transform worldUiRoot;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private WorldMapPresenter worldMapPresenter;
        [SerializeField] private WorldSceneReadinessService worldSceneReadinessService;
        [SerializeField] private WorldLocalPlayerPresenter worldLocalPlayerPresenter;
        [SerializeField] private WorldLocalMovementSyncController worldLocalMovementSyncController;
        [SerializeField] private SkillWorldPresentationCatalog skillWorldPresentationCatalog;

        public Transform MapRoot { get { return mapRoot; } }
        public Transform EntitiesRoot { get { return entitiesRoot; } }
        public Transform WorldUiRoot { get { return worldUiRoot; } }
        public Camera WorldCamera { get { return worldCamera; } }
        public WorldMapPresenter WorldMapPresenter { get { return worldMapPresenter; } }
        public WorldSceneReadinessService WorldSceneReadinessService { get { return worldSceneReadinessService; } }
        public WorldLocalPlayerPresenter WorldLocalPlayerPresenter { get { return worldLocalPlayerPresenter; } }
        public WorldLocalMovementSyncController WorldLocalMovementSyncController { get { return worldLocalMovementSyncController; } }
        public SkillWorldPresentationCatalog SkillWorldPresentationCatalog { get { return skillWorldPresentationCatalog; } }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning(
                    $"Duplicate WorldSceneController detected on '{gameObject.name}'. " +
                    $"Keeping '{Instance.gameObject.name}' and disabling this component.");
                enabled = false;
                return;
            }

            Instance = this;
            AutoWireReferences();
            EnsureRuntimeInitialized();
            EnsureWorldSceneReadinessService();
            ConfigureSkillPresentation();
            EnsureClientPoolService();
            EnsureWorldTargetSelectionController();
            EnsureWorldTargetActionController();
            EnsureWorldPortalPresenter();
            EnsureWorldGroundRewardPresenter();

            if (mapRoot == null || entitiesRoot == null || worldUiRoot == null)
                Debug.LogWarning("WorldSceneController is missing one or more scene roots.");
        }

        public void CycleNearbyTarget()
        {
            var controller = EnsureWorldTargetSelectionController();
            if (controller != null)
                controller.CycleNearbyTarget();
        }

        public void ClearSelectedTarget()
        {
            var controller = EnsureWorldTargetSelectionController();
            if (controller != null)
                controller.ClearSelectedTarget();
        }

        public void PinCurrentTargetForCombat()
        {
            var controller = EnsureWorldTargetSelectionController();
            if (controller != null)
                controller.PinCurrentTargetForCombat();
        }

        public void PinCurrentTargetManually()
        {
            var controller = EnsureWorldTargetSelectionController();
            if (controller != null)
                controller.PinCurrentTargetManually();
        }

        public void ClearPinnedTarget()
        {
            var controller = EnsureWorldTargetSelectionController();
            if (controller != null)
                controller.ClearPinnedTarget();
        }

        public bool RequestPrimaryTargetAction(PhamNhanOnline.Client.Features.Targeting.Application.WorldTargetHandle target)
        {
            var controller = EnsureWorldTargetActionController();
            return controller != null && controller.RequestPrimaryAction(target);
        }

        private void Update()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            ClientRuntime.SkillPresentationService.ConfigureCatalog(skillWorldPresentationCatalog);
            ClientRuntime.SkillPresentationService.Tick(System.DateTime.UtcNow);
        }

        private void EnsureRuntimeInitialized()
        {
            if (ClientRuntime.IsInitialized)
                return;

            var settings = runtimeSettingsOverride != null
                ? runtimeSettingsOverride
                : ClientBootstrapSettings.CreateRuntimeDefaults();

            ClientRuntime.Initialize(settings);
            ClientLog.Info($"Client runtime auto-initialized from World scene using {settings.name}.");
        }

        private ClientPoolService EnsureClientPoolService()
        {
            return ClientPoolService.Ensure(transform);
        }

        private WorldSceneReadinessService EnsureWorldSceneReadinessService()
        {
            if (worldSceneReadinessService == null)
                worldSceneReadinessService = GetComponent<WorldSceneReadinessService>();

            if (worldSceneReadinessService == null)
                worldSceneReadinessService = gameObject.AddComponent<WorldSceneReadinessService>();

            return worldSceneReadinessService;
        }

        private void ConfigureSkillPresentation()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            ClientRuntime.SkillPresentationService.ConfigureCatalog(skillWorldPresentationCatalog);
        }

        private WorldClickTargetSelectionController EnsureWorldTargetSelectionController()
        {
            var controller = GetComponent<WorldClickTargetSelectionController>();
            if (controller == null)
                controller = gameObject.AddComponent<WorldClickTargetSelectionController>();

            controller.Initialize(worldCamera, worldMapPresenter);
            return controller;
        }

        private WorldTargetActionController EnsureWorldTargetActionController()
        {
            var controller = GetComponent<WorldTargetActionController>();
            if (controller == null)
                controller = gameObject.AddComponent<WorldTargetActionController>();

            return controller;
        }

        private WorldPortalPresenter EnsureWorldPortalPresenter()
        {
            var presenter = GetComponent<WorldPortalPresenter>();
            if (presenter == null)
                presenter = gameObject.AddComponent<WorldPortalPresenter>();

            return presenter;
        }

        private WorldGroundRewardPresenter EnsureWorldGroundRewardPresenter()
        {
            var presenter = GetComponent<WorldGroundRewardPresenter>();
            if (presenter == null)
                presenter = gameObject.AddComponent<WorldGroundRewardPresenter>();

            return presenter;
        }

        private void AutoWireReferences()
        {
            if (worldMapPresenter == null)
                worldMapPresenter = GetComponent<WorldMapPresenter>();

            if (worldSceneReadinessService == null)
                worldSceneReadinessService = GetComponent<WorldSceneReadinessService>();

            if (worldLocalPlayerPresenter == null)
                worldLocalPlayerPresenter = GetComponent<WorldLocalPlayerPresenter>();

            if (worldLocalMovementSyncController == null)
                worldLocalMovementSyncController = GetComponent<WorldLocalMovementSyncController>();
        }

        private void OnDestroy()
        {
            if (ClientRuntime.IsInitialized)
                ClientRuntime.SkillPresentationService.Clear();

            if (Instance == this)
                Instance = null;
        }
    }
}
