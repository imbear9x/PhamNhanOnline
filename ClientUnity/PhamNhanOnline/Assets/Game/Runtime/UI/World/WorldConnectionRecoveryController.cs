using PhamNhanOnline.Client.Core.Application;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.World
{
    public sealed class WorldConnectionRecoveryController : MonoBehaviour
    {
        private void OnEnable()
        {
            if (!ClientRuntime.IsInitialized || ClientRuntime.ConnectionRecovery == null)
                return;

            ClientRuntime.ConnectionRecovery.RecoveryStateChanged += HandleRecoveryStateChanged;
            RefreshPopup();
        }

        private void OnDisable()
        {
            if (!ClientRuntime.IsInitialized || ClientRuntime.ConnectionRecovery == null)
                return;

            ClientRuntime.ConnectionRecovery.RecoveryStateChanged -= HandleRecoveryStateChanged;
        }

        private void Update()
        {
            if (!ClientRuntime.IsInitialized || ClientRuntime.ConnectionRecovery == null)
                return;

            var recovery = ClientRuntime.ConnectionRecovery;
            if (!recovery.IsRecovering && !recovery.IsForcedLogoutPending)
                return;

            RefreshPopup();
        }

        private void HandleRecoveryStateChanged()
        {
            RefreshPopup();
        }

        private void RefreshPopup()
        {
            if (!ClientRuntime.IsInitialized || ClientRuntime.ConnectionRecovery == null)
                return;

            var recovery = ClientRuntime.ConnectionRecovery;
            if (!recovery.IsRecovering && !recovery.IsForcedLogoutPending)
            {
                WorldModalUIManager.Instance?.HideNotificationPopup(force: true);
                return;
            }

            if (WorldModalUIManager.Instance == null)
                return;

            WorldModalUIManager.Instance.ShowNotificationPopup(
                "Ket noi",
                BuildPopupMessage(recovery.ActivePopupMessage, recovery.ActivePopupStatusText),
                null,
                recovery.IsForcedLogoutPending ? (System.Action)recovery.ConfirmForcedLogout : null,
                false,
                null,
                "OK");
        }

        private static string BuildPopupMessage(string message, string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return message ?? string.Empty;

            if (string.IsNullOrWhiteSpace(message))
                return status.Trim();

            return string.Concat(message.Trim(), "\n", status.Trim());
        }
    }
}
