using System;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Features.Character.Presentation;
using PhamNhanOnline.Client.UI.World;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    public sealed class WorldLocalPlayerPresenter : MonoBehaviour
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

        public Transform CurrentPlayerTransform
        {
            get { return playerInstance != null ? playerInstance.transform : null; }
        }

        public LocalCharacterActionController CurrentLocalActionController
        {
            get { return localActionController; }
        }

        private void Start()
        {
            if (!ClientRuntime.IsInitialized)
            {
                ClientLog.Warn("WorldLocalPlayerPresenter started before ClientRuntime initialization.");
                return;
            }

            TryEnsureLocalPlayer();
            SyncInputBlockState();
            ApplyLatestPosition(force: true);
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

            TryEnsureLocalPlayer();
            SyncInputBlockState();
            ApplyLatestPosition(force: false);
        }

        private void OnDestroy()
        {
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

            if (worldMapPresenter != null && !worldMapPresenter.TryGetPlayableBounds(out _))
                return;

            var characterId = selectedCharacter.Value.CharacterId;
            if (playerInstance != null && activeCharacterId == characterId)
                return;

            ClearLocalPlayer();
            warnedMissingPrefab = false;
            warnedPositionMapping = false;

            var parent = localPlayerRoot != null ? localPlayerRoot : transform;
            playerInstance = Instantiate(playerPrefab, parent, false);
            playerInstance.name = string.Format("LocalPlayer_{0}", selectedCharacter.Value.Name);
            localActionController = ConfigureLocalActionController(playerInstance);
            activeCharacterId = characterId;
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

        private LocalCharacterActionController ConfigureLocalActionController(GameObject target)
        {
            if (target == null)
                return null;

            var controller = target.GetComponent<LocalCharacterActionController>();
            if (controller == null)
                controller = target.AddComponent<LocalCharacterActionController>();

            controller.Initialize(localCharacterActionConfig, ResolveBaseSpeedPercent());
            return controller;
        }

        private void RefreshLocalActionSpeed()
        {
            if (localActionController != null)
                localActionController.SetSpeedStatPercent(ResolveBaseSpeedPercent());
        }

        private void SyncInputBlockState()
        {
            if (localActionController == null)
                return;

            var currentState = ClientRuntime.Character.CurrentState;
            var shouldBlock =
                (currentState.HasValue && currentState.Value.CurrentState == CultivatingStateCode) ||
                WorldMenuController.IsAnyMenuOpen;
            localActionController.SetInputBlocked(shouldBlock);
        }

        private int ResolveBaseSpeedPercent()
        {
            var baseStats = ClientRuntime.Character.BaseStats;
            if (!baseStats.HasValue || baseStats.Value.BaseSpeed <= 0)
                return 100;

            return baseStats.Value.BaseSpeed;
        }

        private const int CultivatingStateCode = 3;
    }
}
