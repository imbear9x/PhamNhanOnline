using System;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Features.Character.Presentation;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    public sealed class WorldLocalPlayerPresenter : MonoBehaviour
    {
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

        private void Start()
        {
            if (!ClientRuntime.IsInitialized)
            {
                ClientLog.Warn("WorldLocalPlayerPresenter started before ClientRuntime initialization.");
                return;
            }

            TryEnsureLocalPlayer();
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
            if (worldMapPresenter != null && worldMapPresenter.TryMapServerPositionToWorld(serverPosition, out worldPosition))
            {
                ApplyAuthoritativeWorldPosition(worldPosition, force);
                lastAppliedServerPosition = serverPosition;
                warnedPositionMapping = false;
                RefreshLocalActionSpeed();
                return;
            }

            if (!warnedPositionMapping)
            {
                ClientLog.Warn("WorldLocalPlayerPresenter could not map server position into Unity world space. Falling back to raw coordinates.");
                warnedPositionMapping = true;
            }

            ApplyAuthoritativeWorldPosition(serverPosition, force);
            lastAppliedServerPosition = serverPosition;
            RefreshLocalActionSpeed();
        }

        private void ApplyAuthoritativeWorldPosition(Vector2 worldPosition, bool force)
        {
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

        private int ResolveBaseSpeedPercent()
        {
            var baseStats = ClientRuntime.Character.BaseStats;
            if (!baseStats.HasValue || baseStats.Value.BaseSpeed <= 0)
                return 100;

            return baseStats.Value.BaseSpeed;
        }
    }
}
