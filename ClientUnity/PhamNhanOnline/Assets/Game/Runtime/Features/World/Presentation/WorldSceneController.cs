using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Infrastructure.Config;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    public sealed class WorldSceneController : MonoBehaviour
    {
        [Header("Runtime")]
        [SerializeField] private ClientBootstrapSettings runtimeSettingsOverride;

        [SerializeField] private Transform mapRoot;
        [SerializeField] private Transform entitiesRoot;
        [SerializeField] private Transform worldUiRoot;
        [SerializeField] private Camera worldCamera;

        public Transform MapRoot { get { return mapRoot; } }
        public Transform EntitiesRoot { get { return entitiesRoot; } }
        public Transform WorldUiRoot { get { return worldUiRoot; } }
        public Camera WorldCamera { get { return worldCamera; } }

        private void Awake()
        {
            EnsureRuntimeInitialized();
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

        private WorldClickTargetSelectionController EnsureWorldTargetSelectionController()
        {
            var controller = GetComponent<WorldClickTargetSelectionController>();
            if (controller == null)
                controller = gameObject.AddComponent<WorldClickTargetSelectionController>();

            controller.Initialize(worldCamera, GetComponentInChildren<WorldMapPresenter>());
            return controller;
        }

        private WorldTargetActionController EnsureWorldTargetActionController()
        {
            var controller = GetComponent<WorldTargetActionController>();
            if (controller == null)
                controller = gameObject.AddComponent<WorldTargetActionController>();

            return controller;
        }
    }
}
