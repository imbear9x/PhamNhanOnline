using System;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Features.Combat.Presentation;
using PhamNhanOnline.Client.Features.Character.Presentation;
using PhamNhanOnline.Client.Features.Character.Application;
using PhamNhanOnline.Client.Features.Targeting.Application;
using PhamNhanOnline.Client.UI.World;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    public sealed class WorldLocalPlayerPresenter : WorldSceneBehaviour
    {
        private const float DefaultBoundsPadding = 0.1f;

        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform localPlayerRoot;
        [SerializeField] private WorldMapPresenter worldMapPresenter;
        [SerializeField] private LocalCharacterActionConfig localCharacterActionConfig;
        [SerializeField] private float authoritativeSnapDistance = 1.5f;

        private GameObject playerInstance;
        private LocalCharacterActionController localActionController;
        private Guid? activeCharacterId;
        private Vector2 lastAppliedServerPosition;
        private bool warnedMissingPrefab;
        private bool warnedPositionMapping;
        private bool hasReportedLocalPlayerReadyForCurrentCycle;

        public Transform CurrentPlayerTransform
        {
            get { return playerInstance != null ? playerInstance.transform : null; }
        }

        public LocalCharacterActionController CurrentLocalActionController
        {
            get { return localActionController; }
        }

        public bool TryGetPopupAnchorPosition(float additionalHeight, out Vector2 worldPosition)
        {
            if (playerInstance == null)
            {
                worldPosition = default;
                return false;
            }

            var collider = ResolvePlayerBodyCollider();
            if (collider != null && collider.enabled)
            {
                var bounds = collider.bounds;
                worldPosition = new Vector2(bounds.center.x, bounds.max.y + Mathf.Max(0f, additionalHeight));
                return true;
            }

            var basePosition = (Vector2)playerInstance.transform.position;
            worldPosition = basePosition + new Vector2(0f, Mathf.Max(DefaultBoundsPadding, additionalHeight));
            return true;
        }

        private void Start()
        {
            if (!ClientRuntime.IsInitialized)
            {
                ClientLog.Warn("WorldLocalPlayerPresenter started before ClientRuntime initialization.");
                return;
            }

            AutoWireReferences();
            LogMissingCriticalWorldSceneDependenciesIfNeeded();
            ActivateWorldSceneReadiness();
            TryInitializeForReadyState(forcePosition: true);
        }

        private void OnEnable()
        {
            AutoWireReferences();
            ActivateWorldSceneReadiness();
            TryInitializeForReadyState(forcePosition: true);
        }

        private void OnDisable()
        {
            DeactivateWorldSceneReadiness();
        }

        private void Update()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            if (string.IsNullOrWhiteSpace(ClientRuntime.World.CurrentClientMapKey))
            {
                ClearLocalPlayer();
                return;
            }

            if (!IsReady(WorldSceneReadyKey.MapVisual))
                return;

            TryEnsureLocalPlayer();
            SyncInputBlockState();
            ApplyLatestPosition(force: false);
            TryReportLocalPlayerReady();
        }

        private void OnDestroy()
        {
            DeactivateWorldSceneReadiness();
            ClearLocalPlayer();
        }

        private void TryEnsureLocalPlayer()
        {
            var selectedCharacter = ClientRuntime.Character.SelectedCharacter;
            if (!selectedCharacter.HasValue)
                return;

            if (playerPrefab == null)
            {
                if (!warnedMissingPrefab)
                {
                    ClientLog.Warn("WorldLocalPlayerPresenter has no player prefab assigned.");
                    warnedMissingPrefab = true;
                }

                return;
            }

            if (worldMapPresenter != null)
            {
                if (worldMapPresenter.CurrentMapTransform == null)
                    return;

                if (!worldMapPresenter.TryGetPlayableBounds(out _))
                    return;
            }

            var characterId = selectedCharacter.Value.CharacterId;
            if (playerInstance != null && activeCharacterId == characterId)
                return;

            ClearLocalPlayer();
            warnedMissingPrefab = false;
            warnedPositionMapping = false;

            var parent = localPlayerRoot != null ? localPlayerRoot : transform;
            playerInstance = Instantiate(playerPrefab, parent, false);
            playerInstance.name = string.Format("LocalPlayer_{0}", selectedCharacter.Value.Name);
            activeCharacterId = characterId;
            localActionController = ConfigureLocalActionController(playerInstance);
            lastAppliedServerPosition = new Vector2(float.NaN, float.NaN);
            ClientLog.Info(string.Format("Spawned local player presenter for {0}.", selectedCharacter.Value.Name));
        }

        private void ApplyLatestPosition(bool force)
        {
            if (playerInstance == null)
                return;

            var serverPosition = ClientRuntime.World.LocalPlayerPosition;
            if (!force && serverPosition == lastAppliedServerPosition)
            {
                RefreshLocalActionSpeed();
                return;
            }

            Vector2 worldPosition;
            if (!TryResolveWorldPosition(serverPosition, out worldPosition))
            {
                RefreshLocalActionSpeed();
                return;
            }

            ApplyAuthoritativeWorldPosition(worldPosition, force);
            lastAppliedServerPosition = serverPosition;
            RefreshLocalActionSpeed();
        }

        private bool TryResolveWorldPosition(Vector2 serverPosition, out Vector2 worldPosition)
        {
            worldPosition = default;

            if (worldMapPresenter == null)
                return false;

            if (worldMapPresenter.TryMapServerPositionToWorld(serverPosition, out worldPosition))
            {
                worldPosition = ClampToPlayableBounds(worldPosition);
                warnedPositionMapping = false;
                return true;
            }

            Bounds playableBounds;
            if (!worldMapPresenter.TryGetPlayableBounds(out playableBounds))
                return false;

            if (!warnedPositionMapping)
            {
                ClientLog.Warn("WorldLocalPlayerPresenter could not map server position into Unity world space. Using safe PlayableBounds fallback instead of raw coordinates.");
                warnedPositionMapping = true;
            }

            worldPosition = ClampToPlayableBounds(playableBounds.center);
            return true;
        }

        private Vector2 ClampToPlayableBounds(Vector2 worldPosition)
        {
            if (worldMapPresenter == null)
                return worldPosition;

            Bounds playableBounds;
            if (!worldMapPresenter.TryGetPlayableBounds(out playableBounds))
                return worldPosition;

            var padding = ResolvePlayableBoundsPadding();
            var minX = playableBounds.min.x + padding.x;
            var maxX = playableBounds.max.x - padding.x;
            var minY = playableBounds.min.y + padding.y;
            var maxY = playableBounds.max.y - padding.y;

            var clampedX = maxX >= minX
                ? Mathf.Clamp(worldPosition.x, minX, maxX)
                : playableBounds.center.x;
            var clampedY = maxY >= minY
                ? Mathf.Clamp(worldPosition.y, minY, maxY)
                : playableBounds.center.y;

            return new Vector2(clampedX, clampedY);
        }

        private Vector2 ResolvePlayableBoundsPadding()
        {
            var bodyCollider = ResolvePlayerBodyCollider();
            if (bodyCollider == null)
                return new Vector2(DefaultBoundsPadding, DefaultBoundsPadding);

            var extents = bodyCollider.bounds.extents;
            return new Vector2(
                Mathf.Max(DefaultBoundsPadding, extents.x),
                Mathf.Max(DefaultBoundsPadding, extents.y));
        }

        private Collider2D ResolvePlayerBodyCollider()
        {
            if (playerInstance == null)
                return null;

            var playerView = playerInstance.GetComponent<PlayerView>();
            if (playerView != null && playerView.BodyCollider != null)
                return playerView.BodyCollider;

            return playerInstance.GetComponent<Collider2D>();
        }

        private void ApplyAuthoritativeWorldPosition(Vector2 worldPosition, bool force)
        {
            worldPosition = ClampToPlayableBounds(worldPosition);

            if (localActionController != null)
            {
                if (localActionController.ShouldApplyAuthoritativeWorldPosition(worldPosition, force, authoritativeSnapDistance))
                    localActionController.ApplyAuthoritativeWorldPosition(worldPosition);

                return;
            }

            var current = playerInstance.transform.position;
            playerInstance.transform.position = new Vector3(worldPosition.x, worldPosition.y, current.z);
        }

        private void ClearLocalPlayer()
        {
            if (playerInstance != null)
                Destroy(playerInstance);

            playerInstance = null;
            localActionController = null;
            activeCharacterId = null;
            lastAppliedServerPosition = new Vector2(float.NaN, float.NaN);
        }

        private void TryInitializeForReadyState(bool forcePosition)
        {
            if (!ClientRuntime.IsInitialized)
                return;

            if (!IsReady(WorldSceneReadyKey.MapVisual))
                return;

            TryEnsureLocalPlayer();
            SyncInputBlockState();
            ApplyLatestPosition(force: forcePosition);
            TryReportLocalPlayerReady();
        }

        private void TryReportLocalPlayerReady()
        {
            if (hasReportedLocalPlayerReadyForCurrentCycle || Readiness == null || playerInstance == null)
                return;

            if (!IsReady(WorldSceneReadyKey.MapVisual))
                return;

            if (localActionController == null)
                return;

            hasReportedLocalPlayerReadyForCurrentCycle = Readiness.ReportReady(WorldSceneReadyKey.LocalPlayer);
        }

        protected override void ConfigureReadyWaits()
        {
            WaitFor(WorldSceneReadyKey.MapVisual, HandleMapVisualReady);
        }

        protected override void OnWorldLoadCycleStarted(int loadVersion, string mapKey)
        {
            hasReportedLocalPlayerReadyForCurrentCycle = false;
            lastAppliedServerPosition = new Vector2(float.NaN, float.NaN);
        }

        private void HandleMapVisualReady()
        {
            TryInitializeForReadyState(forcePosition: true);
        }

        private void AutoWireReferences()
        {
            InitializeWorldSceneBehaviour(ref worldMapPresenter);
        }

        private LocalCharacterActionController ConfigureLocalActionController(GameObject target)
        {
            if (target == null)
                return null;

            var controller = target.GetComponent<LocalCharacterActionController>();
            if (controller == null)
                controller = target.AddComponent<LocalCharacterActionController>();

            controller.Initialize(
                localCharacterActionConfig,
                ResolveBaseSpeedPercent(),
                ResolveBaseMoveSpeedUnitsPerSecond());

            ConfigureSkillPresenter(target);
            return controller;
        }

        private void ConfigureSkillPresenter(GameObject target)
        {
            if (target == null || !activeCharacterId.HasValue)
                return;

            var handle = WorldTargetHandle.CreateObservedCharacter(activeCharacterId.Value);
            var targetable = target.GetComponent<WorldTargetable>();
            if (targetable == null)
                targetable = target.AddComponent<WorldTargetable>();

            targetable.Configure(handle);

            var presenter = target.GetComponent<CharacterSkillPresenter>();
            if (presenter == null)
            {
                ClientLog.Error(
                    $"WorldLocalPlayerPresenter requires CharacterSkillPresenter on local player prefab '{playerPrefab?.name ?? "<null>"}'. " +
                    $"Spawned object='{target.name}'. Add the component to the prefab instead of relying on runtime AddComponent.");
                return;
            }

            presenter.ConfigureCharacter(activeCharacterId);
            presenter.ConfigureTargetHandle(handle);
        }

        private void RefreshLocalActionSpeed()
        {
            if (localActionController == null)
                return;

            localActionController.SetSpeedStatPercent(ResolveBaseSpeedPercent());
            localActionController.SetMovementProfile(ResolveBaseMoveSpeedUnitsPerSecond());
        }

        private void SyncInputBlockState()
        {
            if (localActionController == null)
                return;

            var currentState = ClientRuntime.Character.CurrentState;
            var isDefeated = currentState.HasValue &&
                             ClientCharacterRuntimeStateCodes.IsDefeated(currentState.Value);
            var isCultivating = currentState.HasValue &&
                                currentState.Value.CurrentState == CultivatingStateCode;
            var isPracticing = currentState.HasValue &&
                               currentState.Value.CurrentState == PracticingStateCode;
            var isMenuOpen = WorldUiController.IsAnyMenuOpen;
            var isRecoveryBlocked = ClientRuntime.ConnectionRecovery != null &&
                                    ClientRuntime.ConnectionRecovery.ShouldBlockGameplayInput;
            var shouldBlock = isDefeated ||
                              isCultivating ||
                              isPracticing ||
                              isMenuOpen ||
                              isRecoveryBlocked;
            localActionController.SetInputBlocked(shouldBlock);
        }

        private int ResolveBaseSpeedPercent()
        {
            var baseStats = ClientRuntime.Character.BaseStats;
            if (!baseStats.HasValue)
                return 100;

            var totalSpeed = baseStats.Value.FinalSpeed;
            if (totalSpeed <= 0)
                return 100;

            return totalSpeed;
        }

        private float? ResolveBaseMoveSpeedUnitsPerSecond()
        {
            var baseStats = ClientRuntime.Character.BaseStats;
            if (!baseStats.HasValue)
                return null;

            var baseMoveSpeed = baseStats.Value.BaseMoveSpeed;
            return baseMoveSpeed > 0f ? baseMoveSpeed : null;
        }

        private const int CultivatingStateCode = ClientCharacterRuntimeStateCodes.Cultivating;
        private const int PracticingStateCode = ClientCharacterRuntimeStateCodes.Practicing;
    }
}




