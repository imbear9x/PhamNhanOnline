using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Infrastructure.Config;
using PhamNhanOnline.Client.UI.Common;
using PhamNhanOnline.Client.UI.World;
using TMPro;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.Screens.Login
{
    public sealed class LoginScreenController : MonoBehaviour
    {
        private const string LoginScreenId = "login";
        private const string CreateCharacterScreenId = "create-character";

        [Header("Runtime")]
        [SerializeField] private ClientBootstrapSettings runtimeSettingsOverride;

        [Header("Inputs")]
        [SerializeField] private TMP_InputField usernameInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private TMP_InputField characterNameInput;

        [Header("Actions")]
        [SerializeField] private UIButtonView connectButton;

        [Header("Feedback")]
        [SerializeField] private TMP_Text statusText;

        private void Awake()
        {
            EnsureRuntimeInitialized();

            if (connectButton != null)
                connectButton.Clicked += HandleConnectClicked;

            SetStatus("Ready.");

            // hard code de test cho nhanh
            if (usernameInput != null)
                usernameInput.text = "admin123456";
            if (passwordInput != null)
                passwordInput.text = "admin@admin";
        }

        private void Start()
        {
            SetCharacterCreationMode(false);
            ConsumePendingConnectionLostPopup();
        }

        private void EnsureRuntimeInitialized()
        {
            if (ClientRuntime.IsInitialized)
                return;

            var settings = runtimeSettingsOverride != null
                ? runtimeSettingsOverride
                : ClientBootstrapSettings.CreateRuntimeDefaults();

            ClientRuntime.Initialize(settings);
            ClientLog.Info($"Client runtime auto-initialized from Login scene using {settings.name}.");
        }

        private void OnDestroy()
        {
            if (connectButton != null)
                connectButton.Clicked -= HandleConnectClicked;
        }

        private async void HandleConnectClicked()
        {
            if (!ClientRuntime.IsInitialized)
            {
                SetStatus("Client runtime is not initialized.");
                return;
            }

            HideConnectionLostPopup();

            if (connectButton != null)
                connectButton.SetInteractable(false, force: true);

            var username = usernameInput != null ? usernameInput.text : string.Empty;
            var password = passwordInput != null ? passwordInput.text : string.Empty;
            SetStatus(string.Format("Connecting to {0}...", ClientRuntime.Connection.Endpoint));

            var result = await ClientRuntime.LoginFlow.ConnectLoginAndEnterWorldAsync(username, password);
            if (result.RequiresCharacterCreation)
            {
                SetCharacterCreationMode(true);
            }
            else if (result.Success)
            {
                SetCharacterCreationMode(false);
            }

            SetStatus(result.Message);
            if (result.IsConnectionFailure)
                ShowConnectionLostPopup();

            if (connectButton != null)
                connectButton.SetInteractable(true, force: true);
        }

        private void SetCharacterCreationMode(bool enabled)
        {
            if (!ClientRuntime.IsInitialized)
                return;

            var targetScreenId = enabled ? CreateCharacterScreenId : LoginScreenId;
            if (!ClientRuntime.UIScreens.IsRegistered(targetScreenId))
            {
                ClientLog.Warn($"Screen '{targetScreenId}' is not registered in UIScreenService.");
                return;
            }

            ClientRuntime.UIScreens.ShowOnly(targetScreenId);
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        private void ConsumePendingConnectionLostPopup()
        {
            if (!ClientRuntime.IsInitialized || ClientRuntime.ConnectionRecovery == null)
                return;

            if (ClientRuntime.ConnectionRecovery.ConsumePendingLoginPopup(out var message))
                ShowConnectionLostPopup(message);
        }

        private void ShowConnectionLostPopup(string message = null)
        {
            if (WorldModalUIManager.Instance == null)
            {
                ClientLog.Warn("LoginScreenController could not show connection lost popup because WorldModalUIManager.Instance is not available.");
                return;
            }

            var resolvedMessage = string.IsNullOrWhiteSpace(message)
                ? "Mat ket noi toi server."
                : message;
            WorldModalUIManager.Instance.ShowNotificationPopup(
                "Mat ket noi",
                resolvedMessage,
                null,
                null,
                false,
                null,
                "OK");
        }

        private void HideConnectionLostPopup()
        {
            WorldModalUIManager.Instance?.HideNotificationPopup(force: true);
        }
    }
}
