using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Infrastructure.Config;
using PhamNhanOnline.Client.Infrastructure.Pooling;
using System.Collections.Generic;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    public sealed class WorldSceneController : MonoBehaviour
    {
        public static WorldSceneController Instance { get; private set; }
        private bool loggedMissingSceneRoots;
        private bool loggedMissingWorldCamera;
        private bool loggedMissingMapPresenter;
        private bool loggedMissingLocalPlayerPresenter;
        private bool loggedMissingLocalMovementSyncController;
        private bool loggedMissingClickTargetSelectionController;
        private bool loggedMissingAutoTargetSelectionController;
        private bool loggedMissingTargetLifecycleController;
        private bool loggedMissingTargetActionController;
        private bool loggedMissingPortalPresenter;
        private bool loggedMissingGroundRewardPresenter;

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
        [SerializeField] private WorldRemotePlayersPresenter worldRemotePlayersPresenter;
        [SerializeField] private WorldEnemiesPresenter worldEnemiesPresenter;
        [SerializeField] private WorldClickTargetSelectionController worldClickTargetSelectionController;
        [SerializeField] private WorldAutoTargetSelectionController worldAutoTargetSelectionController;
        [SerializeField] private WorldTargetLifecycleController worldTargetLifecycleController;
        [SerializeField] private WorldTargetActionController worldTargetActionController;
        [SerializeField] private WorldTargetSelectionIndicatorController worldTargetSelectionIndicatorController;
        [SerializeField] private WorldPortalPresenter worldPortalPresenter;
        [SerializeField] private WorldGroundRewardPresenter worldGroundRewardPresenter;

        public Transform MapRoot { get { return mapRoot; } }
        public Transform EntitiesRoot { get { return entitiesRoot; } }
        public Transform WorldUIRoot { get { return worldUiRoot; } }
        public Camera WorldCamera { get { return worldCamera; } }
        public WorldMapPresenter WorldMapPresenter { get { return worldMapPresenter; } }
        public WorldSceneReadinessService WorldSceneReadinessService { get { return worldSceneReadinessService; } }
        public WorldLocalPlayerPresenter WorldLocalPlayerPresenter { get { return worldLocalPlayerPresenter; } }
        public WorldLocalMovementSyncController WorldLocalMovementSyncController { get { return worldLocalMovementSyncController; } }
        public WorldRemotePlayersPresenter WorldRemotePlayersPresenter { get { return worldRemotePlayersPresenter; } }
        public WorldEnemiesPresenter WorldEnemiesPresenter { get { return worldEnemiesPresenter; } }
        public WorldClickTargetSelectionController WorldClickTargetSelectionController { get { return worldClickTargetSelectionController; } }
        public WorldAutoTargetSelectionController WorldAutoTargetSelectionController { get { return worldAutoTargetSelectionController; } }
        public WorldTargetLifecycleController WorldTargetLifecycleController { get { return worldTargetLifecycleController; } }
        public WorldTargetActionController WorldTargetActionController { get { return worldTargetActionController; } }
        public WorldTargetSelectionIndicatorController WorldTargetSelectionIndicatorController { get { return worldTargetSelectionIndicatorController; } }
        public WorldPortalPresenter WorldPortalPresenter { get { return worldPortalPresenter; } }
        public WorldGroundRewardPresenter WorldGroundRewardPresenter { get { return worldGroundRewardPresenter; } }

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
            EnsureRuntimeInitialized();
            ValidateRequiredReferences();
            if (worldMapPresenter != null)
                worldMapPresenter.Initialize(this, worldSceneReadinessService);
            EnsureClientPoolService();
            InjectSceneContext();
        }

        private void Start()
        {
            LogMissingCriticalSceneRefsIfNeeded();
        }

        public void CycleNearbyTarget()
        {
            var controller = worldAutoTargetSelectionController;
            if (controller != null)
                controller.CycleNearbyTarget();
        }

        public void ClearSelectedTarget()
        {
            var controller = worldAutoTargetSelectionController;
            if (controller != null)
                controller.ClearSelectedTarget();
        }

        public void PinCurrentTargetForCombat()
        {
            var controller = worldAutoTargetSelectionController;
            if (controller != null)
                controller.PinCurrentTargetForCombat();
        }

        public void PinCurrentTargetManually()
        {
            var controller = worldAutoTargetSelectionController;
            if (controller != null)
                controller.PinCurrentTargetManually();
        }

        public void ClearPinnedTarget()
        {
            var controller = worldAutoTargetSelectionController;
            if (controller != null)
                controller.ClearPinnedTarget();
        }

        public bool RequestPrimaryTargetAction(PhamNhanOnline.Client.Features.Targeting.Application.WorldTargetHandle target)
        {
            var controller = worldTargetActionController;
            return controller != null && controller.RequestPrimaryAction(target);
        }

        public bool RequestPrimaryActionForCurrentSelection()
        {
            if (!ClientRuntime.IsInitialized)
                return false;

            var currentTarget = ClientRuntime.Target.CurrentTarget;
            if (!currentTarget.HasValue || !currentTarget.Value.IsValid)
                return false;

            return RequestPrimaryTargetAction(currentTarget.Value);
        }

        public WorldTargetActionController TryResolveWorldTargetActionController()
        {
            return worldTargetActionController;
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

        private void InjectSceneContext()
        {
            var context = new WorldSceneContext
            {
                SceneController = this,
                WorldMapPresenter = worldMapPresenter,
                WorldSceneReadinessService = worldSceneReadinessService,
                WorldLocalPlayerPresenter = worldLocalPlayerPresenter,
                WorldLocalMovementSyncController = worldLocalMovementSyncController,
                WorldRemotePlayersPresenter = worldRemotePlayersPresenter,
                WorldEnemiesPresenter = worldEnemiesPresenter,
                WorldClickTargetSelectionController = worldClickTargetSelectionController,
                WorldAutoTargetSelectionController = worldAutoTargetSelectionController,
                WorldTargetLifecycleController = worldTargetLifecycleController,
                WorldTargetActionController = worldTargetActionController,
                WorldTargetSelectionIndicatorController = worldTargetSelectionIndicatorController,
                WorldPortalPresenter = worldPortalPresenter,
                WorldGroundRewardPresenter = worldGroundRewardPresenter,
                WorldCamera = worldCamera,
                MapRoot = mapRoot,
                EntitiesRoot = entitiesRoot,
                WorldUiRoot = worldUiRoot
            };

            var injectedReceivers = new HashSet<IWorldSceneContextReceiver>();
            InjectSceneContextIntoHierarchy(injectedReceivers, transform, context);
        }

        public void InjectSceneContextIntoHierarchy(Transform root)
        {
            if (root == null)
                return;

            var context = new WorldSceneContext
            {
                SceneController = this,
                WorldMapPresenter = worldMapPresenter,
                WorldSceneReadinessService = worldSceneReadinessService,
                WorldLocalPlayerPresenter = worldLocalPlayerPresenter,
                WorldLocalMovementSyncController = worldLocalMovementSyncController,
                WorldRemotePlayersPresenter = worldRemotePlayersPresenter,
                WorldEnemiesPresenter = worldEnemiesPresenter,
                WorldClickTargetSelectionController = worldClickTargetSelectionController,
                WorldAutoTargetSelectionController = worldAutoTargetSelectionController,
                WorldTargetLifecycleController = worldTargetLifecycleController,
                WorldTargetActionController = worldTargetActionController,
                WorldTargetSelectionIndicatorController = worldTargetSelectionIndicatorController,
                WorldPortalPresenter = worldPortalPresenter,
                WorldGroundRewardPresenter = worldGroundRewardPresenter,
                WorldCamera = worldCamera,
                MapRoot = mapRoot,
                EntitiesRoot = entitiesRoot,
                WorldUiRoot = worldUiRoot
            };

            var injectedReceivers = new HashSet<IWorldSceneContextReceiver>();
            InjectSceneContextIntoHierarchy(injectedReceivers, root, context);
        }

        private static void InjectSceneContextIntoHierarchy(
            HashSet<IWorldSceneContextReceiver> injectedReceivers,
            Transform root,
            WorldSceneContext context)
        {
            var receiverBehaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (var i = 0; i < receiverBehaviours.Length; i++)
                InjectReceiver(injectedReceivers, receiverBehaviours[i], context);
        }

        private static void InjectReceiver(
            HashSet<IWorldSceneContextReceiver> injectedReceivers,
            MonoBehaviour behaviour,
            WorldSceneContext context)
        {
            if (behaviour is not IWorldSceneContextReceiver receiver || !injectedReceivers.Add(receiver))
                return;

            receiver.InitializeWorldSceneContext(context);
        }

        private void ValidateRequiredReferences()
        {
            if (worldSceneReadinessService == null)
                ClientLog.Error("WorldSceneController is missing WorldSceneReadinessService. Assign it explicitly.");

            if (worldClickTargetSelectionController == null && !loggedMissingClickTargetSelectionController)
            {
                ClientLog.Error("WorldSceneController is missing WorldClickTargetSelectionController. Assign it explicitly.");
                loggedMissingClickTargetSelectionController = true;
            }

            if (worldAutoTargetSelectionController == null && !loggedMissingAutoTargetSelectionController)
            {
                ClientLog.Error("WorldSceneController is missing WorldAutoTargetSelectionController. Assign it explicitly.");
                loggedMissingAutoTargetSelectionController = true;
            }

            if (worldTargetLifecycleController == null && !loggedMissingTargetLifecycleController)
            {
                ClientLog.Error("WorldSceneController is missing WorldTargetLifecycleController. Assign it explicitly.");
                loggedMissingTargetLifecycleController = true;
            }

            if (worldTargetActionController == null && !loggedMissingTargetActionController)
            {
                ClientLog.Error("WorldSceneController is missing WorldTargetActionController. Assign it explicitly.");
                loggedMissingTargetActionController = true;
            }

            if (worldPortalPresenter == null && !loggedMissingPortalPresenter)
            {
                ClientLog.Error("WorldSceneController is missing WorldPortalPresenter. Assign it explicitly.");
                loggedMissingPortalPresenter = true;
            }

            if (worldGroundRewardPresenter == null && !loggedMissingGroundRewardPresenter)
            {
                ClientLog.Error("WorldSceneController is missing WorldGroundRewardPresenter. Assign it explicitly.");
                loggedMissingGroundRewardPresenter = true;
            }
        }

        private void LogMissingCriticalSceneRefsIfNeeded()
        {
            if ((mapRoot == null || entitiesRoot == null || worldUiRoot == null) && !loggedMissingSceneRoots)
            {
                ClientLog.Error("WorldSceneController is missing one or more scene roots: MapRoot, EntitiesRoot, or WorldUIRoot.");
                loggedMissingSceneRoots = true;
            }

            if (worldCamera == null && !loggedMissingWorldCamera)
            {
                ClientLog.Error("WorldSceneController is missing World Camera.");
                loggedMissingWorldCamera = true;
            }

            if (worldMapPresenter == null && !loggedMissingMapPresenter)
            {
                ClientLog.Error("WorldSceneController could not resolve WorldMapPresenter.");
                loggedMissingMapPresenter = true;
            }

            if (worldLocalPlayerPresenter == null && !loggedMissingLocalPlayerPresenter)
            {
                ClientLog.Error("WorldSceneController could not resolve WorldLocalPlayerPresenter.");
                loggedMissingLocalPlayerPresenter = true;
            }

            if (worldLocalMovementSyncController == null && !loggedMissingLocalMovementSyncController)
            {
                ClientLog.Error("WorldSceneController could not resolve WorldLocalMovementSyncController.");
                loggedMissingLocalMovementSyncController = true;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

    }
}
