using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.Screens.Login
{
    public sealed class LoginSceneUiRegistry : MonoBehaviour
    {
        private const string LoginScreenId = "login";
        private const string CreateCharacterScreenId = "create-character";

        [SerializeField] private GameObject loginPanelRoot;
        [SerializeField] private GameObject createCharacterPanelRoot;

        private void Start()
        {
            if (!ClientRuntime.IsInitialized)
            {
                ClientLog.Warn("LoginSceneUiRegistry started before ClientRuntime initialization.");
                return;
            }

            if (loginPanelRoot != null)
                ClientRuntime.UiScreens.Register(LoginScreenId, loginPanelRoot);

            if (createCharacterPanelRoot != null)
                ClientRuntime.UiScreens.Register(CreateCharacterScreenId, createCharacterPanelRoot);
        }

        private void OnDestroy()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            ClientRuntime.UiScreens.Unregister(LoginScreenId, loginPanelRoot);
            ClientRuntime.UiScreens.Unregister(CreateCharacterScreenId, createCharacterPanelRoot);
        }
    }
}
