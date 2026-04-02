using System;
using System.Collections.Generic;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Features.World.Application;
using PhamNhanOnline.Client.UI.Inventory;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    public sealed class WorldGroundRewardPresenter : WorldSceneBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform rewardsRoot;
        [SerializeField] private WorldMapPresenter worldMapPresenter;
        [SerializeField] private WorldTargetActionController worldTargetActionController;
        [SerializeField] private WorldLocalPlayerPresenter worldLocalPlayerPresenter;
        [SerializeField] private InventoryItemPresentationCatalog itemPresentationCatalog;
        [SerializeField] private GameObject rewardVisualPrefab;

        [Header("Visual")]
        [SerializeField] private string sortingLayerName = "Ground";
        [SerializeField] private int sortingOrder = 12;
        [SerializeField] private float iconWorldSize = 0.65f;
        [SerializeField] private float outlineOffsetWorldUnits = 0.025f;
        [SerializeField] private Color outlineColor = Color.black;
        [SerializeField] private float bobAmplitudeWorldUnits = 0.05f;
        [SerializeField] private float bobSpeed = 2.8f;
        [SerializeField] private float verticalOffsetWorldUnits = 0f;
        [SerializeField] private float selectedScaleMultiplier = 1.1f;

        [Header("Ground Snap")]
        [SerializeField] private bool snapToGround = true;
        [SerializeField] private LayerMask groundLayerMask;
        [SerializeField] private float groundProbeHeight = 3f;
        [SerializeField] private float groundProbeDistance = 12f;
        [SerializeField] private float groundContactOffset = 0f;

        [Header("Pickup Animation")]
        [SerializeField] private float pickupMoveDurationSeconds = 0.2f;
        [SerializeField] private float pickupEndScaleMultiplier = 0.1f;

        [Header("Spawn Animation")]
        [SerializeField] private bool playSpawnAnimation = true;
        [SerializeField] private float spawnMoveDurationSeconds = 0.22f;
        [SerializeField] private float spawnArcHeightWorldUnits = 0.35f;
        [SerializeField] private float spawnHorizontalOffsetWorldUnits = 0.3f;

        private readonly Dictionary<int, GroundRewardPresenter> rewardPresenters = new Dictionary<int, GroundRewardPresenter>();
        private readonly HashSet<int> suppressSpawnAnimationRewardIds = new HashSet<int>();
        private bool runtimeEventsBound;
        private bool loggedMissingWorldMapPresenter;
        private bool loggedMissingTargetActionController;

        private void Start()
        {
            AutoWireReferences();
            LogMissingCriticalWorldSceneDependenciesIfNeeded();
            LogMissingCriticalDependenciesIfNeeded();
            ActivateWorldSceneReadiness();
            TryBindRuntimeEvents();
            TrySyncIfReady();
        }

        private void OnEnable()
        {
            AutoWireReferences();
            ActivateWorldSceneReadiness();
            TryBindRuntimeEvents();
            TrySyncIfReady();
        }

        private void OnDisable()
        {
            DeactivateWorldSceneReadiness();
            UnbindRuntimeEvents();
        }

        private void OnDestroy()
        {
            DeactivateWorldSceneReadiness();
            UnbindRuntimeEvents();
            ClearRewards();
        }

        protected override void ConfigureReadyWaits()
        {
            WaitFor(WorldSceneReadyKey.MapVisual, HandleMapVisualReady);
        }

        protected override void OnWorldLoadCycleStarted(int loadVersion, string mapKey)
        {
            suppressSpawnAnimationRewardIds.Clear();
            ClearRewards();
        }

        private void HandleMapVisualReady()
        {
            SyncRewards();
        }

        private void HandleGroundRewardUpserted(GroundRewardModel reward)
        {
            if (!IsReady(WorldSceneReadyKey.MapVisual))
                return;

            var allowSpawnAnimation = playSpawnAnimation && !suppressSpawnAnimationRewardIds.Remove(reward.RewardId);
            UpsertPresenter(reward, allowSpawnAnimation);
        }

        private void HandleGroundRewardRemoved(int rewardId)
        {
            if (!IsReady(WorldSceneReadyKey.MapVisual))
                return;

            RemovePresenter(rewardId);
        }

        private void HandleGroundRewardsChanged()
        {
            if (!IsReady(WorldSceneReadyKey.MapVisual))
                return;

            SyncRewards();
        }

        private void HandleCurrentTargetChanged()
        {
            RefreshSelectionVisuals();
        }

        private void HandleInteractionRequested(PhamNhanOnline.Client.Features.Targeting.Application.WorldTargetHandle handle)
        {
            int rewardId;
            if (!ClientWorldState.TryParseGroundRewardTargetId(handle.TargetId, out rewardId))
                return;

            _ = PickupRewardAsync(rewardId);
        }

        private void HandlePickupSucceeded(GroundRewardPickupResult result)
        {
            GroundRewardPresenter presenter;
            if (!rewardPresenters.TryGetValue(result.RewardId, out presenter) || presenter == null)
                return;

            presenter.BeginPickupAnimation(
                worldLocalPlayerPresenter != null ? worldLocalPlayerPresenter.CurrentPlayerTransform : null,
                pickupMoveDurationSeconds,
                pickupEndScaleMultiplier);
        }

        private void HandleLocalDropToGroundSucceeded(int rewardId)
        {
            suppressSpawnAnimationRewardIds.Add(rewardId);
        }

        private async System.Threading.Tasks.Task PickupRewardAsync(int rewardId)
        {
            if (!ClientRuntime.IsInitialized || ClientRuntime.GroundRewardService == null)
                return;

            try
            {
                var result = await ClientRuntime.GroundRewardService.PickupAsync(rewardId);
                if (!result.Success)
                    ClientLog.Warn($"Ground reward pickup failed for {rewardId}: {result.Code} ({result.Message})");
            }
            catch (Exception ex)
            {
                ClientLog.Warn($"Ground reward pickup exception for {rewardId}: {ex.Message}");
            }
        }

        private void SyncRewards()
        {
            if (!ClientRuntime.IsInitialized || !IsReady(WorldSceneReadyKey.MapVisual))
                return;

            if (string.IsNullOrWhiteSpace(ClientRuntime.World.CurrentClientMapKey))
            {
                ClearRewards();
                return;
            }

            var activeRewardIds = new HashSet<int>();
            foreach (var reward in ClientRuntime.World.GroundRewards)
            {
                activeRewardIds.Add(reward.RewardId);
                UpsertPresenter(reward, false);
            }

            var removedRewardIds = new List<int>();
            foreach (var pair in rewardPresenters)
            {
                if (!activeRewardIds.Contains(pair.Key))
                    removedRewardIds.Add(pair.Key);
            }

            for (var i = 0; i < removedRewardIds.Count; i++)
                RemovePresenter(removedRewardIds[i]);

            RefreshSelectionVisuals();
        }

        private void UpsertPresenter(GroundRewardModel reward, bool allowSpawnAnimation)
        {
            GroundRewardPresenter presenter;
            var isNewPresenter = !rewardPresenters.TryGetValue(reward.RewardId, out presenter) || presenter == null;
            if (isNewPresenter)
            {
                presenter = CreatePresenter(reward);
                if (presenter == null)
                    return;

                rewardPresenters[reward.RewardId] = presenter;
            }

            if (!presenter.gameObject.activeSelf)
                presenter.gameObject.SetActive(true);

            presenter.ApplySnapshot(
                reward,
                worldMapPresenter,
                itemPresentationCatalog,
                sortingLayerName,
                sortingOrder,
                iconWorldSize,
                outlineOffsetWorldUnits,
                outlineColor,
                bobAmplitudeWorldUnits,
                bobSpeed,
                verticalOffsetWorldUnits,
                selectedScaleMultiplier,
                rewardVisualPrefab,
                snapToGround,
                groundLayerMask,
                groundProbeHeight,
                groundProbeDistance,
                groundContactOffset);

            if (isNewPresenter && allowSpawnAnimation)
                presenter.BeginSpawnAnimation(
                    spawnMoveDurationSeconds,
                    spawnArcHeightWorldUnits,
                    ResolveSpawnHorizontalOffset(reward.RewardId));
        }

        private GroundRewardPresenter CreatePresenter(GroundRewardModel reward)
        {
            var parent = rewardsRoot != null
                ? rewardsRoot
                : (SceneController != null && SceneController.EntitiesRoot != null ? SceneController.EntitiesRoot : transform);
            var rewardObject = new GameObject("GroundReward_" + reward.RewardId);
            rewardObject.transform.SetParent(parent, false);
            var presenter = rewardObject.AddComponent<GroundRewardPresenter>();
            return presenter;
        }

        private void RemovePresenter(int rewardId)
        {
            GroundRewardPresenter presenter;
            if (!rewardPresenters.TryGetValue(rewardId, out presenter))
                return;

            rewardPresenters.Remove(rewardId);
            if (presenter != null)
                presenter.MarkPendingDestroy();
        }

        private void ClearRewards()
        {
            foreach (var pair in rewardPresenters)
            {
                if (pair.Value != null)
                    Destroy(pair.Value.gameObject);
            }

            rewardPresenters.Clear();
        }

        private void RefreshSelectionVisuals()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            var currentTarget = ClientRuntime.Target.CurrentTarget;
            foreach (var pair in rewardPresenters)
            {
                var presenter = pair.Value;
                if (presenter == null)
                    continue;

                var isSelected = currentTarget.HasValue &&
                                 currentTarget.Value.Kind == PhamNhanOnline.Client.Features.Targeting.Application.WorldTargetKind.GroundReward &&
                                 ClientWorldState.TryParseGroundRewardTargetId(currentTarget.Value.TargetId, out var rewardId) &&
                                 rewardId == pair.Key;
                presenter.SetSelected(isSelected);
            }
        }

        private void TrySyncIfReady()
        {
            if (!IsReady(WorldSceneReadyKey.MapVisual))
                return;

            SyncRewards();
        }

        private void TryBindRuntimeEvents()
        {
            AutoWireReferences();
            if (runtimeEventsBound || !ClientRuntime.IsInitialized || worldTargetActionController == null)
                return;

            ClientRuntime.World.GroundRewardUpserted += HandleGroundRewardUpserted;
            ClientRuntime.World.GroundRewardRemoved += HandleGroundRewardRemoved;
            ClientRuntime.World.GroundRewardsChanged += HandleGroundRewardsChanged;
            ClientRuntime.Target.CurrentTargetChanged += HandleCurrentTargetChanged;
            if (ClientRuntime.GroundRewardService != null)
                ClientRuntime.GroundRewardService.PickupSucceeded += HandlePickupSucceeded;
            if (ClientRuntime.InventoryService != null)
                ClientRuntime.InventoryService.DropToGroundSucceeded += HandleLocalDropToGroundSucceeded;
            worldTargetActionController.InteractionRequested += HandleInteractionRequested;
            runtimeEventsBound = true;
        }

        private void UnbindRuntimeEvents()
        {
            if (!runtimeEventsBound || !ClientRuntime.IsInitialized)
                return;

            ClientRuntime.World.GroundRewardUpserted -= HandleGroundRewardUpserted;
            ClientRuntime.World.GroundRewardRemoved -= HandleGroundRewardRemoved;
            ClientRuntime.World.GroundRewardsChanged -= HandleGroundRewardsChanged;
            ClientRuntime.Target.CurrentTargetChanged -= HandleCurrentTargetChanged;
            if (ClientRuntime.GroundRewardService != null)
                ClientRuntime.GroundRewardService.PickupSucceeded -= HandlePickupSucceeded;
            if (ClientRuntime.InventoryService != null)
                ClientRuntime.InventoryService.DropToGroundSucceeded -= HandleLocalDropToGroundSucceeded;
            if (worldTargetActionController != null)
                worldTargetActionController.InteractionRequested -= HandleInteractionRequested;
            runtimeEventsBound = false;
        }

        private void AutoWireReferences()
        {
            InitializeWorldSceneBehaviour(ref worldMapPresenter);

            if (worldTargetActionController == null)
                worldTargetActionController = GetComponent<WorldTargetActionController>();

            if (worldLocalPlayerPresenter == null)
                worldLocalPlayerPresenter = SceneController != null ? SceneController.WorldLocalPlayerPresenter : GetComponent<WorldLocalPlayerPresenter>();
        }

        private void LogMissingCriticalDependenciesIfNeeded()
        {
            if (worldMapPresenter == null && !loggedMissingWorldMapPresenter)
            {
                ClientLog.Error("WorldGroundRewardPresenter could not resolve WorldMapPresenter.");
                loggedMissingWorldMapPresenter = true;
            }

            if (worldTargetActionController == null && !loggedMissingTargetActionController)
            {
                ClientLog.Error("WorldGroundRewardPresenter could not resolve WorldTargetActionController.");
                loggedMissingTargetActionController = true;
            }
        }

        private float ResolveSpawnHorizontalOffset(int rewardId)
        {
            var magnitude = Mathf.Max(0f, spawnHorizontalOffsetWorldUnits);
            if (magnitude <= Mathf.Epsilon)
                return 0f;

            return (rewardId & 1) == 0 ? magnitude : -magnitude;
        }
    }
}


