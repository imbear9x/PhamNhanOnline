using System;
using System.Collections.Generic;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    public sealed class WorldRemotePlayersPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform remotePlayersRoot;
        [SerializeField] private WorldMapPresenter worldMapPresenter;
        [SerializeField] private float remoteMoveSmoothing = 14f;

        private readonly Dictionary<Guid, RemoteCharacterPresenter> remotePresenters = new Dictionary<Guid, RemoteCharacterPresenter>();
        private bool warnedMissingPrefab;

        private void Start()
        {
            if (!ClientRuntime.IsInitialized)
            {
                ClientLog.Warn("WorldRemotePlayersPresenter started before ClientRuntime initialization.");
                return;
            }

            ClientRuntime.World.MapChanged += HandleWorldChanged;
            ClientRuntime.World.ObservedCharactersChanged += HandleWorldChanged;
            SyncRemotePlayers();
        }

        private void Update()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            if (string.IsNullOrWhiteSpace(ClientRuntime.World.CurrentClientMapKey))
            {
                ClearRemotePlayers();
                return;
            }
        }

        private void OnDestroy()
        {
            if (ClientRuntime.IsInitialized)
            {
                ClientRuntime.World.MapChanged -= HandleWorldChanged;
                ClientRuntime.World.ObservedCharactersChanged -= HandleWorldChanged;
            }

            ClearRemotePlayers();
        }

        private void HandleWorldChanged()
        {
            SyncRemotePlayers();
        }

        private void SyncRemotePlayers()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            if (string.IsNullOrWhiteSpace(ClientRuntime.World.CurrentClientMapKey))
            {
                ClearRemotePlayers();
                return;
            }

            if (playerPrefab == null)
            {
                if (!warnedMissingPrefab)
                {
                    ClientLog.Warn("WorldRemotePlayersPresenter has no player prefab assigned.");
                    warnedMissingPrefab = true;
                }

                return;
            }

            warnedMissingPrefab = false;
            var activeCharacterIds = new HashSet<Guid>();
            foreach (var observedCharacter in ClientRuntime.World.ObservedCharacters)
            {
                var characterId = observedCharacter.Character.CharacterId;
                activeCharacterIds.Add(characterId);

                RemoteCharacterPresenter presenter;
                if (!remotePresenters.TryGetValue(characterId, out presenter) || presenter == null)
                {
                    presenter = CreatePresenter(observedCharacter);
                    if (presenter == null)
                        continue;

                    remotePresenters[characterId] = presenter;
                    presenter.ApplySnapshot(observedCharacter, worldMapPresenter, snap: true);
                    continue;
                }

                presenter.ApplySnapshot(observedCharacter, worldMapPresenter, snap: false);
            }

            var removedCharacterIds = new List<Guid>();
            foreach (var pair in remotePresenters)
            {
                if (!activeCharacterIds.Contains(pair.Key))
                    removedCharacterIds.Add(pair.Key);
            }

            for (var i = 0; i < removedCharacterIds.Count; i++)
                RemovePresenter(removedCharacterIds[i]);
        }

        private RemoteCharacterPresenter CreatePresenter(ObservedCharacterModel observedCharacter)
        {
            var parent = remotePlayersRoot != null ? remotePlayersRoot : transform;
            var instance = Instantiate(playerPrefab, parent, false);
            instance.name = string.Format("RemotePlayer_{0}", observedCharacter.Character.Name);

            var presenter = instance.GetComponent<RemoteCharacterPresenter>();
            if (presenter == null)
                presenter = instance.AddComponent<RemoteCharacterPresenter>();

            presenter.Initialize(remoteMoveSmoothing);
            ClientLog.Info($"Spawned remote player presenter for {observedCharacter.Character.Name}.");
            return presenter;
        }

        private void RemovePresenter(Guid characterId)
        {
            RemoteCharacterPresenter presenter;
            if (!remotePresenters.TryGetValue(characterId, out presenter))
                return;

            remotePresenters.Remove(characterId);
            if (presenter != null)
                Destroy(presenter.gameObject);
        }

        private void ClearRemotePlayers()
        {
            foreach (var pair in remotePresenters)
            {
                if (pair.Value != null)
                    Destroy(pair.Value.gameObject);
            }

            remotePresenters.Clear();
        }
    }
}
