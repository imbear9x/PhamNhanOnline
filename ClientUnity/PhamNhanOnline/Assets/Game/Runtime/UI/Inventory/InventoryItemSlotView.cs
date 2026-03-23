using System;
using GameShared.Models;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Inventory
{
    public sealed class InventoryItemSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("References")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text quantityText;
        [SerializeField] private GameObject quantityRoot;
        [SerializeField] private GameObject equippedMarkerRoot;
        [SerializeField] private TMP_Text enhanceLevelText;
        [SerializeField] private GameObject enhanceLevelRoot;
        [SerializeField] private GameObject selectedHighlightRoot;

        [Header("Display")]
        [SerializeField] private Color emptyIconColor = new Color(1f, 1f, 1f, 0f);
        [SerializeField] private Color filledIconColor = Color.white;

        private InventoryItemModel item;
        private bool hasItem;
        private long lastPlayerItemId = long.MinValue;
        private int lastQuantity = int.MinValue;
        private bool lastEquippedState;
        private int lastEnhanceLevel = int.MinValue;
        private Sprite lastIconSprite;
        private Sprite lastBackgroundSprite;
        private bool isSelected;

        public event Action<InventoryItemSlotView> Clicked;
        public event Action<InventoryItemSlotView> Hovered;
        public event Action<InventoryItemSlotView> HoverExited;

        public InventoryItemModel Item => item;
        public bool HasItem => hasItem;

        public void SetItem(InventoryItemModel value, InventoryItemPresentation presentation, bool force = false)
        {
            hasItem = true;
            item = value;

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
                iconImage.color = presentation.IconSprite != null ? filledIconColor : emptyIconColor;
            }

            var hasQuantity = value.Quantity > 1;
            if (quantityRoot != null)
                quantityRoot.SetActive(hasQuantity);
            if (quantityText != null)
                quantityText.text = hasQuantity ? value.Quantity.ToString() : string.Empty;

            if (equippedMarkerRoot != null)
                equippedMarkerRoot.SetActive(value.IsEquipped);

            var hasEnhanceLevel = value.EnhanceLevel > 0;
            if (enhanceLevelRoot != null)
                enhanceLevelRoot.SetActive(hasEnhanceLevel);
            if (enhanceLevelText != null)
                enhanceLevelText.text = hasEnhanceLevel ? string.Format("+{0}", value.EnhanceLevel) : string.Empty;
        }

        public void Clear(bool force = false)
        {
            hasItem = false;
            item = default;
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
                iconImage.color = emptyIconColor;
            }

            if (quantityRoot != null)
                quantityRoot.SetActive(false);
            if (quantityText != null)
                quantityText.text = string.Empty;

            if (equippedMarkerRoot != null)
                equippedMarkerRoot.SetActive(false);

            if (enhanceLevelRoot != null)
                enhanceLevelRoot.SetActive(false);
            if (enhanceLevelText != null)
                enhanceLevelText.text = string.Empty;

            if (force)
                SetSelected(false, force: true);
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
    }
}
