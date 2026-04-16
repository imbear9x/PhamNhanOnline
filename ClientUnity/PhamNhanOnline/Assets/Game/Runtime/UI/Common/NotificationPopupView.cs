using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Common
{
    public class NotificationPopupView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private GameObject itemListRoot;
        [SerializeField] private Transform itemListContentRoot;
        [SerializeField] private NotificationPopupItemSlotView itemSlotTemplate;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TMP_Text confirmButtonText;

        [Header("Fallback")]
        [SerializeField] private string defaultTitleText = "Thong bao";
        [SerializeField] private string defaultConfirmText = "OK";

        private readonly List<NotificationPopupItemSlotView> itemSlotInstances = new List<NotificationPopupItemSlotView>(4);

        public event Action Confirmed;

        public bool IsVisible => (panelRoot != null ? panelRoot : gameObject).activeSelf;

        protected virtual void Awake()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(HandleConfirmClicked);
                confirmButton.onClick.AddListener(HandleConfirmClicked);
            }

            if (confirmButtonText != null)
                confirmButtonText.text = defaultConfirmText;
        }

        protected virtual void Start()
        {
            ValidateSerializedReferences();
        }

        protected virtual void OnDestroy()
        {
            if (confirmButton != null)
                confirmButton.onClick.RemoveListener(HandleConfirmClicked);
        }

        public void Show(string title, string message, NotificationPopupItemData[] items)
        {
            var root = panelRoot != null ? panelRoot : gameObject;
            if (!root.activeSelf)
                root.SetActive(true);

            if (titleText != null)
                titleText.text = string.IsNullOrWhiteSpace(title) ? defaultTitleText : title.Trim();
            if (messageText != null)
                messageText.text = string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim();

            BindItems(items);
        }

        public void Hide(bool force = false)
        {
            var root = panelRoot != null ? panelRoot : gameObject;
            if (!force && !root.activeSelf)
                return;
            root.SetActive(false);
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
            Confirmed?.Invoke();
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
