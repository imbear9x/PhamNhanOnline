using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Features.Character.Application;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.World
{
    public sealed class WorldCombatDeathController : MonoBehaviour
    {
        [Header("Popup Text")]
        [SerializeField] private string popupTitle = "Trong thuong";
        [SerializeField] private string popupMessage = "Nhan vat da tu thuong. Tam thoi chi co the tro ve dong phu.";
        [SerializeField] private string confirmButtonText = "Tro ve dong phu";

        [Header("Status Text")]
        [SerializeField] private string actionInProgressText = "Dang tro ve dong phu...";

        private bool actionInFlight;
        private string statusText = string.Empty;
        private bool loggedMissingModalManager;

        private void Awake()
        {
            ApplyViewState(false);
        }

        private void OnEnable()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            ClientRuntime.Character.CurrentStateChanged += HandleCharacterCurrentStateChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (ClientRuntime.IsInitialized)
                ClientRuntime.Character.CurrentStateChanged -= HandleCharacterCurrentStateChanged;

            ApplyViewState(false);
        }

        private void HandleCharacterCurrentStateChanged(CharacterCurrentStateChangeNotice notice)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (!ClientRuntime.IsInitialized)
            {
                ApplyViewState(false);
                return;
            }

            var isCombatDead = IsCombatDead(ClientRuntime.Character.CurrentState);
            if (!isCombatDead)
            {
                actionInFlight = false;
                statusText = string.Empty;
            }

            ApplyViewState(isCombatDead);
        }

        private async void HandleReturnHomeRequested()
        {
            if (actionInFlight || !ClientRuntime.IsInitialized || ClientRuntime.CombatDeathRecoveryService == null)
                return;

            actionInFlight = true;
            statusText = actionInProgressText;
            ApplyViewState(true);

            CombatDeathReturnHomeResult result;
            try
            {
                result = await ClientRuntime.CombatDeathRecoveryService.ReturnHomeAsync();
            }
            finally
            {
                actionInFlight = false;
            }

            statusText = result.Success ? string.Empty : result.Message;

            Refresh();
        }

        private void ApplyViewState(bool isCombatDead)
        {
            if (isCombatDead)
            {
                if (WorldUIController.Instance != null)
                    WorldUIController.Instance.HideMenuIfVisible();

                if (WorldModalUIManager.Instance == null)
                {
                    LogMissingModalManagerIfNeeded();
                    return;
                }

                WorldModalUIManager.Instance.ShowNotificationPopup(
                    popupTitle,
                    BuildPopupMessage(popupMessage, statusText),
                    null,
                    HandleReturnHomeRequested,
                    false,
                    null,
                    confirmButtonText);
            }
            else
            {
                WorldModalUIManager.Instance?.HideNotificationPopup(force: true);
            }
        }

        private static bool IsCombatDead(GameShared.Models.CharacterCurrentStateModel? currentState)
        {
            return currentState.HasValue &&
                   ClientCharacterRuntimeStateCodes.IsCombatDead(currentState.Value.CurrentState);
        }

        private static string BuildPopupMessage(string message, string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return message ?? string.Empty;

            if (string.IsNullOrWhiteSpace(message))
                return status.Trim();

            return string.Concat(message.Trim(), "\n\n", status.Trim());
        }

        private void LogMissingModalManagerIfNeeded()
        {
            if (loggedMissingModalManager)
                return;

            ClientLog.Error("WorldCombatDeathController requires WorldModalUIManager to show the death popup.");
            loggedMissingModalManager = true;
        }
    }
}


