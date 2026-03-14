using System;
using System.Threading;
using System.Threading.Tasks;
using PhamNhanOnline.Client.Core.Logging;
using UnityEngine.SceneManagement;

namespace PhamNhanOnline.Client.Infrastructure.SceneLoading
{
    public sealed class UnitySceneFlowService : ISceneFlowService
    {
        public string ActiveSceneName
        {
            get { return SceneManager.GetActiveScene().name; }
        }

        public async Task LoadSceneAsync(
            string sceneName,
            LoadSceneMode loadSceneMode,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                return;

            ClientLog.Info(string.Format("Loading scene '{0}' ({1}).", sceneName, loadSceneMode));

            var operation = SceneManager.LoadSceneAsync(sceneName, loadSceneMode);
            if (operation == null)
                throw new InvalidOperationException(string.Format("Unity could not start loading scene '{0}'.", sceneName));

            while (!operation.isDone)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }
        }
    }
}
