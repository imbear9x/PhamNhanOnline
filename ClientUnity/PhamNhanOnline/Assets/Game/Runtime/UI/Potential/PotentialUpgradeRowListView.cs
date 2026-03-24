using System;
using System.Collections.Generic;
using GameShared.Models;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.Potential
{
    public sealed class PotentialUpgradeRowListView : MonoBehaviour
    {
        public readonly struct Entry
        {
            public Entry(PotentialAllocationTarget target, PotentialStatPresentation presentation, string currentValue)
            {
                Target = target;
                Presentation = presentation;
                CurrentValue = currentValue;
            }

            public PotentialAllocationTarget Target { get; }
            public PotentialStatPresentation Presentation { get; }
            public string CurrentValue { get; }
        }

        [Header("References")]
        [SerializeField] private Transform contentRoot;
        [SerializeField] private PotentialUpgradeRowView itemTemplate;

        [Header("Behavior")]
        [SerializeField] private bool hideTemplateObject = true;

        private readonly List<PotentialUpgradeRowView> spawnedItems = new List<PotentialUpgradeRowView>(8);
        private string lastSnapshot = string.Empty;
        private int lastEntryCount = -1;

        public event Action<PotentialUpgradeRowView> RowClicked;
        public event Action<PotentialUpgradeRowView> RowHovered;
        public event Action<PotentialUpgradeRowView> RowHoverExited;

        private void Awake()
        {
            if (contentRoot == null && itemTemplate != null)
                contentRoot = itemTemplate.transform.parent;

            if (hideTemplateObject && itemTemplate != null)
                itemTemplate.gameObject.SetActive(false);
        }

        public void SetEntries(IReadOnlyList<Entry> entries, bool force = false)
        {
            entries ??= Array.Empty<Entry>();

            var snapshot = BuildSnapshot(entries);
            if (!force &&
                lastEntryCount == entries.Count &&
                string.Equals(lastSnapshot, snapshot, StringComparison.Ordinal))
            {
                return;
            }

            lastEntryCount = entries.Count;
            lastSnapshot = snapshot;

            EnsureItemCount(entries.Count);
            for (var i = 0; i < spawnedItems.Count; i++)
            {
                var item = spawnedItems[i];
                if (item == null)
                    continue;

                var shouldBeVisible = i < entries.Count;
                if (item.gameObject.activeSelf != shouldBeVisible)
                    item.gameObject.SetActive(shouldBeVisible);

                if (!shouldBeVisible)
                    continue;

                item.SetContent(entries[i].Target, entries[i].Presentation, entries[i].CurrentValue, force: true);
            }
        }

        public void Clear(bool force = false)
        {
            SetEntries(Array.Empty<Entry>(), force);
        }

        private void EnsureItemCount(int targetCount)
        {
            if (targetCount <= spawnedItems.Count)
                return;

            if (itemTemplate == null)
            {
                Debug.LogWarning("PotentialUpgradeRowListView is missing itemTemplate.");
                return;
            }

            var parent = contentRoot != null ? contentRoot : itemTemplate.transform.parent;
            for (var i = spawnedItems.Count; i < targetCount; i++)
            {
                var instance = Instantiate(itemTemplate, parent);
                instance.name = string.Format("{0}_{1}", itemTemplate.name, i);
                instance.gameObject.SetActive(true);
                instance.Clicked += HandleRowClicked;
                instance.Hovered += HandleRowHovered;
                instance.HoverExited += HandleRowHoverExited;
                spawnedItems.Add(instance);
            }
        }

        private void HandleRowClicked(PotentialUpgradeRowView row)
        {
            RowClicked?.Invoke(row);
        }

        private void HandleRowHovered(PotentialUpgradeRowView row)
        {
            RowHovered?.Invoke(row);
        }

        private void HandleRowHoverExited(PotentialUpgradeRowView row)
        {
            RowHoverExited?.Invoke(row);
        }

        private static string BuildSnapshot(IReadOnlyList<Entry> entries)
        {
            if (entries == null || entries.Count == 0)
                return string.Empty;

            var parts = new string[entries.Count];
            for (var i = 0; i < entries.Count; i++)
            {
                parts[i] = string.Concat(
                    ((int)entries[i].Target).ToString(),
                    "=",
                    entries[i].CurrentValue ?? string.Empty,
                    "@",
                    entries[i].Presentation.DisplayName ?? string.Empty);
            }

            return string.Join("|", parts);
        }
    }
}
