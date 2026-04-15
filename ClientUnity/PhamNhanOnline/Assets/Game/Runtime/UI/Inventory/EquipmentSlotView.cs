using System;
using GameShared.Models;
using PhamNhanOnline.Client.UI.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Inventory
{
    public sealed class EquipmentSlotView : MonoBehaviour,
        IUiDragPayloadSource,
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
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject emptyStateRoot;

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
        }
    }
}
