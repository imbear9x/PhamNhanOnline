using System;
using GameShared.Models;
using PhamNhanOnline.Client.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Potential
{
    public sealed class PotentialUpgradeRowView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIButtonView buttonView;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text valueText;

        [Header("Display")]
        [SerializeField] private Color missingIconColor = new Color(1f, 1f, 1f, 0f);
        [SerializeField] private Color visibleIconColor = Color.white;

        private PotentialAllocationTarget target;
        private string lastDisplayName = string.Empty;
        private string lastValue = string.Empty;
        private Sprite lastIconSprite;

        public event Action<PotentialUpgradeRowView> Clicked;

        public PotentialAllocationTarget Target => target;

        private void Awake()
        {
            if (buttonView == null)
                buttonView = GetComponent<UIButtonView>();

            if (buttonView != null)
                buttonView.Clicked += HandleButtonClicked;
        }

        private void OnDestroy()
        {
            if (buttonView != null)
                buttonView.Clicked -= HandleButtonClicked;
        }

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

        public void SetButtonInteractable(bool interactable, bool force = false)
        {
            if (buttonView != null)
                buttonView.SetInteractable(interactable, force);
        }

        private void HandleButtonClicked()
        {
            Debug.LogWarning(
                $"[PotentialPopupDebug] Row click target={target} name='{lastDisplayName}' value='{lastValue}' object='{gameObject.name}'.");
            Clicked?.Invoke(this);
        }
    }
}
