using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace PhamNhanOnline.Client.UI.Inventory
{
    public sealed class InventoryCharacterSummaryView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text characterNameText;
        [SerializeField] private TMP_Text lifespanText;
        [SerializeField] private TMP_Text hpValueText;
        [SerializeField] private TMP_Text mpValueText;
        [SerializeField] private TMP_Text atkValueText;
        [SerializeField] private TMP_Text speedValueText;
        [FormerlySerializedAs("fortuneValueText")]
        [SerializeField] private TMP_Text luckValueText;
        [FormerlySerializedAs("spiritualSenseValueText")]
        [SerializeField] private TMP_Text senseValueText;

        private string lastCharacterName = string.Empty;
        private string lastStatsSnapshot = string.Empty;
        private long? lifespanEndUnixMs;
        private string lastLifespanText = string.Empty;
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
