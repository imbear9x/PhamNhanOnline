using System;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    public sealed class WorldLocalPlayerPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform localPlayerRoot;
        [SerializeField] private WorldMapPresenter worldMapPresenter;

        private GameObject playerInstance;
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
                return;

            Vector2 worldPosition;
            if (worldMapPresenter != null && worldMapPresenter.TryMapServerPositionToWorld(serverPosition, out worldPosition))
            {
                var current = playerInstance.transform.position;
                playerInstance.transform.position = new Vector3(worldPosition.x, worldPosition.y, current.z);
                lastAppliedServerPosition = serverPosition;
                warnedPositionMapping = false;
                return;
            }

            if (!warnedPositionMapping)
            {
                ClientLog.Warn("WorldLocalPlayerPresenter could not map server position into Unity world space. Falling back to raw coordinates.");
                warnedPositionMapping = true;
            }

            var fallback = playerInstance.transform.position;
            playerInstance.transform.position = new Vector3(serverPosition.x, serverPosition.y, fallback.z);
            lastAppliedServerPosition = serverPosition;
        }

        private void ClearLocalPlayer()
        {
            if (playerInstance != null)
                Destroy(playerInstance);

            playerInstance = null;
            activeCharacterId = null;
            lastAppliedServerPosition = new Vector2(float.NaN, float.NaN);
        }
    }
}