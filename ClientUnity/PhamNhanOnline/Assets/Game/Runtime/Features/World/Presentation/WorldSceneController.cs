using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
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
        [SerializeField] private WorldLocalPlayerPresenter worldLocalPlayerPresenter;
        [SerializeField] private WorldLocalMovementSyncController worldLocalMovementSyncController;

        public Transform MapRoot { get { return mapRoot; } }
        public Transform EntitiesRoot { get { return entitiesRoot; } }
        public Transform WorldUiRoot { get { return worldUiRoot; } }
        public Camera WorldCamera { get { return worldCamera; } }
        public WorldMapPresenter WorldMapPresenter { get { return worldMapPresenter; } }
        public WorldLocalPlayerPresenter WorldLocalPlayerPresenter { get { return worldLocalPlayerPresenter; } }
        public WorldLocalMovementSyncController WorldLocalMovementSyncController { get { return worldLocalMovementSyncController; } }

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
            EnsureClientPoolService();
            EnsureWorldTargetSelectionController();
            EnsureWorldTargetActionController();

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

        private void AutoWireReferences()
        {
            if (worldMapPresenter == null)
                worldMapPresenter = GetComponent<WorldMapPresenter>();

            if (worldLocalPlayerPresenter == null)
                worldLocalPlayerPresenter = GetComponent<WorldLocalPlayerPresenter>();

            if (worldLocalMovementSyncController == null)
                worldLocalMovementSyncController = GetComponent<WorldLocalMovementSyncController>();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
