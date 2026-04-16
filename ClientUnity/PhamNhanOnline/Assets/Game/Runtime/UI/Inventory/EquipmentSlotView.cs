using System;
using GameShared.Models;
using PhamNhanOnline.Client.UI.Common;
using PhamNhanOnline.Client.UI.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Inventory
{
    public sealed class EquipmentSlotView : MonoBehaviour,
        IUiDragPayloadSource,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerClickHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IDropHandler
    {
        [Header("Configuration")]
        [SerializeField] private InventoryEquipmentSlot slotType = InventoryEquipmentSlot.None;

        [Header("References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject emptyStateRoot;
        [SerializeField] private GameObject selectedRoot;

        [Header("Behavior")]
        [SerializeField] private float draggingAlpha = 0.65f;

        private InventoryItemModel item;
        private bool hasItem;
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

            ApplyEmptyState();
        }

        public void SetItem(InventoryItemModel value, InventoryItemPresentation presentation, bool force = false)
        {
            _ = force;

            hasItem = true;
            item = value;
            currentPresentation = presentation;

            if (emptyStateRoot != null)
                emptyStateRoot.SetActive(false);

            if (iconImage != null)
            {
                iconImage.sprite = presentation.IconSprite;
                iconImage.enabled = presentation.IconSprite != null;
            }

            SetSelected(selected: false, force: true);
        }

        public void Clear(bool force = false)
        {
            _ = force;

            hasItem = false;
            item = default;
            currentPresentation = default;
            ApplyEmptyState();
        }

        public void SetSelected(bool selected, bool force = false)
        {
            _ = selected;
            _ = force;
            SetDragSelectionVisible(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData != null && IsValidDraggedInventoryItem(eventData.pointerDrag != null ? eventData.pointerDrag.transform : null))
                SetDragSelectionVisible(true);

            if (eventData != null && eventData.pointerDrag != null)
                return;

            if (!hasItem)
                return;

            WorldModalUIManager.Instance?.ShowItemTooltip(this, item, currentPresentation, force: true);
            var handler = Hovered;
            if (handler != null)
                handler(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetDragSelectionVisible(false);

            if (!hasItem)
                return;

            WorldModalUIManager.Instance?.HideItemTooltip(this, force: true);
            var handler = HoverExited;
            if (handler != null)
                handler(this);
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

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!hasItem)
                return;

            var handler = Clicked;
            if (handler != null)
                handler(this);

            eventData?.Use();
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

        public void OnDrop(PointerEventData eventData)
        {
            SetDragSelectionVisible(false);

            if (!UiDragPayloadResolver.TryResolve(eventData, out var payload) ||
                payload.Kind != UiDragPayloadKind.InventoryItem ||
                !payload.HasInventoryItem ||
                payload.SourceKind != UiDragSourceKind.InventoryGridItem)
            {
                return;
            }

            var droppedItem = payload.InventoryItem;
            if (droppedItem.EquipmentSlotType != (int)slotType)
                return;

            var handler = InventoryItemDropped;
            if (handler != null)
                handler(this, droppedItem);
        }

        public bool TryCreateDragPayload(out UiDragPayload payload)
        {
            if (!hasItem)
            {
                payload = default;
                return false;
            }

            payload = UiDragPayload.FromInventoryItem(item, UiDragSourceKind.EquipmentSlot, slotType);
            return true;
        }

        private void ApplyEmptyState()
        {
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            if (emptyStateRoot != null)
                emptyStateRoot.SetActive(true);

            ResetDragVisuals();
            SetSelected(false, force: true);
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

            SetDragSelectionVisible(false);
        }

        private bool IsValidDraggedInventoryItem(Transform dragTransform)
        {
            if (!UiDragPayloadResolver.TryResolve(dragTransform, out var payload) ||
                payload.Kind != UiDragPayloadKind.InventoryItem ||
                !payload.HasInventoryItem ||
                payload.SourceKind != UiDragSourceKind.InventoryGridItem)
            {
                return false;
            }

            return payload.InventoryItem.EquipmentSlotType == (int)slotType;
        }

        private void SetDragSelectionVisible(bool visible)
        {
            if (selectedRoot != null && selectedRoot.activeSelf != visible)
                selectedRoot.SetActive(visible);
        }
    }
}
