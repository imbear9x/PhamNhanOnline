using System;
using System.Collections.Generic;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    public enum WorldSceneReadyKey
    {
        None = 0,
        MapVisual = 1,
        LocalPlayer = 2,
        RemotePlayers = 3,
        Enemies = 4,
    }

    [DisallowMultipleComponent]
    public sealed class WorldSceneReadinessService : MonoBehaviour
    {
        [SerializeField] private bool verboseLogging;

        private readonly HashSet<WorldSceneReadyKey> readyKeys = new HashSet<WorldSceneReadyKey>();
        private bool runtimeEventsBound;
        private string currentMapKey = string.Empty;

        public int CurrentLoadVersion { get; private set; }

        public event Action<int, string> LoadCycleStarted;
        public event Action<int, WorldSceneReadyKey> ReadyReported;

        private void Awake()
        {
            TryBindRuntimeEvents();
            EnsureLoadCycleForCurrentMapState();
        }

        private void OnEnable()
        {
            TryBindRuntimeEvents();
            EnsureLoadCycleForCurrentMapState();
        }

        private void OnDisable()
        {
            UnbindRuntimeEvents();
        }

        private void OnDestroy()
        {
            UnbindRuntimeEvents();
        }

        public int EnsureLoadCycleForCurrentMapState()
        {
            if (!ClientRuntime.IsInitialized)
                return CurrentLoadVersion;

            var nextMapKey = ClientRuntime.World.CurrentClientMapKey ?? string.Empty;
            if (CurrentLoadVersion > 0 && string.Equals(currentMapKey, nextMapKey, StringComparison.Ordinal))
                return CurrentLoadVersion;

            BeginLoadCycle(nextMapKey);
            return CurrentLoadVersion;
        }

        public bool IsReady(WorldSceneReadyKey key)
        {
            return readyKeys.Contains(key);
        }

        public bool AreReady(params WorldSceneReadyKey[] keys)
        {
            if (keys == null || keys.Length == 0)
                return true;

            for (var i = 0; i < keys.Length; i++)
            {
                if (!readyKeys.Contains(keys[i]))
                    return false;
            }

            return true;
        }

        public bool ReportReady(WorldSceneReadyKey key)
        {
            if (key == WorldSceneReadyKey.None)
                return false;

            if (readyKeys.Contains(key))
                return false;

            readyKeys.Add(key);
            if (verboseLogging)
            {
                ClientLog.Info(
                    string.Format(
                        "World readiness reported. Version={0}, MapKey='{1}', Key={2}.",
                        CurrentLoadVersion,
                        currentMapKey,
                        key));
            }

            var handler = ReadyReported;
            if (handler != null)
                handler(CurrentLoadVersion, key);

            return true;
        }

        private void HandleMapChanged()
        {
            EnsureLoadCycleForCurrentMapState();
        }

        private void BeginLoadCycle(string mapKey)
        {
            CurrentLoadVersion++;
            currentMapKey = mapKey ?? string.Empty;
            readyKeys.Clear();

            if (verboseLogging)
            {
                ClientLog.Info(
                    string.Format(
                        "World readiness load cycle started. Version={0}, MapKey='{1}'.",
                        CurrentLoadVersion,
                        currentMapKey));
            }

            var handler = LoadCycleStarted;
            if (handler != null)
                handler(CurrentLoadVersion, currentMapKey);
        }

        private void TryBindRuntimeEvents()
        {
            if (runtimeEventsBound || !ClientRuntime.IsInitialized)
                return;

            ClientRuntime.World.MapChanged += HandleMapChanged;
            runtimeEventsBound = true;
        }

        private void UnbindRuntimeEvents()
        {
            if (!runtimeEventsBound || !ClientRuntime.IsInitialized)
                return;

            ClientRuntime.World.MapChanged -= HandleMapChanged;
            runtimeEventsBound = false;
        }
    }
}
