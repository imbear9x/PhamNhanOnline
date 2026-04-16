using System;
using GameShared.Models;
using PhamNhanOnline.Client.UI.Common;
using PhamNhanOnline.Client.UI.World;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Inventory
{
    public sealed class InventoryItemSlotView : LoopScrollViewItem,
        IUiDragPayloadSource,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        IInitializePotentialDragHandler,
        IPointerClickHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [Header("References")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text quantityText;
        [SerializeField] private GameObject quantityRoot;
        [SerializeField] private GameObject selectedHighlightRoot;

        [Header("Display")]
        [SerializeField] private float draggingAlpha = 0.65f;

        private InventoryItemModel item;
        private bool hasItem;
        private long lastPlayerItemId = long.MinValue;
        private int lastQuantity = int.MinValue;
        private bool lastEquippedState;
        private int lastEnhanceLevel = int.MinValue;
        private Sprite lastIconSprite;
        private Sprite lastBackgroundSprite;
        private bool isSelected;
        private CanvasGroup canvasGroup;
        private InventoryDragGhost dragGhost;
        private InventoryItemPresentation currentPresentation;

        public event Action<InventoryItemSlotView> Clicked;
        public event Action<InventoryItemSlotView> Hovered;
        public event Action<InventoryItemSlotView> HoverExited;

        public InventoryItemModel Item => item;
        public bool HasItem => hasItem;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void SetItem(InventoryItemModel value, InventoryItemPresentation presentation, bool force = false)
        {
            hasItem = true;
            item = value;
            currentPresentation = presentation;

            if (!force &&
                lastPlayerItemId == value.PlayerItemId &&
                lastQuantity == value.Quantity &&
                lastEquippedState == value.IsEquipped &&
                lastEnhanceLevel == value.EnhanceLevel &&
                lastIconSprite == presentation.IconSprite &&
                lastBackgroundSprite == presentation.BackgroundSprite)
            {
                return;
            }

            lastPlayerItemId = value.PlayerItemId;
            lastQuantity = value.Quantity;
            lastEquippedState = value.IsEquipped;
            lastEnhanceLevel = value.EnhanceLevel;
            lastIconSprite = presentation.IconSprite;
            lastBackgroundSprite = presentation.BackgroundSprite;

            if (backgroundImage != null)
                backgroundImage.sprite = presentation.BackgroundSprite;

            if (iconImage != null)
            {
                iconImage.sprite = presentation.IconSprite;
                iconImage.color = presentation.IconSprite != null
                    ? Color.white
                    : new Color(1f, 1f, 1f, 0f);
            }

            var hasQuantity = value.Quantity > 1;
            if (quantityRoot != null)
                quantityRoot.SetActive(hasQuantity);
            if (quantityText != null)
                quantityText.text = hasQuantity ? value.Quantity.ToString() : string.Empty;
        }

        public void Clear(bool force = false)
        {
            hasItem = false;
            item = default;
            currentPresentation = default;
            lastPlayerItemId = long.MinValue;
            lastQuantity = int.MinValue;
            lastEquippedState = false;
            lastEnhanceLevel = int.MinValue;
            lastIconSprite = null;
            lastBackgroundSprite = null;

            if (backgroundImage != null)
                backgroundImage.sprite = null;

            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.color = new Color(1f, 1f, 1f, 0f);
            }

            if (quantityRoot != null)
                quantityRoot.SetActive(false);
            if (quantityText != null)
                quantityText.text = string.Empty;

            ResetDragVisuals();

            if (force)
                SetSelected(false, force: true);
        }

        public override void OnItemRecycled()
        {
            Clear(force: true);
        }

        public void SetSelected(bool selected, bool force = false)
        {
            if (!force && isSelected == selected)
                return;

            isSelected = selected;
            if (selectedHighlightRoot != null)
                selectedHighlightRoot.SetActive(selected);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!hasItem)
                return;

            WorldModalUIManager.Instance?.ShowItemTooltip(this, item, currentPresentation, force: true);
            var handler = Hovered;
            if (handler != null)
                handler(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!hasItem)
                return;

            WorldModalUIManager.Instance?.HideItemTooltip(this, force: true);
            var handler = HoverExited;
            if (handler != null)
                handler(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!hasItem)
                return;

            var handler = Clicked;
            if (handler != null)
                handler(this);

            eventData?.Use();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!hasItem)
                return;

            var modalUiManager = WorldModalUIManager.Instance;
            if (modalUiManager != null)
            {
                modalUiManager.SetItemTooltipSuppressed(this, suppressed: true, force: true);
                modalUiManager.HideItemTooltip(this, force: true);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!hasItem)
                return;

            WorldModalUIManager.Instance?.SetItemTooltipSuppressed(this, suppressed: false);
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (!hasItem || eventData == null)
                return;

            eventData.useDragThreshold = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!hasItem)
                return;

            var modalUiManager = WorldModalUIManager.Instance;
            if (modalUiManager != null)
            {
                modalUiManager.HideInventoryItemOptionsPopup(force: true);
                modalUiManager.SetItemTooltipSuppressed(this, suppressed: true, force: true);
                modalUiManager.HideItemTooltip(this, force: true);
            }

            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = draggingAlpha;
            dragGhost = InventoryDragGhost.Create(
                transform,
                currentPresentation,
                eventData,
                iconImage != null ? iconImage.rectTransform : transform as RectTransform);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragGhost != null)
                dragGhost.UpdatePosition(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            ResetDragVisuals();
            var modalUiManager = WorldModalUIManager.Instance;
            if (modalUiManager != null)
            {
                modalUiManager.SetItemTooltipSuppressed(this, suppressed: false);
                modalUiManager.HideItemTooltip(this, force: true);
            }
        }

        public bool TryCreateDragPayload(out UiDragPayload payload)
        {
            if (!hasItem)
            {
                payload = default;
                return false;
            }

            payload = UiDragPayload.FromInventoryItem(item, UiDragSourceKind.InventoryGridItem);
            return true;
        }

        private void ResetDragVisuals()
        {
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.alpha = 1f;
            }

            if (dragGhost != null)
            {
                dragGhost.Dispose();
                dragGhost = null;
            }
        }
    }
}
