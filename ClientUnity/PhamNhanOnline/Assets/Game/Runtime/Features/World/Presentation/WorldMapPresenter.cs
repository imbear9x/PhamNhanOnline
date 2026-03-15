using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    public sealed class WorldMapPresenter : MonoBehaviour
    {
        [SerializeField] private ClientMapCatalog mapCatalog;
        [SerializeField] private Transform activeMapRoot;

        private GameObject activeMapInstance;
        private string activeClientMapKey = string.Empty;

        private void Start()
        {
            if (!ClientRuntime.IsInitialized)
            {
                ClientLog.Warn("WorldMapPresenter started before ClientRuntime initialization.");
                return;
            }

            ClientRuntime.World.MapChanged += HandleMapChanged;
            RebuildActiveMap(ClientRuntime.World.CurrentClientMapKey);
        }

        private void OnDestroy()
        {
            if (ClientRuntime.IsInitialized)
                ClientRuntime.World.MapChanged -= HandleMapChanged;

            ClearActiveMap();
        }

        public void Refresh()
        {
            if (!ClientRuntime.IsInitialized)
            {
                ClientLog.Warn("WorldMapPresenter cannot refresh before ClientRuntime initialization.");
                return;
            }

            RebuildActiveMap(ClientRuntime.World.CurrentClientMapKey);
        }

        private void HandleMapChanged()
        {
            RebuildActiveMap(ClientRuntime.World.CurrentClientMapKey);
        }

        private void RebuildActiveMap(string clientMapKey)
        {
            if (string.Equals(activeClientMapKey, clientMapKey, System.StringComparison.Ordinal) && activeMapInstance != null)
                return;

            ClearActiveMap();
            activeClientMapKey = clientMapKey ?? string.Empty;

            if (string.IsNullOrWhiteSpace(activeClientMapKey))
                return;

            if (mapCatalog == null)
            {
                ClientLog.Warn($"WorldMapPresenter has no {nameof(ClientMapCatalog)} assigned.");
                return;
            }

            GameObject mapPrefab;
            if (!mapCatalog.TryGetMapPrefab(activeClientMapKey, out mapPrefab))
            {
                ClientLog.Warn($"No map prefab is registered for ClientMapKey '{activeClientMapKey}'.");
                return;
            }

            var parent = activeMapRoot != null ? activeMapRoot : transform;
            activeMapInstance = Instantiate(mapPrefab, parent, false);
            activeMapInstance.name = mapPrefab.name;
            ClientLog.Info($"Spawned map '{activeClientMapKey}' into World scene.");
        }

        private void ClearActiveMap()
        {
            if (activeMapInstance == null)
                return;

            Destroy(activeMapInstance);
            activeMapInstance = null;
            activeClientMapKey = string.Empty;
        }
    }
}