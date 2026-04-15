using System;
using System.Globalization;
using PhamNhanOnline.Client.UI.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Crafting
{
    public sealed class CraftResultPreviewView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private Image progressFillImage;
        [SerializeField] private TMP_Text progressText;

        private void Start()
        {
            ValidateSerializedReferences();
        }

        public void SetState(
            InventoryItemPresentation presentation,
            int quantity,
            float hiddenFillAmount,
            string progressLabel)
        {
            SetVisible(true);

            if (iconImage != null)
            {
                iconImage.sprite = presentation.IconSprite;
                iconImage.enabled = presentation.IconSprite != null;
            }

            if (countText != null)
                countText.text = Math.Max(0, quantity).ToString(CultureInfo.InvariantCulture);

            if (progressFillImage != null)
            {
                progressFillImage.enabled = true;
                progressFillImage.fillAmount = Mathf.Clamp01(hiddenFillAmount);
            }

            if (progressText != null)
                progressText.text = progressLabel ?? string.Empty;
        }

        public void Clear()
        {
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            if (countText != null)
                countText.text = string.Empty;

            if (progressFillImage != null)
            {
                progressFillImage.fillAmount = 0f;
                progressFillImage.enabled = false;
            }

            if (progressText != null)
                progressText.text = string.Empty;

            SetVisible(false);
        }

        private void ValidateSerializedReferences()
        {
            ThrowIfMissing(iconImage, nameof(iconImage));
            ThrowIfMissing(countText, nameof(countText));
            ThrowIfMissing(progressFillImage, nameof(progressFillImage));
            ThrowIfMissing(progressText, nameof(progressText));
        }

        private void SetVisible(bool visible)
        {
            SetComponentVisible(iconImage, visible);
            SetComponentVisible(countText, visible);
            SetComponentVisible(progressFillImage, visible);
            SetComponentVisible(progressText, visible);
        }

        private static void SetComponentVisible(Component component, bool visible)
        {
            if (component == null)
                return;

            var target = component.gameObject;
            if (target.activeSelf != visible)
                target.SetActive(visible);
        }

        private void ThrowIfMissing(UnityEngine.Object value, string fieldName)
        {
            if (value == null)
                throw new InvalidOperationException($"{nameof(CraftResultPreviewView)} on '{gameObject.name}' is missing required reference '{fieldName}'.");
        }
    }
}
