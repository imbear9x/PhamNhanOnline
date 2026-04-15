using System;
using System.Collections.Generic;
using System.Globalization;
using PhamNhanOnline.Client.UI.Common;
using TMPro;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.Inventory
{
    public sealed class InventoryCharacterSummaryView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text characterNameText;
        [SerializeField] private StatLineListView statListView;
        [SerializeField] private TMP_Text lifespanText;

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

        public void SetStatEntries(IReadOnlyList<StatLineListView.Entry> entries, bool force = false)
        {
            var snapshot = BuildStatSnapshot(entries);
            if (!force && string.Equals(lastStatsSnapshot, snapshot, StringComparison.Ordinal))
                return;

            lastStatsSnapshot = snapshot;
            if (statListView != null)
                statListView.SetEntries(entries ?? Array.Empty<StatLineListView.Entry>(), force: true);
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
            SetStatEntries(Array.Empty<StatLineListView.Entry>(), force);
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

        private static string BuildStatSnapshot(IReadOnlyList<StatLineListView.Entry> entries)
        {
            if (entries == null || entries.Count == 0)
                return string.Empty;

            var parts = new List<string>(entries.Count);
            for (var i = 0; i < entries.Count; i++)
                parts.Add(string.Concat(entries[i].Name ?? string.Empty, "=", entries[i].Value ?? string.Empty));

            return string.Join("|", parts);
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
