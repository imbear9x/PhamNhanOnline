using System;
using System.Globalization;
using System.Threading.Tasks;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.UI.Inventory;
using PhamNhanOnline.Client.UI.World;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.Common
{
    public class NotificationInboxController : MonoBehaviour
    {
        private const int LifespanExpiredNotificationType = 4;

        [Header("References")]
        [SerializeField] private InventoryItemPresentationCatalog itemPresentationCatalog;

        private long? showingNotificationId;
        private bool acknowledgeInFlight;

        protected virtual void Start()
        {
            ValidateSerializedReferences();
        }

        protected virtual void OnEnable()
        {
            TrySubscribeStateChanged();
            Refresh(force: true);
        }

        protected virtual void OnDisable()
        {
            TryUnsubscribeStateChanged();
        }

        protected virtual void OnDestroy()
        {
            TryUnsubscribeStateChanged();
        }

        protected virtual void Refresh(bool force)
        {
            var modalUIManager = WorldModalUIManager.Instance;
            if (modalUIManager == null || !ClientRuntime.IsInitialized)
                return;

            var notification = ClientRuntime.Notifications.CurrentNotification;
            if (!notification.HasValue)
            {
                var wasShowingInboxPopup = showingNotificationId.HasValue;
                showingNotificationId = null;
                if (wasShowingInboxPopup)
                    modalUIManager.HideNotificationPopup(force);
                return;
            }

            if (!force &&
                showingNotificationId.HasValue &&
                showingNotificationId.Value == notification.Value.NotificationId &&
                modalUIManager.IsNotificationPopupVisible)
            {
                return;
            }

            showingNotificationId = notification.Value.NotificationId;
            var items = ResolvePopupItems(notification.Value);
            modalUIManager.ShowNotificationPopup(
                ResolveTitle(notification.Value),
                ResolveMessage(notification.Value),
                items,
                onConfirm: HandlePopupConfirmed);
        }

        protected virtual InventoryItemPresentation ResolvePresentation(ItemTemplateSummaryModel? item)
        {
            if (!item.HasValue || itemPresentationCatalog == null)
                return new InventoryItemPresentation(null, null, Color.white);

            return itemPresentationCatalog.Resolve(item.Value);
        }

        protected virtual NotificationPopupItemData[] ResolvePopupItems(PlayerNotificationModel notification)
        {
            if (notification.Items == null || notification.Items.Count == 0)
                return Array.Empty<NotificationPopupItemData>();

            var result = new NotificationPopupItemData[notification.Items.Count];
            for (var i = 0; i < notification.Items.Count; i++)
            {
                var item = notification.Items[i];
                var presentation = ResolvePresentation(item.Item);
                result[i] = new NotificationPopupItemData(
                    presentation.IconSprite,
                    presentation.BackgroundSprite,
                    item.Quantity);
            }

            return result;
        }

        protected virtual string ResolveTitle(PlayerNotificationModel notification)
        {
            if (!string.IsNullOrWhiteSpace(notification.Title))
                return notification.Title.Trim();

            return "Thong bao";
        }

        protected virtual string ResolveMessage(PlayerNotificationModel notification)
        {
            return string.IsNullOrWhiteSpace(notification.Message)
                ? "Thong bao moi."
                : notification.Message.Trim();
        }

        protected virtual void HandlePopupConfirmed()
        {
            var currentNotification = ClientRuntime.IsInitialized
                ? ClientRuntime.Notifications.CurrentNotification
                : (PlayerNotificationModel?)null;
            if (!showingNotificationId.HasValue || acknowledgeInFlight || !ClientRuntime.IsInitialized)
            {
                WorldModalUIManager.Instance?.HideNotificationPopup(force: true);
                return;
            }

            _ = AcknowledgeAsync(showingNotificationId.Value, currentNotification);
        }

        protected virtual async Task AcknowledgeAsync(long notificationId, PlayerNotificationModel? notification)
        {
            acknowledgeInFlight = true;
            try
            {
                var result = await ClientRuntime.NotificationService.AcknowledgeAsync(notificationId);
                if (!result.Success)
                {
                    return;
                }

                showingNotificationId = null;
                WorldModalUIManager.Instance?.HideNotificationPopup(force: true);

                if (notification.HasValue &&
                    notification.Value.NotificationType == LifespanExpiredNotificationType &&
                    ClientRuntime.ConnectionRecovery != null)
                {
                    ClientRuntime.ConnectionRecovery.ForceLogoutToLogin();
                }
            }
            catch (Exception ex)
            {
                ClientLog.Warn($"{nameof(NotificationInboxController)} acknowledge exception: {ex.Message}");
            }
            finally
            {
                acknowledgeInFlight = false;
            }
        }

        protected virtual void ValidateSerializedReferences()
        {
            ThrowIfMissing(itemPresentationCatalog, nameof(itemPresentationCatalog));
        }

        private void HandleNotificationStateChanged()
        {
            Refresh(force: true);
        }

        private void TrySubscribeStateChanged()
        {
            if (!ClientRuntime.IsInitialized || ClientRuntime.Notifications == null)
                return;

            ClientRuntime.Notifications.Changed -= HandleNotificationStateChanged;
            ClientRuntime.Notifications.Changed += HandleNotificationStateChanged;
        }

        private void TryUnsubscribeStateChanged()
        {
            if (!ClientRuntime.IsInitialized || ClientRuntime.Notifications == null)
                return;

            ClientRuntime.Notifications.Changed -= HandleNotificationStateChanged;
        }

        protected void ThrowIfMissing(UnityEngine.Object value, string fieldName)
        {
            if (value == null)
                throw new InvalidOperationException($"{nameof(NotificationInboxController)} on '{gameObject.name}' is missing required reference '{fieldName}'.");
        }
    }
}
