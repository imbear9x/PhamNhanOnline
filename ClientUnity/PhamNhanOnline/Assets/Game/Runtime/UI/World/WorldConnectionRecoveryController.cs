using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.UI.Common;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.World
{
    public sealed class WorldConnectionRecoveryController : MonoBehaviour
    {
        [SerializeField] private ServerConnectionPopupView popupView;

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
            if (popupView == null || !ClientRuntime.IsInitialized || ClientRuntime.ConnectionRecovery == null)
                return;

            var recovery = ClientRuntime.ConnectionRecovery;
            if (!recovery.IsRecovering && !recovery.IsForcedLogoutPending)
            {
                popupView.Hide();
                return;
            }

            popupView.Show(
                recovery.ActivePopupMessage,
                recovery.ActivePopupStatusText,
                recovery.ActivePopupAllowClose,
                recovery.IsForcedLogoutPending ? (System.Action)recovery.ConfirmForcedLogout : null);
        }
    }
}
