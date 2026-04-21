using System;
using GameShared.Models;
using PhamNhanOnline.Client.UI.Common;
using PhamNhanOnline.Client.UI.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Crafting
{
    public sealed class CraftInputMaterialSlotView : MonoBehaviour,
        IUIDragPayloadSource,
        IDropHandler,
        IPointerClickHandler,
        IPointerEnterHandler,
        IPointerExitHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [Header("References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private GameObject emptyIconRoot;
        [SerializeField] private GameObject selectedRoot;

        [Header("Display")]
        [SerializeField] private float draggingAlpha = 0.65f;
        [SerializeField] [Range(0f, 1f)] private float placeholderIconAlpha = 0.65f;
        [SerializeField] private Color insufficientCountColor = Color.white;
        [SerializeField] private Color sufficientCountColor = Color.white;

        private int inputId;
        private int acceptedItemTemplateId;
        private bool hasSelection;
        private bool interactionLocked;
        private CanvasGroup canvasGroup;
        private InventoryDragGhost dragGhost;
        private InventoryItemPresentation currentPresentation;

        public event Action<CraftInputMaterialSlotView, InventoryItemModel> InventoryItemDropped;
        public event Action<CraftInputMaterialSlotView, PointerEventData.InputButton> Clicked;
        public event Action<CraftInputMaterialSlotView> Hovered;
        public event Action<CraftInputMaterialSlotView> HoverExited;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        private void Start()
        {
            ValidateSerializedReferences();
        }

        public void SetState(
            int resolvedInputId,
            int resolvedAcceptedItemTemplateId,
            InventoryItemPresentation presentation,
            int currentQuantity,
            int requiredQuantity,
            bool hasSelection,
            bool locked,
            bool showEmptyIcon = true)
        {
            inputId = resolvedInputId;
            acceptedItemTemplateId = Math.Max(0, resolvedAcceptedItemTemplateId);
            this.hasSelection = hasSelection;
            interactionLocked = locked;
            currentPresentation = presentation;
            var showFilledVisual = hasSelection;
            var showPlaceholderVisual = !showFilledVisual && showEmptyIcon && presentation.IconSprite != null;

            if (iconImage != null)
            {
                iconImage.sprite = presentation.IconSprite;
                iconImage.enabled = (showFilledVisual || showPlaceholderVisual) && presentation.IconSprite != null;
                iconImage.color = showFilledVisual
                    ? Color.white
                    : new Color(1f, 1f, 1f, Mathf.Clamp01(placeholderIconAlpha));
            }

            var resolvedRequiredQuantity = Math.Max(1, requiredQuantity);
            var resolvedCurrentQuantity = Math.Max(0, currentQuantity);
            if (countText != null)
            {
                countText.text = string.Concat(resolvedCurrentQuantity, "/", resolvedRequiredQuantity);
                countText.color = resolvedCurrentQuantity >= resolvedRequiredQuantity
                    ? sufficientCountColor
                    : insufficientCountColor;
            }

            if (emptyIconRoot != null)
                emptyIconRoot.SetActive(showEmptyIcon && !showFilledVisual);
            if (selectedRoot != null)
                selectedRoot.SetActive(hasSelection);
        }

        public void Clear()
        {
            inputId = 0;
            acceptedItemTemplateId = 0;
            hasSelection = false;
            interactionLocked = false;
            currentPresentation = default;

            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
                iconImage.color = Color.white;
            }
            if (countText != null)
            {
                countText.text = "0/0";
                countText.color = insufficientCountColor;
            }
            if (emptyIconRoot != null)
                emptyIconRoot.SetActive(true);
            if (selectedRoot != null)
                selectedRoot.SetActive(false);
            ResetDragVisuals();
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (interactionLocked)
                return;

            if (!UIDragPayloadResolver.TryResolve(eventData, out var payload) ||
                payload.Kind != UIDragPayloadKind.InventoryItem ||
                !payload.HasInventoryItem ||
                payload.SourceKind != UIDragSourceKind.InventoryGridItem ||
                (acceptedItemTemplateId > 0 && payload.InventoryItem.ItemTemplateId != acceptedItemTemplateId))
            {
                return;
            }

            InventoryItemDropped?.Invoke(this, payload.InventoryItem);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (interactionLocked)
                return;

            Clicked?.Invoke(this, eventData.button);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (hasSelection)
                Hovered?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (hasSelection)
                HoverExited?.Invoke(this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!hasSelection || interactionLocked)
                return;

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.alpha = draggingAlpha;
            }

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
        }

        public bool TryCreateDragPayload(out UIDragPayload payload)
        {
            if (!hasSelection || interactionLocked || inputId <= 0)
            {
                payload = default;
                return false;
            }

            payload = UIDragPayload.FromCraftInputMaterial(inputId);
            return true;
        }

        private void ValidateSerializedReferences()
        {
            ThrowIfMissing(iconImage, nameof(iconImage));
            ThrowIfMissing(countText, nameof(countText));
            ThrowIfMissing(selectedRoot, nameof(selectedRoot));
        }

        private void ThrowIfMissing(UnityEngine.Object value, string fieldName)
        {
            if (value == null)
                throw new InvalidOperationException($"{nameof(CraftInputMaterialSlotView)} on '{gameObject.name}' is missing required reference '{fieldName}'.");
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
