using System.Collections.Generic;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    public sealed class WorldEnemiesPresenter : WorldSceneBehaviour
    {
        [SerializeField] private EnemyPresentationCatalog presentationCatalog;
        [SerializeField] private Transform enemiesRoot;
        [SerializeField] private WorldMapPresenter worldMapPresenter;

        private readonly Dictionary<int, EnemyPresenter> enemyPresenters = new Dictionary<int, EnemyPresenter>();
        private bool warnedMissingCatalog;
        private bool runtimeEventsBound;
        private bool hasReportedReadyForCurrentCycle;

        private void Start()
        {
            if (!ClientRuntime.IsInitialized)
            {
                ClientLog.Warn("WorldEnemiesPresenter started before ClientRuntime initialization.");
                return;
            }

            AutoWireReferences();
            ActivateWorldSceneReadiness();
            TryBindRuntimeEvents();
            TrySyncIfReady();
        }

        private void OnEnable()
        {
            AutoWireReferences();
            ActivateWorldSceneReadiness();
            TryBindRuntimeEvents();
            TrySyncIfReady();
        }

        private void OnDisable()
        {
            DeactivateWorldSceneReadiness();
            UnbindRuntimeEvents();
        }

        private void OnDestroy()
        {
            DeactivateWorldSceneReadiness();
            UnbindRuntimeEvents();
            ClearEnemies();
        }

        protected override void ConfigureReadyWaits()
        {
            WaitFor(WorldSceneReadyKey.MapVisual, HandleMapVisualReady);
        }

        protected override void OnWorldLoadCycleStarted(int loadVersion, string mapKey)
        {
            hasReportedReadyForCurrentCycle = false;
            ClearEnemies();
        }

        private void HandleMapVisualReady()
        {
            SyncEnemies();
            TryReportReady();
        }

        private void HandleEnemyUpserted(EnemyRuntimeModel enemy)
        {
            if (!IsMapVisualReady())
                return;

            UpsertPresenter(enemy);
        }

        private void HandleEnemyRemoved(int runtimeId)
        {
            if (!IsMapVisualReady())
                return;

            RemovePresenter(runtimeId);
        }

        private void HandleEnemyHpChanged(PhamNhanOnline.Client.Features.World.Application.EnemyHpChangedNotice notice)
        {
            if (!IsMapVisualReady())
                return;

            UpsertPresenter(notice.Enemy);
        }

        private void HandleEnemiesChanged()
        {
            if (!IsMapVisualReady())
                return;

            SyncEnemies();
        }

        private void SyncEnemies()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            if (!IsMapVisualReady())
                return;

            if (string.IsNullOrWhiteSpace(ClientRuntime.World.CurrentClientMapKey))
            {
                ClearEnemies();
                return;
            }

            if (presentationCatalog == null)
            {
                if (!warnedMissingCatalog)
                {
                    ClientLog.Warn("WorldEnemiesPresenter has no EnemyPresentationCatalog assigned.");
                    warnedMissingCatalog = true;
                }

                ClearEnemies();
                return;
            }

            warnedMissingCatalog = false;
            var activeRuntimeIds = new HashSet<int>();
            foreach (var enemy in ClientRuntime.World.Enemies)
                activeRuntimeIds.Add(enemy.RuntimeId);

            foreach (var enemy in ClientRuntime.World.Enemies)
                UpsertPresenter(enemy);

            var removedRuntimeIds = new List<int>();
            foreach (var pair in enemyPresenters)
            {
                if (!activeRuntimeIds.Contains(pair.Key))
                    removedRuntimeIds.Add(pair.Key);
            }

            for (var i = 0; i < removedRuntimeIds.Count; i++)
                RemovePresenter(removedRuntimeIds[i]);
        }

        private bool IsMapVisualReady()
        {
            return IsReady(WorldSceneReadyKey.MapVisual);
        }

        private void TrySyncIfReady()
        {
            if (!IsMapVisualReady())
                return;

            SyncEnemies();
            TryReportReady();
        }

        private void TryReportReady()
        {
            if (hasReportedReadyForCurrentCycle || Readiness == null)
                return;

            hasReportedReadyForCurrentCycle = Readiness.ReportReady(WorldSceneReadyKey.Enemies);
        }

        private void TryBindRuntimeEvents()
        {
            if (runtimeEventsBound || !ClientRuntime.IsInitialized)
                return;

            ClientRuntime.World.EnemyUpserted += HandleEnemyUpserted;
            ClientRuntime.World.EnemyRemoved += HandleEnemyRemoved;
            ClientRuntime.World.EnemyHpChanged += HandleEnemyHpChanged;
            ClientRuntime.World.EnemiesChanged += HandleEnemiesChanged;
            runtimeEventsBound = true;
        }

        private void UnbindRuntimeEvents()
        {
            if (!runtimeEventsBound || !ClientRuntime.IsInitialized)
                return;

            ClientRuntime.World.EnemyUpserted -= HandleEnemyUpserted;
            ClientRuntime.World.EnemyRemoved -= HandleEnemyRemoved;
            ClientRuntime.World.EnemyHpChanged -= HandleEnemyHpChanged;
            ClientRuntime.World.EnemiesChanged -= HandleEnemiesChanged;
            runtimeEventsBound = false;
        }

        private void AutoWireReferences()
        {
            InitializeWorldSceneBehaviour(ref worldMapPresenter);
        }

        private EnemyPresenter CreatePresenter(EnemyRuntimeModel enemy)
        {
            GameObject prefab;
            if (!presentationCatalog.TryResolvePrefab(enemy, out prefab) || prefab == null)
            {
                ClientLog.Warn($"WorldEnemiesPresenter could not resolve prefab for enemy '{enemy.Code}' ({enemy.EnemyTemplateId}).");
                return null;
            }

            var parent = enemiesRoot != null ? enemiesRoot : transform;
            var instance = Instantiate(prefab, parent, false);
            instance.name = $"Enemy_{enemy.Code}_{enemy.RuntimeId}";

            var presenter = instance.GetComponent<EnemyPresenter>();
            if (presenter == null)
                presenter = instance.AddComponent<EnemyPresenter>();

            return presenter;
        }

        private void UpsertPresenter(EnemyRuntimeModel enemy)
        {
            EnemyPresenter presenter;
            if (!enemyPresenters.TryGetValue(enemy.RuntimeId, out presenter) || presenter == null)
            {
                presenter = CreatePresenter(enemy);
                if (presenter == null)
                    return;

                enemyPresenters[enemy.RuntimeId] = presenter;
            }

            presenter.ApplySnapshot(enemy, worldMapPresenter);
        }

        private void RemovePresenter(int runtimeId)
        {
            EnemyPresenter presenter;
            if (!enemyPresenters.TryGetValue(runtimeId, out presenter))
                return;

            enemyPresenters.Remove(runtimeId);
            if (presenter != null)
                Destroy(presenter.gameObject);
        }

        private void ClearEnemies()
        {
            foreach (var pair in enemyPresenters)
            {
                if (pair.Value != null)
                    Destroy(pair.Value.gameObject);
            }

            enemyPresenters.Clear();
        }
    }
}
