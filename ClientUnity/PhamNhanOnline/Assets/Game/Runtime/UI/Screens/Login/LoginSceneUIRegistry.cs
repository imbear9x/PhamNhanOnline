using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.Screens.Login
{
    public sealed class LoginSceneUIRegistry : MonoBehaviour
    {
        private const string LoginScreenId = "login";
        private const string CreateCharacterScreenId = "create-character";

        [SerializeField] private GameObject loginPanelRoot;
        [SerializeField] private GameObject createCharacterPanelRoot;
        private bool registered;

        private void Awake()
        {
            TryRegisterScreens();
        }

        private void OnEnable()
        {
            TryRegisterScreens();
        }

        private void Start()
        {
            TryRegisterScreens();
        }

        private void TryRegisterScreens()
        {
            if (registered)
                return;

            if (!ClientRuntime.IsInitialized)
            {
                ClientLog.Warn("LoginSceneUIRegistry could not register screens before ClientRuntime initialization.");
                return;
            }

            if (loginPanelRoot != null)
                ClientRuntime.UIScreens.Register(LoginScreenId, loginPanelRoot);

            if (createCharacterPanelRoot != null)
                ClientRuntime.UIScreens.Register(CreateCharacterScreenId, createCharacterPanelRoot);

            registered = true;
        }

        private void OnDestroy()
        {
            if (!registered || !ClientRuntime.IsInitialized)
                return;

            ClientRuntime.UIScreens.Unregister(LoginScreenId, loginPanelRoot);
            ClientRuntime.UIScreens.Unregister(CreateCharacterScreenId, createCharacterPanelRoot);
            registered = false;
        }
    }
}
