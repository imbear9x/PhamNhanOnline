using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Inventory
{
    public sealed class CharacterSummaryView : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Optional. Leave empty when this panel does not show the character name.")]
        [SerializeField] private TMP_Text characterNameText;
        [Tooltip("Optional. Leave empty when this panel does not show lifespan.")]
        [SerializeField] private TMP_Text lifespanText;
        [Tooltip("Optional. Leave empty when this panel does not show HP.")]
        [SerializeField] private TMP_Text hpValueText;
        [Tooltip("Optional. Leave empty when this panel does not show MP.")]
        [SerializeField] private TMP_Text mpValueText;
        [Tooltip("Optional. Leave empty when this panel does not show attack.")]
        [SerializeField] private TMP_Text atkValueText;
        [Tooltip("Optional. Leave empty when this panel does not show speed.")]
        [SerializeField] private TMP_Text speedValueText;
        [Tooltip("Optional. Leave empty when this panel does not show luck.")]
        [FormerlySerializedAs("fortuneValueText")]
        [SerializeField] private TMP_Text luckValueText;
        [Tooltip("Optional. Leave empty when this panel does not show sense.")]
        [FormerlySerializedAs("spiritualSenseValueText")]
        [SerializeField] private TMP_Text senseValueText;
        [Tooltip("Optional. Leave empty when this panel does not show realm name.")]
        [SerializeField] private TMP_Text realmNameText;
        [Tooltip("Optional. Leave empty when this panel does not show cultivation progress text.")]
        [SerializeField] private TMP_Text cultivationProgressText;
        [Tooltip("Optional. Leave empty when this panel does not show cultivation progress fill.")]
        [SerializeField] private Image cultivationProgressFillImage;
        [Tooltip("Optional. Leave empty when this panel does not show unallocated potential.")]
        [SerializeField] private TMP_Text unallocatedPotentialText;

        private string lastCharacterName = string.Empty;
        private string lastStatsSnapshot = string.Empty;
        private string lastRealmSnapshot = string.Empty;
        private long? lifespanEndUnixMs;
        private string lastLifespanText = string.Empty;
        private float lastCultivationFillAmount = -1f;
        private float nextLifespanRefreshAtUnscaled;

        public void SetCharacterName(string characterName, bool force = false)
        {
            characterName = string.IsNullOrWhiteSpace(characterName) ? "-" : characterName.Trim();
            if (!force && string.Equals(lastCharacterName, characterName, StringComparison.Ordinal))
                return;

            lastCharacterName = characterName;
            if (characterNameText != null)
                characterNameText.text = characterName;
        }

        public void SetStats(
            string hpValue,
            string mpValue,
            string atkValue,
            string speedValue,
            string luckValue,
            string senseValue,
            bool force = false)
        {
            hpValue = NormalizeStatValue(hpValue);
            mpValue = NormalizeStatValue(mpValue);
            atkValue = NormalizeStatValue(atkValue);
            speedValue = NormalizeStatValue(speedValue);
            luckValue = NormalizeStatValue(luckValue);
            senseValue = NormalizeStatValue(senseValue);

            var snapshot = string.Join(
                "|",
                hpValue,
                mpValue,
                atkValue,
                speedValue,
                luckValue,
                senseValue);
            if (!force && string.Equals(lastStatsSnapshot, snapshot, StringComparison.Ordinal))
                return;

            lastStatsSnapshot = snapshot;
            ApplyStatValue(hpValueText, hpValue, force: true);
            ApplyStatValue(mpValueText, mpValue, force: true);
            ApplyStatValue(atkValueText, atkValue, force: true);
            ApplyStatValue(speedValueText, speedValue, force: true);
            ApplyStatValue(luckValueText, luckValue, force: true);
            ApplyStatValue(senseValueText, senseValue, force: true);
        }

        public void SetRealmProgress(
            string realmName,
            string cultivationProgressValue,
            string unallocatedPotentialValue,
            float cultivationFillAmount,
            bool force = false)
        {
            realmName = NormalizeStatValue(realmName);
            cultivationProgressValue = NormalizeStatValue(cultivationProgressValue);
            unallocatedPotentialValue = NormalizeStatValue(unallocatedPotentialValue);

            var normalizedFillAmount = Mathf.Clamp01(cultivationFillAmount);
            var snapshot = string.Join(
                "|",
                realmName,
                cultivationProgressValue,
                unallocatedPotentialValue);
            if (!force &&
                string.Equals(lastRealmSnapshot, snapshot, StringComparison.Ordinal) &&
                Mathf.Approximately(lastCultivationFillAmount, normalizedFillAmount))
            {
                return;
            }

            lastRealmSnapshot = snapshot;
            lastCultivationFillAmount = normalizedFillAmount;
            ApplyStatValue(realmNameText, realmName, force: true);
            ApplyStatValue(cultivationProgressText, cultivationProgressValue, force: true);
            ApplyStatValue(unallocatedPotentialText, unallocatedPotentialValue, force: true);
            ApplyFillAmount(cultivationProgressFillImage, normalizedFillAmount, force: true);
        }

        public void SetLifespanEndUnixMs(long? value, bool force = false)
        {
            if (!force && lifespanEndUnixMs == value)
                return;

            lifespanEndUnixMs = value;
            nextLifespanRefreshAtUnscaled = 0f;
            RefreshLifespanText(force: true);
        }

        public void Clear(bool force = false)
        {
            SetCharacterName("-", force);
            SetStats("-", "-", "-", "-", "-", "-", force);
            SetRealmProgress("-", "-", "-", 0f, force);
            SetLifespanEndUnixMs(null, force: true);
        }

        private void OnEnable()
        {
            nextLifespanRefreshAtUnscaled = 0f;
            RefreshLifespanText(force: true);
        }

        private void Update()
        {
            if (lifespanText == null || !lifespanEndUnixMs.HasValue)
                return;

            if (Time.unscaledTime < nextLifespanRefreshAtUnscaled)
                return;

            RefreshLifespanText(force: false);
        }

        private static string NormalizeStatValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
        }

        private static void ApplyStatValue(TMP_Text text, string value, bool force)
        {
            if (text == null)
                return;

            if (!force && string.Equals(text.text, value, StringComparison.Ordinal))
                return;

            text.text = value;
        }

        private static void ApplyFillAmount(Image image, float value, bool force)
        {
            if (image == null)
                return;

            var normalized = Mathf.Clamp01(value);
            if (!force && Mathf.Approximately(image.fillAmount, normalized))
                return;

            image.fillAmount = normalized;
        }

        private void RefreshLifespanText(bool force)
        {
            if (lifespanText == null)
                return;

            var text = FormatRemainingLifespan(lifespanEndUnixMs);
            if (!force && string.Equals(lastLifespanText, text, StringComparison.Ordinal))
            {
                nextLifespanRefreshAtUnscaled = Time.unscaledTime + 1f;
                return;
            }

            lastLifespanText = text;
            lifespanText.text = text;
            nextLifespanRefreshAtUnscaled = Time.unscaledTime + 1f;
        }

        private static string FormatRemainingLifespan(long? endUnixMs)
        {
            if (!endUnixMs.HasValue || endUnixMs.Value <= 0)
                return "-";

            var nowUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var remainingMs = Math.Max(0L, endUnixMs.Value - nowUnixMs);
            var remaining = TimeSpan.FromMilliseconds(remainingMs);
            if (remaining <= TimeSpan.Zero)
                return "00:00:00";

            if (remaining.Days > 0)
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}d {1:00}:{2:00}:{3:00}",
                    remaining.Days,
                    remaining.Hours,
                    remaining.Minutes,
                    remaining.Seconds);
            }

            var totalHours = Math.Max(0, (int)Math.Floor(remaining.TotalHours));
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:00}:{1:00}:{2:00}",
                totalHours,
                remaining.Minutes,
                remaining.Seconds);
        }
    }
}
