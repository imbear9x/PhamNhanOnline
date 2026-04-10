using System;
using System.Globalization;
using System.Threading.Tasks;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.UI.Inventory;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.Common
{
    public class NotificationInboxController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NotificationPopupView popupView;
        [SerializeField] private InventoryItemPresentationCatalog itemPresentationCatalog;
        [SerializeField] private GameObject[] eligibleRoots = Array.Empty<GameObject>();

        private long? showingNotificationId;
        private bool acknowledgeInFlight;
        private long? blockedNotificationId;

        protected virtual void Awake()
        {
            if (popupView != null)
                popupView.Confirmed += HandlePopupConfirmed;
        }

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

        protected virtual void Update()
        {
            Refresh(force: false);
        }

        protected virtual void OnDestroy()
        {
            TryUnsubscribeStateChanged();
            if (popupView != null)
                popupView.Confirmed -= HandlePopupConfirmed;
        }

        protected virtual void Refresh(bool force)
        {
            if (popupView == null || !ClientRuntime.IsInitialized)
                return;

            var notification = ClientRuntime.Notifications.CurrentNotification;
            if (!notification.HasValue)
            {
                showingNotificationId = null;
                blockedNotificationId = null;
                popupView.Hide(force);
                return;
            }

            if (!CanShowPopup())
            {
                blockedNotificationId = notification.Value.NotificationId;
                return;
            }

            if (!force &&
                showingNotificationId.HasValue &&
                showingNotificationId.Value == notification.Value.NotificationId &&
                popupView.IsVisible)
            {
                return;
            }

            blockedNotificationId = null;
            showingNotificationId = notification.Value.NotificationId;
            var items = ResolvePopupItems(notification.Value);
            popupView.Show(
                ResolveTitle(notification.Value),
                ResolveMessage(notification.Value),
                items);
        }

        protected virtual bool CanShowPopup()
        {
            if (eligibleRoots == null || eligibleRoots.Length == 0)
                return isActiveAndEnabled;

            for (var i = 0; i < eligibleRoots.Length; i++)
            {
                if (eligibleRoots[i] != null && eligibleRoots[i].activeInHierarchy)
                    return true;
            }

            return false;
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
            if (!showingNotificationId.HasValue || acknowledgeInFlight || !ClientRuntime.IsInitialized)
            {
                popupView.Hide(force: true);
                return;
            }

            _ = AcknowledgeAsync(showingNotificationId.Value);
        }

        protected virtual async Task AcknowledgeAsync(long notificationId)
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
                popupView.Hide(force: true);
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
            ThrowIfMissing(popupView, nameof(popupView));
            ThrowIfMissing(itemPresentationCatalog, nameof(itemPresentationCatalog));
            if (eligibleRoots == null || eligibleRoots.Length == 0)
                throw new InvalidOperationException($"{nameof(NotificationInboxController)} on '{gameObject.name}' requires at least one eligible root.");

            for (var i = 0; i < eligibleRoots.Length; i++)
            {
                if (eligibleRoots[i] == null)
                    throw new InvalidOperationException($"{nameof(NotificationInboxController)} on '{gameObject.name}' has a null entry in '{nameof(eligibleRoots)}' at index {i}.");
            }
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
