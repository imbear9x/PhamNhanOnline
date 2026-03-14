using System.Threading;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace PhamNhanOnline.Client.Infrastructure.SceneLoading
{
    public interface ISceneFlowService
    {
        string ActiveSceneName { get; }

        Task LoadSceneAsync(
            string sceneName,
            LoadSceneMode loadSceneMode,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}
