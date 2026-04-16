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
    public sealed class CraftMaterialSlotView : MonoBehaviour, IDropHandler, IPointerClickHandler
    {
        [Header("References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private GameObject emptyIconRoot;
        [SerializeField] private GameObject selectedRoot;
        [SerializeField] private GameObject lockedRoot;

        [Header("Display")]
        [SerializeField] private Color insufficientCountColor = Color.white;
        [SerializeField] private Color sufficientCountColor = Color.white;
        private bool interactionLocked;

        public event Action<CraftMaterialSlotView, InventoryItemModel> InventoryItemDropped;
        public event Action<CraftMaterialSlotView, PointerEventData.InputButton> Clicked;

        private void Awake()
        {
        }

        private void Start()
        {
            ValidateSerializedReferences();
        }

        public void SetState(
            InventoryItemPresentation presentation,
            int currentQuantity,
            int requiredQuantity,
            bool hasSelection,
            bool locked,
            bool showEmptyIcon = true)
        {
            interactionLocked = locked;
            var showFilledVisual = hasSelection || locked;

            if (iconImage != null)
            {
                iconImage.sprite = presentation.IconSprite;
                iconImage.enabled = showFilledVisual && presentation.IconSprite != null;
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
            if (lockedRoot != null)
                lockedRoot.SetActive(locked);
        }

        public void Clear()
        {
            interactionLocked = false;

            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
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
            if (lockedRoot != null)
                lockedRoot.SetActive(false);
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (interactionLocked)
                return;

            if (!UIDragPayloadResolver.TryResolve(eventData, out var payload) ||
                payload.Kind != UIDragPayloadKind.InventoryItem ||
                !payload.HasInventoryItem ||
                payload.SourceKind != UIDragSourceKind.InventoryGridItem)
            {
                return;
            }

            InventoryItemDropped?.Invoke(this, payload.InventoryItem);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Clicked?.Invoke(this, eventData.button);
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
                throw new InvalidOperationException($"{nameof(CraftMaterialSlotView)} on '{gameObject.name}' is missing required reference '{fieldName}'.");
        }

    }
}
