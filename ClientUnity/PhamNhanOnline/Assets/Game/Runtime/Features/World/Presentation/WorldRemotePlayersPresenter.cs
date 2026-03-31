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
        [SerializeField] private WorldSceneReadinessService readinessService;
        [SerializeField] private float remoteMoveSmoothing = 14f;

        private readonly Dictionary<Guid, RemoteCharacterPresenter> remotePresenters = new Dictionary<Guid, RemoteCharacterPresenter>();
        private bool warnedMissingPrefab;
        private bool runtimeEventsBound;
        private bool hasReportedReadyForCurrentCycle;

        private void Start()
        {
            if (!ClientRuntime.IsInitialized)
            {
                ClientLog.Warn("WorldRemotePlayersPresenter started before ClientRuntime initialization.");
                return;
            }

            AutoWireReferences();
            TryBindRuntimeEvents();
            TrySyncIfReady();
        }

        private void Update()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            if (readinessService != null && !readinessService.IsReady(WorldSceneReadyKey.MapVisual))
                return;

            if (string.IsNullOrWhiteSpace(ClientRuntime.World.CurrentClientMapKey))
            {
                ClearRemotePlayers();
                return;
            }
        }

        private void OnDestroy()
        {
            UnbindRuntimeEvents();
            ClearRemotePlayers();
        }

        private void HandleMapVisualReady()
        {
            SyncRemotePlayers();
            TryReportReady();
        }

        private void HandleObservedCharacterUpserted(ObservedCharacterModel observedCharacter)
        {
            if (!IsMapVisualReady())
                return;

            UpsertPresenter(observedCharacter, snap: true);
        }

        private void HandleObservedCharacterRemoved(Guid characterId)
        {
            if (!IsMapVisualReady())
                return;

            RemovePresenter(characterId);
        }

        private void HandleObservedCharacterMoved(PhamNhanOnline.Client.Features.World.Application.ObservedCharacterMovedNotice notice)
        {
            if (!IsMapVisualReady())
                return;

            UpsertPresenter(notice.Character, snap: false);
        }

        private void HandleObservedCharacterStateChanged(PhamNhanOnline.Client.Features.World.Application.ObservedCharacterStateChangedNotice notice)
        {
            if (!IsMapVisualReady())
                return;

            UpsertPresenter(notice.Character, snap: false);
        }

        private void SyncRemotePlayers()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            if (!IsMapVisualReady())
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

        private bool IsMapVisualReady()
        {
            return readinessService == null || readinessService.IsReady(WorldSceneReadyKey.MapVisual);
        }

        private void TrySyncIfReady()
        {
            if (!IsMapVisualReady())
                return;

            SyncRemotePlayers();
            TryReportReady();
        }

        private void TryReportReady()
        {
            if (hasReportedReadyForCurrentCycle || readinessService == null)
                return;

            hasReportedReadyForCurrentCycle = readinessService.ReportReady(WorldSceneReadyKey.RemotePlayers);
        }

        private void HandleLoadCycleStarted(int loadVersion, string mapKey)
        {
            hasReportedReadyForCurrentCycle = false;
            ClearRemotePlayers();
        }

        private void HandleReadyReported(int loadVersion, WorldSceneReadyKey key)
        {
            if (key != WorldSceneReadyKey.MapVisual)
                return;

            HandleMapVisualReady();
        }

        private void TryBindRuntimeEvents()
        {
            if (runtimeEventsBound || !ClientRuntime.IsInitialized)
                return;

            ClientRuntime.World.ObservedCharacterUpserted += HandleObservedCharacterUpserted;
            ClientRuntime.World.ObservedCharacterRemoved += HandleObservedCharacterRemoved;
            ClientRuntime.World.ObservedCharacterMoved += HandleObservedCharacterMoved;
            ClientRuntime.World.ObservedCharacterStateChanged += HandleObservedCharacterStateChanged;
            if (readinessService != null)
            {
                readinessService.LoadCycleStarted += HandleLoadCycleStarted;
                readinessService.ReadyReported += HandleReadyReported;
            }

            runtimeEventsBound = true;
        }

        private void UnbindRuntimeEvents()
        {
            if (!runtimeEventsBound || !ClientRuntime.IsInitialized)
                return;

            ClientRuntime.World.ObservedCharacterUpserted -= HandleObservedCharacterUpserted;
            ClientRuntime.World.ObservedCharacterRemoved -= HandleObservedCharacterRemoved;
            ClientRuntime.World.ObservedCharacterMoved -= HandleObservedCharacterMoved;
            ClientRuntime.World.ObservedCharacterStateChanged -= HandleObservedCharacterStateChanged;
            if (readinessService != null)
            {
                readinessService.LoadCycleStarted -= HandleLoadCycleStarted;
                readinessService.ReadyReported -= HandleReadyReported;
            }

            runtimeEventsBound = false;
        }

        private void AutoWireReferences()
        {
            if (worldMapPresenter == null)
                worldMapPresenter = GetComponent<WorldMapPresenter>();

            if (readinessService == null)
                readinessService = GetComponent<WorldSceneReadinessService>();

            if (readinessService == null && worldMapPresenter != null)
                readinessService = worldMapPresenter.GetComponent<WorldSceneReadinessService>();

            if (readinessService == null && WorldSceneController.Instance != null)
                readinessService = WorldSceneController.Instance.WorldSceneReadinessService;
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
