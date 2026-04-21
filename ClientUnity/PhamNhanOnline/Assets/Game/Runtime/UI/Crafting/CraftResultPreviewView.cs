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
        [SerializeField] private GameObject detailRoot;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text successRateText;
        [SerializeField] private TMP_Text durationText;
        [SerializeField] private GameObject progressRoot;
        [SerializeField] private Image progressFillImage;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text statusText;

        [Header("Text")]
        [SerializeField] private string quantityPrefix = "x";

        private void Start()
        {
            ValidateSerializedReferences();
        }

        public void SetState(
            InventoryItemPresentation presentation,
            string itemName,
            string successRateLabel,
            string durationLabel,
            int quantity,
            float progressFillAmount,
            string progressLabel,
            string statusLabel)
        {
            SetVisible(true);

            if (iconImage != null)
            {
                iconImage.sprite = presentation.IconSprite;
                iconImage.enabled = presentation.IconSprite != null;
            }

            if (countText != null)
                countText.text = string.Concat(quantityPrefix ?? string.Empty, Math.Max(0, quantity).ToString(CultureInfo.InvariantCulture));

            if (nameText != null)
            {
                nameText.text = itemName ?? string.Empty;
                nameText.color = presentation.NameColor;
            }

            if (successRateText != null)
                successRateText.text = successRateLabel ?? string.Empty;

            if (durationText != null)
                durationText.text = durationLabel ?? string.Empty;

            if (progressFillImage != null)
            {
                progressFillImage.enabled = true;
                progressFillImage.fillAmount = Mathf.Clamp01(progressFillAmount);
            }

            if (progressText != null)
                progressText.text = progressLabel ?? string.Empty;

            if (progressRoot != null)
                progressRoot.SetActive(true);

            if (statusText != null)
            {
                statusText.text = statusLabel ?? string.Empty;
                statusText.gameObject.SetActive(!string.IsNullOrWhiteSpace(statusText.text));
            }
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

            if (nameText != null)
                nameText.text = string.Empty;

            if (successRateText != null)
                successRateText.text = string.Empty;

            if (durationText != null)
                durationText.text = string.Empty;

            if (progressFillImage != null)
            {
                progressFillImage.fillAmount = 0f;
                progressFillImage.enabled = false;
            }

            if (progressText != null)
                progressText.text = string.Empty;

            if (statusText != null)
            {
                statusText.text = string.Empty;
                statusText.gameObject.SetActive(false);
            }

            if (progressRoot != null)
                progressRoot.SetActive(false);

            SetVisible(false);
        }

        private void ValidateSerializedReferences()
        {
            ThrowIfMissing(detailRoot, nameof(detailRoot));
            ThrowIfMissing(iconImage, nameof(iconImage));
            ThrowIfMissing(countText, nameof(countText));
            ThrowIfMissing(nameText, nameof(nameText));
            ThrowIfMissing(successRateText, nameof(successRateText));
            ThrowIfMissing(durationText, nameof(durationText));
            ThrowIfMissing(progressFillImage, nameof(progressFillImage));
            ThrowIfMissing(progressText, nameof(progressText));
            ThrowIfMissing(statusText, nameof(statusText));
        }

        private void SetVisible(bool visible)
        {
            if (detailRoot != null && detailRoot.activeSelf != visible)
                detailRoot.SetActive(visible);
        }

        private void ThrowIfMissing(UnityEngine.Object value, string fieldName)
        {
            if (value == null)
                throw new InvalidOperationException($"{nameof(CraftResultPreviewView)} on '{gameObject.name}' is missing required reference '{fieldName}'.");
        }
    }
}
