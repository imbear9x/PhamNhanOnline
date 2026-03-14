using PhamNhanOnline.Client.Shared.Constants;
using PhamNhanOnline.Client.Shared.Protocol;
using UnityEngine;

namespace PhamNhanOnline.Client.Infrastructure.Config
{
    [CreateAssetMenu(
        fileName = "ClientBootstrapSettings",
        menuName = "PhamNhanOnline/Config/Client Bootstrap Settings")]
    public sealed class ClientBootstrapSettings : ScriptableObject
    {
        [Header("Server")]
        [SerializeField] private string serverHost = "127.0.0.1";
        [SerializeField] private int serverPort = 7777;

        [Header("Scenes")]
        [SerializeField] private string loginSceneName = ClientSceneIds.Login;
        [SerializeField] private string worldSceneName = ClientSceneIds.World;
        [SerializeField] private string initialSceneName = ClientSceneIds.Login;

        [Header("Startup")]
        [SerializeField] private bool autoLoadInitialScene = true;
        [SerializeField] private bool attemptReconnectOnStartup = true;
        [SerializeField] private bool verboseLogging = true;

        public ServerEndpoint ServerEndpoint
        {
            get { return new ServerEndpoint(serverHost, serverPort); }
        }

        public string LoginSceneName
        {
            get { return loginSceneName; }
        }

        public string WorldSceneName
        {
            get { return worldSceneName; }
        }

        public string InitialSceneName
        {
            get { return initialSceneName; }
        }

        public bool AutoLoadInitialScene
        {
            get { return autoLoadInitialScene; }
        }

        public bool AttemptReconnectOnStartup
        {
            get { return attemptReconnectOnStartup; }
        }

        public bool VerboseLogging
        {
            get { return verboseLogging; }
        }

        public static ClientBootstrapSettings CreateRuntimeDefaults()
        {
            var settings = CreateInstance<ClientBootstrapSettings>();
            settings.name = "RuntimeDefaultClientBootstrapSettings";
            return settings;
        }
    }
}
