using System;
using PhamNhanOnline.Client.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.World
{
    [DisallowMultipleComponent]
    public sealed class ArtMaterialSummaryView : ViewModelBase
    {
        [SerializeField] private GameObject summaryRoot;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text artQiRateText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text expText;
        [SerializeField] private Image expBarFillImage;

        protected override GameObject ResolveViewRoot()
        {
            return summaryRoot != null ? summaryRoot : gameObject;
        }

        public void Show(bool force = false)
        {
            ShowView(force);
        }

        public void Hide(bool force = false)
        {
            SetViewVisible(false, force);
        }

        public void ValidateSerializedReferences()
        {
            ThrowIfMissing(iconImage, nameof(iconImage));
            ThrowIfMissing(nameText, nameof(nameText));
            ThrowIfMissing(artQiRateText, nameof(artQiRateText));
            ThrowIfMissing(levelText, nameof(levelText));
            ThrowIfMissing(expText, nameof(expText));
        }

        public void SetData(
            Sprite icon,
            string name,
            string artQiRate,
            string level,
            string exp,
            long currentExp,
            long requiredExp,
            bool force)
        {
            ApplyText(nameText, name, force);
            ApplyText(artQiRateText, artQiRate, force);
            ApplyText(levelText, level, force);
            ApplyText(expText, exp, force);
            ApplyImage(iconImage, icon, force);
            ApplyExpFill(currentExp, requiredExp, force);
        }

        public void Clear(bool force)
        {
            SetData(
                null,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                0L,
                0L,
                force);
        }

        private static void ApplyText(TMP_Text text, string value, bool force)
        {
            if (text == null)
                return;

            var normalized = value ?? string.Empty;
            if (!force && string.Equals(text.text, normalized, StringComparison.Ordinal))
                return;

            text.text = normalized;
        }

        private static void ApplyImage(Image image, Sprite sprite, bool force)
        {
            if (image == null)
                return;

            if (!force && image.sprite == sprite)
                return;

            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        private void ApplyExpFill(long currentExp, long requiredExp, bool force)
        {
            if (expBarFillImage == null)
                return;

            var normalizedRequiredExp = Math.Max(0L, requiredExp);
            var normalizedCurrentExp = Math.Max(0L, currentExp);
            var fillAmount = normalizedRequiredExp > 0L
                ? Mathf.Clamp01((float)normalizedCurrentExp / normalizedRequiredExp)
                : 0f;

            if (!force && Mathf.Abs(expBarFillImage.fillAmount - fillAmount) < 0.0001f)
                return;

            expBarFillImage.fillAmount = fillAmount;
        }

        private void ThrowIfMissing(UnityEngine.Object value, string fieldName)
        {
            if (value == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(ArtMaterialSummaryView)} on '{gameObject.name}' is missing required reference '{fieldName}'.");
            }
        }
    }
}
