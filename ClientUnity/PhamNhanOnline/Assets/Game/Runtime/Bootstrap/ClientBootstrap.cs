using System.Threading.Tasks;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Infrastructure.Config;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PhamNhanOnline.Client.Bootstrap
{
    public sealed class ClientBootstrap : MonoBehaviour
    {
        private static bool bootstrapped;

        [SerializeField] private ClientBootstrapSettings settings;
        [SerializeField] private bool persistentAcrossScenes = true;

        private async void Awake()
        {
            if (bootstrapped)
            {
                Destroy(gameObject);
                return;
            }

            if (settings == null)
            {
                ClientLog.Error("ClientBootstrapSettings is missing.");
                enabled = false;
                return;
            }

            bootstrapped = true;

            if (persistentAcrossScenes)
                DontDestroyOnLoad(gameObject);

            ClientRuntime.Initialize(settings);
            await LoadInitialSceneIfNeededAsync();
        }

        private async Task LoadInitialSceneIfNeededAsync()
        {
            if (!settings.AutoLoadInitialScene)
                return;

            if (string.IsNullOrWhiteSpace(settings.InitialSceneName))
                return;

            if (ClientRuntime.SceneFlow.ActiveSceneName == settings.InitialSceneName)
                return;

            await ClientRuntime.SceneFlow.LoadSceneAsync(settings.InitialSceneName, LoadSceneMode.Single);
        }

        private void Update()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            ClientRuntime.Connection.Tick();
        }
    }
}
