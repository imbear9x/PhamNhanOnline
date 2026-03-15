using System;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Infrastructure.Config;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
        [SerializeField] private Button connectButton;

        [Header("Defaults")]
        [SerializeField] private int defaultServerId = 1;
        [SerializeField] private int defaultModelId = 1;

        [Header("Feedback")]
        [SerializeField] private TMP_Text statusText;

        private Guid? pendingCharacterId;

        private void Awake()
        {
            EnsureRuntimeInitialized();

            if (connectButton != null)
                connectButton.onClick.AddListener(HandleConnectClicked);



            SetStatus("Ready.");

            // hard code để test cho nhanh
            usernameInput.text = "admin123456";
            passwordInput.text = "admin@admin";
        }

        private void Start()
        {
            SetCharacterCreationMode(false);
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
                connectButton.onClick.RemoveListener(HandleConnectClicked);

           
        }

        private async void HandleConnectClicked()
        {
            if (!ClientRuntime.IsInitialized)
            {
                SetStatus("Client runtime is not initialized.");
                return;
            }

            if (connectButton != null)
                connectButton.interactable = false;

            var username = usernameInput != null ? usernameInput.text : string.Empty;
            var password = passwordInput != null ? passwordInput.text : string.Empty;
            SetStatus(string.Format("Connecting to {0}...", ClientRuntime.Connection.Endpoint));

            var result = await ClientRuntime.LoginFlow.ConnectLoginAndEnterWorldAsync(username, password);
            if (result.RequiresCharacterCreation)
            {
                pendingCharacterId = null;
                SetCharacterCreationMode(true);
            }
            else if (result.Success)
            {
                SetCharacterCreationMode(false);
            }

            SetStatus(result.Message);

            if (connectButton != null)
                connectButton.interactable = true;
        }

        

        private void SetCharacterCreationMode(bool enabled)
        {
            if (!ClientRuntime.IsInitialized)
                return;

            var targetScreenId = enabled ? CreateCharacterScreenId : LoginScreenId;
            if (!ClientRuntime.UiScreens.IsRegistered(targetScreenId))
            {
                ClientLog.Warn($"Screen '{targetScreenId}' is not registered in UiScreenService.");
                return;
            }

            ClientRuntime.UiScreens.ShowOnly(targetScreenId);
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }
    }
}
