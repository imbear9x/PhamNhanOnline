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
        [Header("Runtime")]
        [SerializeField] private ClientBootstrapSettings runtimeSettingsOverride;

        [Header("Inputs")]
        [SerializeField] private TMP_InputField usernameInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private TMP_InputField characterNameInput;

        [Header("Panels")]
        [SerializeField] private GameObject loginPanelRoot;
        [SerializeField] private GameObject createCharacterPanelRoot;

        [Header("Actions")]
        [SerializeField] private Button connectButton;
        [SerializeField] private Button createCharacterButton;
        [SerializeField] private Button openWorldButton;

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

            if (createCharacterButton != null)
                createCharacterButton.onClick.AddListener(HandleCreateCharacterClicked);

            if (openWorldButton != null)
                openWorldButton.onClick.AddListener(HandleOpenWorldClicked);

            SetCharacterCreationMode(false);
            if (openWorldButton != null)
                openWorldButton.interactable = false;

            SetStatus("Ready.");
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

            if (createCharacterButton != null)
                createCharacterButton.onClick.RemoveListener(HandleCreateCharacterClicked);

            if (openWorldButton != null)
                openWorldButton.onClick.RemoveListener(HandleOpenWorldClicked);
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
                if (openWorldButton != null)
                    openWorldButton.interactable = false;
            }
            else if (result.Success)
            {
                SetCharacterCreationMode(false);
            }

            SetStatus(result.Message);

            if (connectButton != null)
                connectButton.interactable = true;
        }

        private async void HandleCreateCharacterClicked()
        {
            if (!ClientRuntime.IsInitialized)
            {
                SetStatus("Client runtime is not initialized.");
                return;
            }

            var characterName = characterNameInput != null ? characterNameInput.text : string.Empty;
            if (string.IsNullOrWhiteSpace(characterName))
            {
                SetStatus("Enter a character name first.");
                return;
            }

            if (createCharacterButton != null)
                createCharacterButton.interactable = false;

            SetStatus("Creating character...");
            var result = await ClientRuntime.CharacterService.CreateCharacterAsync(characterName, defaultServerId, defaultModelId);
            if (result.Success && result.Character.HasValue)
            {
                pendingCharacterId = result.Character.Value.CharacterId;
                if (openWorldButton != null)
                    openWorldButton.interactable = true;
                SetStatus(string.Format("Character {0} created. Press Open World to continue.", result.Character.Value.Name));
            }
            else
            {
                SetStatus(result.Message);
            }

            if (createCharacterButton != null)
                createCharacterButton.interactable = true;
        }

        private async void HandleOpenWorldClicked()
        {
            if (!ClientRuntime.IsInitialized)
            {
                SetStatus("Client runtime is not initialized.");
                return;
            }

            var characterId = pendingCharacterId ?? ClientRuntime.Character.SelectedCharacterId;
            if (!characterId.HasValue || characterId.Value == Guid.Empty)
            {
                SetStatus("No character is ready to enter the world.");
                return;
            }

            SetStatus("Entering world...");
            var result = await ClientRuntime.CharacterService.EnterWorldAsync(characterId.Value);
            if (!result.Success)
            {
                SetStatus(result.Message);
                return;
            }

            pendingCharacterId = null;
            SetCharacterCreationMode(false);
            if (openWorldButton != null)
                openWorldButton.interactable = false;

            SetStatus(string.Format("Loading {0}...", ClientRuntime.Settings.WorldSceneName));
            await ClientRuntime.SceneFlow.LoadSceneAsync(ClientRuntime.Settings.WorldSceneName, LoadSceneMode.Single);
        }

        private void SetCharacterCreationMode(bool enabled)
        {
            if (loginPanelRoot != null)
                loginPanelRoot.SetActive(!enabled);

            if (createCharacterPanelRoot != null)
                createCharacterPanelRoot.SetActive(enabled);
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }
    }
}
