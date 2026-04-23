using System;
using System.Collections.Generic;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    public abstract class WorldSceneBehaviour : MonoBehaviour, IWorldSceneContextReceiver
    {
        private sealed class ReadyWaitRegistration
        {
            public WorldSceneReadyKey[] Keys;
            public Action Action;
            public int LastInvokedVersion = -1;
        }

        private readonly List<ReadyWaitRegistration> readyWaits = new List<ReadyWaitRegistration>();
        private bool requestedReadinessActivation;
        private bool readyWaitsConfigured;
        private bool readinessEventsBound;
        private bool loggedMissingSceneController;
        private bool loggedMissingMapPresenter;
        private bool loggedMissingReadiness;

        protected WorldSceneContext SceneContext { get; private set; }
        protected WorldSceneController SceneController { get; private set; }
        protected WorldMapPresenter MapPresenter { get; private set; }
        protected WorldSceneReadinessService Readiness { get; private set; }

        public void InitializeWorldSceneContext(WorldSceneContext context)
        {
            SceneContext = context;
            SceneController = context != null ? context.SceneController : null;
            MapPresenter = context != null ? context.WorldMapPresenter : null;
            Readiness = context != null ? context.WorldSceneReadinessService : null;
            EnsureReadyWaitsConfigured();
            OnWorldSceneContextInitialized(context);

            if (requestedReadinessActivation && isActiveAndEnabled)
                ActivateWorldSceneReadiness();
        }

        protected virtual void OnWorldSceneContextInitialized(WorldSceneContext context)
        {
        }

        protected void InitializeWorldSceneBehaviour(
            WorldSceneController sceneController = null,
            WorldMapPresenter mapPresenter = null,
            WorldSceneReadinessService readinessService = null)
        {
            if (sceneController != null)
                SceneController = sceneController;

            if (mapPresenter != null)
                MapPresenter = mapPresenter;

            if (readinessService != null)
                Readiness = readinessService;

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
            requestedReadinessActivation = true;
            if (readinessEventsBound || !ClientRuntime.IsInitialized || Readiness == null)
                return;

            Readiness.LoadCycleStarted += HandleLoadCycleStarted;
            Readiness.ReadyReported += HandleReadyReported;
            readinessEventsBound = true;
            TryInvokeReadyWaits();
        }

        protected void DeactivateWorldSceneReadiness()
        {
            requestedReadinessActivation = false;
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

        protected void LogMissingCriticalWorldSceneDependenciesIfNeeded()
        {
            LogMissingCriticalDependenciesIfNeeded();
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

        private void LogMissingCriticalDependenciesIfNeeded()
        {
            var owner = GetType().Name;
            if (SceneController == null && !loggedMissingSceneController)
            {
                ClientLog.Error($"{owner} could not resolve WorldSceneController. Assign the scene root controller explicitly.");
                loggedMissingSceneController = true;
            }

            if (MapPresenter == null && !loggedMissingMapPresenter)
            {
                ClientLog.Error($"{owner} could not resolve WorldMapPresenter. Assign the map presenter explicitly.");
                loggedMissingMapPresenter = true;
            }

            if (Readiness == null && !loggedMissingReadiness)
            {
                ClientLog.Error($"{owner} could not resolve WorldSceneReadinessService. Assign the readiness service explicitly.");
                loggedMissingReadiness = true;
            }
        }
    }
}



