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
            ClientRuntime.World.ObservedCharacterUpserted += HandleObservedCharacterUpserted;
            ClientRuntime.World.ObservedCharacterRemoved += HandleObservedCharacterRemoved;
            ClientRuntime.World.ObservedCharacterMoved += HandleObservedCharacterMoved;
            ClientRuntime.World.ObservedCharacterStateChanged += HandleObservedCharacterStateChanged;
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
                ClientRuntime.World.ObservedCharacterUpserted -= HandleObservedCharacterUpserted;
                ClientRuntime.World.ObservedCharacterRemoved -= HandleObservedCharacterRemoved;
                ClientRuntime.World.ObservedCharacterMoved -= HandleObservedCharacterMoved;
                ClientRuntime.World.ObservedCharacterStateChanged -= HandleObservedCharacterStateChanged;
            }

            ClearRemotePlayers();
        }

        private void HandleWorldChanged()
        {
            SyncRemotePlayers();
        }

        private void HandleObservedCharacterUpserted(ObservedCharacterModel observedCharacter)
        {
            UpsertPresenter(observedCharacter, snap: true);
        }

        private void HandleObservedCharacterRemoved(Guid characterId)
        {
            RemovePresenter(characterId);
        }

        private void HandleObservedCharacterMoved(PhamNhanOnline.Client.Features.World.Application.ObservedCharacterMovedNotice notice)
        {
            UpsertPresenter(notice.Character, snap: false);
        }

        private void HandleObservedCharacterStateChanged(PhamNhanOnline.Client.Features.World.Application.ObservedCharacterStateChangedNotice notice)
        {
            UpsertPresenter(notice.Character, snap: false);
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
                activeCharacterIds.Add(observedCharacter.Character.CharacterId);

            foreach (var observedCharacter in ClientRuntime.World.ObservedCharacters)
                UpsertPresenter(observedCharacter, snap: true);

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

        private void UpsertPresenter(ObservedCharacterModel observedCharacter, bool snap)
        {
            var characterId = observedCharacter.Character.CharacterId;
            RemoteCharacterPresenter presenter;
            if (!remotePresenters.TryGetValue(characterId, out presenter) || presenter == null)
            {
                presenter = CreatePresenter(observedCharacter);
                if (presenter == null)
                    return;

                remotePresenters[characterId] = presenter;
                presenter.ApplySnapshot(observedCharacter, worldMapPresenter, snap: true);
                return;
            }

            presenter.ApplySnapshot(observedCharacter, worldMapPresenter, snap);
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
