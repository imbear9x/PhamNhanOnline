using System;
using GameShared.Models;
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
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image fillImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private TMP_Text stateText;
        [SerializeField] private GameObject optionalBadgeRoot;
        [SerializeField] private GameObject selectedRoot;
        [SerializeField] private GameObject lockedRoot;

        [Header("Text")]
        [SerializeField] private string emptyName = "Nguyen lieu";
        [SerializeField] private string emptyState = "Chua chon";
        [SerializeField] [Range(0.05f, 1f)] private float ghostIconAlpha = 0.35f;
        [SerializeField] [Range(0.05f, 1f)] private float activeIconAlpha = 1f;
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
            string itemName,
            InventoryItemPresentation presentation,
            int currentQuantity,
            int requiredQuantity,
            bool hasSelection,
            bool locked,
            string stateLabel = null,
            bool showOptionalBadge = false)
        {
            interactionLocked = locked;

            if (iconImage != null)
            {
                iconImage.sprite = presentation.IconSprite;
                iconImage.enabled = presentation.IconSprite != null;
                SetImageAlpha(iconImage, hasSelection || locked ? activeIconAlpha : ghostIconAlpha);
            }

            if (backgroundImage != null)
            {
                backgroundImage.sprite = presentation.BackgroundSprite;
                SetImageAlpha(backgroundImage, hasSelection || locked ? activeIconAlpha : ghostIconAlpha);
            }

            if (nameText != null)
                nameText.text = string.IsNullOrWhiteSpace(itemName) ? emptyName : itemName.Trim();

            var resolvedRequiredQuantity = Math.Max(1, requiredQuantity);
            var resolvedCurrentQuantity = Math.Max(0, currentQuantity);
            var fill = Mathf.Clamp01((float)resolvedCurrentQuantity / resolvedRequiredQuantity);
            if (fillImage != null)
                fillImage.fillAmount = fill;

            if (countText != null)
                countText.text = string.Concat(resolvedCurrentQuantity, "/", resolvedRequiredQuantity);

            if (stateText != null)
            {
                if (!string.IsNullOrWhiteSpace(stateLabel))
                {
                    stateText.text = stateLabel.Trim();
                }
                else if (locked)
                {
                    stateText.text = "Dang khoa";
                }
                else if (fill >= 1f)
                {
                    stateText.text = "Da du";
                }
                else
                {
                    stateText.text = hasSelection ? "Dang them" : emptyState;
                }
            }

            if (optionalBadgeRoot != null)
                optionalBadgeRoot.SetActive(showOptionalBadge);
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
                SetImageAlpha(iconImage, activeIconAlpha);
            }

            if (backgroundImage != null)
            {
                backgroundImage.sprite = null;
                SetImageAlpha(backgroundImage, activeIconAlpha);
            }
            if (fillImage != null)
                fillImage.fillAmount = 0f;
            if (nameText != null)
                nameText.text = emptyName;
            if (countText != null)
                countText.text = "0/0";
            if (stateText != null)
                stateText.text = emptyState;
            if (optionalBadgeRoot != null)
                optionalBadgeRoot.SetActive(false);
            if (selectedRoot != null)
                selectedRoot.SetActive(false);
            if (lockedRoot != null)
                lockedRoot.SetActive(false);
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (interactionLocked || eventData.pointerDrag == null)
                return;

            var inventorySlotView = eventData.pointerDrag.GetComponentInParent<InventoryItemSlotView>();
            if (inventorySlotView == null || !inventorySlotView.HasItem)
                return;

            InventoryItemDropped?.Invoke(this, inventorySlotView.Item);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Clicked?.Invoke(this, eventData.button);
        }

        private void ValidateSerializedReferences()
        {
            ThrowIfMissing(iconImage, nameof(iconImage));
            ThrowIfMissing(backgroundImage, nameof(backgroundImage));
            ThrowIfMissing(fillImage, nameof(fillImage));
            ThrowIfMissing(nameText, nameof(nameText));
            ThrowIfMissing(countText, nameof(countText));
            ThrowIfMissing(selectedRoot, nameof(selectedRoot));
        }

        private void ThrowIfMissing(UnityEngine.Object value, string fieldName)
        {
            if (value == null)
                throw new InvalidOperationException($"{nameof(CraftMaterialSlotView)} on '{gameObject.name}' is missing required reference '{fieldName}'.");
        }

        private static void SetImageAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null)
                return;

            var color = graphic.color;
            color.a = Mathf.Clamp01(alpha);
            graphic.color = color;
        }
    }
}
