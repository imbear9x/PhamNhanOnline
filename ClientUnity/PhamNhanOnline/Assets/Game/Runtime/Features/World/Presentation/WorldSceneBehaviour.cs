using System;
using System.Collections.Generic;
using PhamNhanOnline.Client.Core.Application;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    public abstract class WorldSceneBehaviour : MonoBehaviour
    {
        private sealed class ReadyWaitRegistration
        {
            public WorldSceneReadyKey[] Keys;
            public Action Action;
            public int LastInvokedVersion = -1;
        }

        private readonly List<ReadyWaitRegistration> readyWaits = new List<ReadyWaitRegistration>();
        private bool readyWaitsConfigured;
        private bool readinessEventsBound;

        protected WorldSceneController SceneController { get; private set; }
        protected WorldMapPresenter MapPresenter { get; private set; }
        protected WorldSceneReadinessService Readiness { get; private set; }

        protected void InitializeWorldSceneBehaviour(
            WorldSceneController sceneController = null,
            WorldMapPresenter mapPresenter = null,
            WorldSceneReadinessService readinessService = null)
        {
            if (SceneController == null)
                SceneController = sceneController ?? GetComponent<WorldSceneController>() ?? WorldSceneController.Instance;

            if (MapPresenter == null)
                MapPresenter = mapPresenter ?? GetComponent<WorldMapPresenter>() ?? SceneController?.WorldMapPresenter;

            if (Readiness == null)
            {
                Readiness = readinessService ??
                            GetComponent<WorldSceneReadinessService>() ??
                            MapPresenter?.GetComponent<WorldSceneReadinessService>() ??
                            SceneController?.WorldSceneReadinessService;
            }

            EnsureReadyWaitsConfigured();
        }

        protected void InitializeWorldSceneBehaviour(ref WorldMapPresenter mapPresenter)
        {
            InitializeWorldSceneBehaviour(mapPresenter: mapPresenter);
            mapPresenter = MapPresenter;
        }

        protected void InitializeWorldSceneBehaviour(
            ref WorldSceneController sceneController,
            ref WorldMapPresenter mapPresenter)
        {
            InitializeWorldSceneBehaviour(sceneController, mapPresenter);
            sceneController = SceneController;
            mapPresenter = MapPresenter;
        }

        protected void ActivateWorldSceneReadiness()
        {
            if (readinessEventsBound || !ClientRuntime.IsInitialized || Readiness == null)
                return;

            Readiness.LoadCycleStarted += HandleLoadCycleStarted;
            Readiness.ReadyReported += HandleReadyReported;
            readinessEventsBound = true;
            TryInvokeReadyWaits();
        }

        protected void DeactivateWorldSceneReadiness()
        {
            if (!readinessEventsBound || !ClientRuntime.IsInitialized || Readiness == null)
                return;

            Readiness.LoadCycleStarted -= HandleLoadCycleStarted;
            Readiness.ReadyReported -= HandleReadyReported;
            readinessEventsBound = false;
        }

        protected bool IsReady(WorldSceneReadyKey key)
        {
            return Readiness == null || Readiness.IsReady(key);
        }

        protected bool AreReady(params WorldSceneReadyKey[] keys)
        {
            return Readiness == null || Readiness.AreReady(keys);
        }

        protected void WaitFor(WorldSceneReadyKey key, Action action)
        {
            WaitForAll(action, key);
        }

        protected void WaitForAll(Action action, params WorldSceneReadyKey[] keys)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (keys == null || keys.Length == 0)
                throw new ArgumentException("At least one readiness key is required.", nameof(keys));

            readyWaits.Add(new ReadyWaitRegistration
            {
                Keys = (WorldSceneReadyKey[])keys.Clone(),
                Action = action,
            });
        }

        protected virtual void ConfigureReadyWaits()
        {
        }

        protected virtual void OnWorldLoadCycleStarted(int loadVersion, string mapKey)
        {
        }

        protected virtual void OnWorldReadyReported(int loadVersion, WorldSceneReadyKey key)
        {
        }

        private void EnsureReadyWaitsConfigured()
        {
            if (readyWaitsConfigured)
                return;

            readyWaitsConfigured = true;
            ConfigureReadyWaits();
        }

        private void HandleLoadCycleStarted(int loadVersion, string mapKey)
        {
            OnWorldLoadCycleStarted(loadVersion, mapKey);
            TryInvokeReadyWaits();
        }

        private void HandleReadyReported(int loadVersion, WorldSceneReadyKey key)
        {
            OnWorldReadyReported(loadVersion, key);
            TryInvokeReadyWaits();
        }

        private void TryInvokeReadyWaits()
        {
            if (Readiness == null)
                return;

            var currentVersion = Readiness.CurrentLoadVersion;
            for (var i = 0; i < readyWaits.Count; i++)
            {
                var registration = readyWaits[i];
                if (registration == null || registration.Action == null)
                    continue;

                if (registration.LastInvokedVersion == currentVersion)
                    continue;

                if (!Readiness.AreReady(registration.Keys))
                    continue;

                registration.LastInvokedVersion = currentVersion;
                registration.Action();
            }
        }
    }
}
