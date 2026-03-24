using System;
using GameShared.Models;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Potential
{
    public sealed class PotentialUpgradeRowView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private GameObject hoverHighlightRoot;

        [Header("Display")]
        [SerializeField] private Color missingIconColor = new Color(1f, 1f, 1f, 0f);
        [SerializeField] private Color visibleIconColor = Color.white;

        private PotentialAllocationTarget target;
        private string lastDisplayName = string.Empty;
        private string lastValue = string.Empty;
        private Sprite lastIconSprite;
        private bool isPointerInside;

        public event Action<PotentialUpgradeRowView> Clicked;
        public event Action<PotentialUpgradeRowView> Hovered;
        public event Action<PotentialUpgradeRowView> HoverExited;

        public PotentialAllocationTarget Target => target;
        public bool IsPointerInside => isPointerInside;

        public void SetContent(PotentialAllocationTarget valueTarget, PotentialStatPresentation presentation, string currentValue, bool force = false)
        {
            var displayName = string.IsNullOrWhiteSpace(presentation.DisplayName)
                ? PotentialStatPresentationCatalog.GetFallbackDisplayName(valueTarget)
                : presentation.DisplayName.Trim();
            currentValue = string.IsNullOrWhiteSpace(currentValue) ? "-" : currentValue.Trim();

            if (!force &&
                target == valueTarget &&
                string.Equals(lastDisplayName, displayName, StringComparison.Ordinal) &&
                string.Equals(lastValue, currentValue, StringComparison.Ordinal) &&
                lastIconSprite == presentation.IconSprite)
            {
                return;
            }

            target = valueTarget;
            lastDisplayName = displayName;
            lastValue = currentValue;
            lastIconSprite = presentation.IconSprite;

            if (iconImage != null)
            {
                iconImage.sprite = presentation.IconSprite;
                iconImage.color = presentation.IconSprite != null ? visibleIconColor : missingIconColor;
            }

            if (nameText != null)
                nameText.text = displayName;

            if (valueText != null)
                valueText.text = currentValue;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerInside = true;
            ApplyHoverVisual(true);
            Hovered?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerInside = false;
            ApplyHoverVisual(false);
            HoverExited?.Invoke(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Clicked?.Invoke(this);
        }

        private void OnDisable()
        {
            isPointerInside = false;
            ApplyHoverVisual(false);
        }

        private void ApplyHoverVisual(bool visible)
        {
            if (hoverHighlightRoot != null)
                hoverHighlightRoot.SetActive(visible);
        }
    }
}
