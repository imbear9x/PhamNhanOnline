using System.Collections.Generic;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    public sealed class WorldEnemiesPresenter : MonoBehaviour
    {
        [SerializeField] private EnemyPresentationCatalog presentationCatalog;
        [SerializeField] private Transform enemiesRoot;
        [SerializeField] private WorldMapPresenter worldMapPresenter;

        private readonly Dictionary<int, EnemyPresenter> enemyPresenters = new Dictionary<int, EnemyPresenter>();
        private bool warnedMissingCatalog;

        private void Start()
        {
            if (!ClientRuntime.IsInitialized)
            {
                ClientLog.Warn("WorldEnemiesPresenter started before ClientRuntime initialization.");
                return;
            }

            ClientRuntime.World.MapChanged += HandleWorldChanged;
            ClientRuntime.World.EnemyUpserted += HandleEnemyUpserted;
            ClientRuntime.World.EnemyRemoved += HandleEnemyRemoved;
            ClientRuntime.World.EnemyHpChanged += HandleEnemyHpChanged;
            SyncEnemies();
        }

        private void OnDestroy()
        {
            if (ClientRuntime.IsInitialized)
            {
                ClientRuntime.World.MapChanged -= HandleWorldChanged;
                ClientRuntime.World.EnemyUpserted -= HandleEnemyUpserted;
                ClientRuntime.World.EnemyRemoved -= HandleEnemyRemoved;
                ClientRuntime.World.EnemyHpChanged -= HandleEnemyHpChanged;
            }

            ClearEnemies();
        }

        private void HandleWorldChanged()
        {
            SyncEnemies();
        }

        private void HandleEnemyUpserted(EnemyRuntimeModel enemy)
        {
            UpsertPresenter(enemy);
        }

        private void HandleEnemyRemoved(int runtimeId)
        {
            RemovePresenter(runtimeId);
        }

        private void HandleEnemyHpChanged(PhamNhanOnline.Client.Features.World.Application.EnemyHpChangedNotice notice)
        {
            UpsertPresenter(notice.Enemy);
        }

        private void SyncEnemies()
        {
            if (!ClientRuntime.IsInitialized)
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
