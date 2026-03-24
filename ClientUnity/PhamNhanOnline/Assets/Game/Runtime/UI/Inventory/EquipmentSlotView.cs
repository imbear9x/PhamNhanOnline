using System;
using GameShared.Models;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Inventory
{
    public sealed class EquipmentSlotView : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IDropHandler
    {
        [Header("Configuration")]
        [SerializeField] private InventoryEquipmentSlot slotType = InventoryEquipmentSlot.None;

        [Header("References")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text slotLabelText;
        [SerializeField] private TMP_Text enhanceLevelText;
        [SerializeField] private GameObject enhanceLevelRoot;
        [SerializeField] private GameObject selectedHighlightRoot;
        [SerializeField] private GameObject emptyStateRoot;
        [SerializeField] private GameObject occupiedStateRoot;

        [Header("Display")]
        [SerializeField] private string emptySlotLabel = "Ô trống";
        [SerializeField] private Color emptyIconColor = new Color(1f, 1f, 1f, 0f);
        [SerializeField] private Color filledIconColor = Color.white;
        [SerializeField] private float draggingAlpha = 0.65f;

        private InventoryItemModel item;
        private bool hasItem;
        private bool isSelected;
        private CanvasGroup canvasGroup;
        private InventoryDragGhost dragGhost;
        private InventoryItemPresentation currentPresentation;

        public event Action<EquipmentSlotView> Clicked;
        public event Action<EquipmentSlotView> Hovered;
        public event Action<EquipmentSlotView> HoverExited;
        public event Action<EquipmentSlotView, InventoryItemModel> InventoryItemDropped;

        public InventoryEquipmentSlot SlotType => slotType;
        public InventoryItemModel Item => item;
        public bool HasItem => hasItem;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            ApplyEmptyState(force: true);
        }

        public void SetItem(InventoryItemModel value, InventoryItemPresentation presentation, bool force = false)
        {
            hasItem = true;
            item = value;
            currentPresentation = presentation;

            if (emptyStateRoot != null)
                emptyStateRoot.SetActive(false);
            if (occupiedStateRoot != null)
                occupiedStateRoot.SetActive(true);

            if (backgroundImage != null)
                backgroundImage.sprite = presentation.BackgroundSprite;

            if (iconImage != null)
            {
                iconImage.sprite = presentation.IconSprite;
                iconImage.color = presentation.IconSprite != null ? filledIconColor : emptyIconColor;
            }

            if (slotLabelText != null)
            {
                slotLabelText.text = ResolveOccupiedLabel(value);
                slotLabelText.gameObject.SetActive(true);
            }

            var hasEnhanceLevel = value.EnhanceLevel > 0;
            if (enhanceLevelRoot != null)
                enhanceLevelRoot.SetActive(hasEnhanceLevel);
            if (enhanceLevelText != null)
                enhanceLevelText.text = hasEnhanceLevel ? string.Format("+{0}", value.EnhanceLevel) : string.Empty;

            SetSelected(isSelected, force: true);
        }

        public void Clear(bool force = false)
        {
            hasItem = false;
            item = default;
            currentPresentation = default;
            ApplyEmptyState(force);
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

            var handler = Hovered;
            if (handler != null)
                handler(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!hasItem)
                return;

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
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!hasItem)
                return;

            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = draggingAlpha;
            dragGhost = InventoryDragGhost.Create(transform, currentPresentation, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragGhost != null)
                dragGhost.UpdatePosition(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            ResetDragVisuals();
        }

        public void OnDrop(PointerEventData eventData)
        {
            var inventorySlotView = eventData.pointerDrag != null
                ? eventData.pointerDrag.GetComponent<InventoryItemSlotView>()
                : null;

            if (inventorySlotView == null || !inventorySlotView.HasItem)
                return;

            var droppedItem = inventorySlotView.Item;
            if (droppedItem.EquipmentSlotType != (int)slotType)
                return;

            var handler = InventoryItemDropped;
            if (handler != null)
                handler(this, droppedItem);
        }

        private void ApplyEmptyState(bool force)
        {
            if (backgroundImage != null)
                backgroundImage.sprite = null;

            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.color = emptyIconColor;
            }

            if (slotLabelText != null)
            {
                slotLabelText.text = GetDefaultSlotLabel();
                slotLabelText.gameObject.SetActive(true);
            }

            if (enhanceLevelRoot != null)
                enhanceLevelRoot.SetActive(false);
            if (enhanceLevelText != null)
                enhanceLevelText.text = string.Empty;

            if (emptyStateRoot != null)
                emptyStateRoot.SetActive(true);
            if (occupiedStateRoot != null)
                occupiedStateRoot.SetActive(false);

            ResetDragVisuals();
            SetSelected(false, force: force);
        }

        private string GetDefaultSlotLabel()
        {
            return string.IsNullOrWhiteSpace(emptySlotLabel)
                ? InventoryItemPresentationCatalog.GetEquipmentSlotLabel((int)slotType)
                : emptySlotLabel;
        }

        private string ResolveOccupiedLabel(InventoryItemModel value)
        {
            if (!string.IsNullOrWhiteSpace(value.Name))
                return value.Name.Trim();

            return GetDefaultSlotLabel();
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
