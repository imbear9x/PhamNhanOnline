using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.Common
{
    public class NotificationPopupView : ViewModelBase
    {
        [Header("References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private GameObject itemListRoot;
        [SerializeField] private Transform itemListContentRoot;
        [SerializeField] private NotificationPopupItemSlotView itemSlotTemplate;
        [SerializeField] private UIButtonView confirmButton;
        [SerializeField] private TMP_Text confirmButtonText;
        [SerializeField] private UIButtonView cancelButton;
        [SerializeField] private TMP_Text cancelButtonText;

        [Header("Fallback")]
        [SerializeField] private string defaultTitleText = "Thong bao";
        [SerializeField] private string defaultConfirmText = "OK";
        [SerializeField] private string defaultCancelText = "Huy";

        private readonly List<NotificationPopupItemSlotView> itemSlotInstances = new List<NotificationPopupItemSlotView>(4);
        private Action confirmAction;
        private Action cancelAction;

        public event Action Confirmed;
        public event Action Cancelled;

        protected override bool HideOnFirstAwake => true;

        protected override void Awake()
        {
            if (panelRoot == null)
                panelRoot = gameObject;

            if (confirmButton != null)
            {
                confirmButton.Clicked -= HandleConfirmClicked;
                confirmButton.Clicked += HandleConfirmClicked;
            }

            if (cancelButton != null)
            {
                cancelButton.Clicked -= HandleCancelClicked;
                cancelButton.Clicked += HandleCancelClicked;
            }

            if (confirmButtonText != null)
                confirmButtonText.text = defaultConfirmText;
            if (cancelButtonText != null)
                cancelButtonText.text = defaultCancelText;

            base.Awake();
        }

        protected override GameObject ResolveViewRoot()
        {
            return panelRoot != null ? panelRoot : gameObject;
        }

        protected virtual void Start()
        {
            ValidateSerializedReferences();
        }

        protected virtual void OnDestroy()
        {
            if (confirmButton != null)
                confirmButton.Clicked -= HandleConfirmClicked;
            if (cancelButton != null)
                cancelButton.Clicked -= HandleCancelClicked;
        }

        public void Show(
            string title,
            string message,
            NotificationPopupItemData[] items,
            Action onConfirm = null,
            bool showCancelButton = false,
            Action onCancel = null,
            string confirmTextOverride = null,
            string cancelTextOverride = null)
        {
            confirmAction = onConfirm;
            cancelAction = onCancel;
            ShowView();

            if (titleText != null)
                titleText.text = string.IsNullOrWhiteSpace(title) ? defaultTitleText : title.Trim();
            if (messageText != null)
                messageText.text = string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim();
            if (confirmButtonText != null)
                confirmButtonText.text = string.IsNullOrWhiteSpace(confirmTextOverride) ? defaultConfirmText : confirmTextOverride.Trim();
            if (cancelButtonText != null)
                cancelButtonText.text = string.IsNullOrWhiteSpace(cancelTextOverride) ? defaultCancelText : cancelTextOverride.Trim();
            if (cancelButton != null)
                cancelButton.gameObject.SetActive(showCancelButton);

            BindItems(items);
        }

        public void Hide(bool force = false)
        {
            if (!force && !IsVisible)
                return;

            confirmAction = null;
            cancelAction = null;
            SetViewVisible(false, force: true);
            ClearInstancedSlots();
        }

        protected virtual void ValidateSerializedReferences()
        {
            ThrowIfMissing(panelRoot, nameof(panelRoot));
            ThrowIfMissing(titleText, nameof(titleText));
            ThrowIfMissing(messageText, nameof(messageText));
            ThrowIfMissing(itemListRoot, nameof(itemListRoot));
            ThrowIfMissing(itemListContentRoot, nameof(itemListContentRoot));
            ThrowIfMissing(itemSlotTemplate, nameof(itemSlotTemplate));
            ThrowIfMissing(confirmButton, nameof(confirmButton));
            ThrowIfMissing(confirmButtonText, nameof(confirmButtonText));
            ThrowIfMissing(cancelButton, nameof(cancelButton));
            ThrowIfMissing(cancelButtonText, nameof(cancelButtonText));

            itemSlotTemplate.gameObject.SetActive(false);
        }

        private void BindItems(NotificationPopupItemData[] items)
        {
            var resolvedItems = items ?? Array.Empty<NotificationPopupItemData>();
            var hasItems = resolvedItems.Length > 0;

            if (itemListRoot != null)
                itemListRoot.SetActive(hasItems);

            if (!hasItems)
            {
                ClearInstancedSlots();
                return;
            }

            EnsureSlotCount(resolvedItems.Length);
            for (var i = 0; i < itemSlotInstances.Count; i++)
            {
                var slot = itemSlotInstances[i];
                if (i >= resolvedItems.Length)
                {
                    slot.Clear(force: true);
                    continue;
                }

                slot.Bind(
                    resolvedItems[i].IconSprite,
                    resolvedItems[i].BackgroundSprite,
                    resolvedItems[i].Quantity);
            }
        }

        private void EnsureSlotCount(int requiredCount)
        {
            while (itemSlotInstances.Count < requiredCount)
            {
                var instance = Instantiate(itemSlotTemplate, itemListContentRoot);
                instance.gameObject.name = $"{itemSlotTemplate.gameObject.name}_{itemSlotInstances.Count + 1}";
                instance.Clear(force: true);
                itemSlotInstances.Add(instance);
            }
        }

        private void ClearInstancedSlots()
        {
            for (var i = 0; i < itemSlotInstances.Count; i++)
                itemSlotInstances[i].Clear(force: true);
        }

        private void HandleConfirmClicked()
        {
            if (confirmAction != null)
                confirmAction.Invoke();
            else
                Hide(force: true);

            Confirmed?.Invoke();
        }

        private void HandleCancelClicked()
        {
            Hide(force: true);
            cancelAction?.Invoke();
            Cancelled?.Invoke();
        }

        protected void ThrowIfMissing(UnityEngine.Object value, string fieldName)
        {
            if (value == null)
                throw new InvalidOperationException($"{nameof(NotificationPopupView)} on '{gameObject.name}' is missing required reference '{fieldName}'.");
        }
    }

    public readonly struct NotificationPopupItemData
    {
        public NotificationPopupItemData(Sprite iconSprite, Sprite backgroundSprite, int quantity)
        {
            IconSprite = iconSprite;
            BackgroundSprite = backgroundSprite;
            Quantity = quantity;
        }

        public Sprite IconSprite { get; }
        public Sprite BackgroundSprite { get; }
        public int Quantity { get; }
    }
}
