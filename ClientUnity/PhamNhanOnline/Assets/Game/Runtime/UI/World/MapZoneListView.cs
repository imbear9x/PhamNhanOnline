using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.World
{
    public sealed class MapZoneListView : MonoBehaviour
    {
        [Serializable]
        public struct Entry
        {
            public int ZoneIndex;
            public string ZoneName;
            public string PlayerCountText;
            public Color BackgroundColor;
            public bool IsCurrentZone;
            public bool IsInteractable;
        }

        [Header("References")]
        [SerializeField] private Transform contentRoot;
        [SerializeField] private MapZoneListItemView itemTemplate;

        [Header("Behavior")]
        [SerializeField] private bool hideTemplateObject = true;

        private readonly List<MapZoneListItemView> spawnedItems = new List<MapZoneListItemView>(8);
        private string lastSnapshot = string.Empty;
        private int lastItemCount = -1;

        public event Action<MapZoneListItemView> ItemClicked;

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
                lastItemCount == entries.Count &&
                string.Equals(lastSnapshot, snapshot, StringComparison.Ordinal))
            {
                return;
            }

            lastItemCount = entries.Count;
            lastSnapshot = snapshot;

            EnsureItemCount(entries.Count);
            for (var i = 0; i < spawnedItems.Count; i++)
            {
                var itemView = spawnedItems[i];
                if (itemView == null)
                    continue;

                var shouldBeVisible = i < entries.Count;
                if (itemView.gameObject.activeSelf != shouldBeVisible)
                    itemView.gameObject.SetActive(shouldBeVisible);

                if (!shouldBeVisible)
                {
                    itemView.Clear(force: true);
                    continue;
                }

                itemView.SetEntry(entries[i], force: true);
            }
        }

        public void Clear(bool force = false)
        {
            lastItemCount = 0;
            lastSnapshot = string.Empty;

            for (var i = 0; i < spawnedItems.Count; i++)
            {
                var itemView = spawnedItems[i];
                if (itemView == null)
                    continue;

                itemView.Clear(force: true);
                if (itemView.gameObject.activeSelf)
                    itemView.gameObject.SetActive(false);
            }
        }

        private void EnsureItemCount(int targetCount)
        {
            if (targetCount <= spawnedItems.Count)
                return;

            if (itemTemplate == null)
            {
                Debug.LogWarning("MapZoneListView is missing itemTemplate.");
                return;
            }

            var parent = contentRoot != null ? contentRoot : itemTemplate.transform.parent;
            for (var i = spawnedItems.Count; i < targetCount; i++)
            {
                var instance = Instantiate(itemTemplate, parent);
                instance.name = string.Format("{0}_{1}", itemTemplate.name, i);
                instance.gameObject.SetActive(true);
                instance.Clicked += HandleItemClicked;
                spawnedItems.Add(instance);
            }
        }

        private void HandleItemClicked(MapZoneListItemView itemView)
        {
            if (itemView == null || !itemView.HasEntry)
                return;

            var handler = ItemClicked;
            if (handler != null)
                handler(itemView);
        }

        private static string BuildSnapshot(IReadOnlyList<Entry> entries)
        {
            if (entries == null || entries.Count == 0)
                return string.Empty;

            var parts = new string[entries.Count];
            for (var i = 0; i < entries.Count; i++)
            {
                parts[i] = string.Concat(
                    entries[i].ZoneIndex.ToString(CultureInfo.InvariantCulture),
                    ":",
                    entries[i].ZoneName ?? string.Empty,
                    ":",
                    entries[i].PlayerCountText ?? string.Empty,
                    ":",
                    entries[i].BackgroundColor.r.ToString("0.###", CultureInfo.InvariantCulture),
                    ",",
                    entries[i].BackgroundColor.g.ToString("0.###", CultureInfo.InvariantCulture),
                    ",",
                    entries[i].BackgroundColor.b.ToString("0.###", CultureInfo.InvariantCulture),
                    ",",
                    entries[i].BackgroundColor.a.ToString("0.###", CultureInfo.InvariantCulture),
                    ":",
                    entries[i].IsCurrentZone ? "1" : "0",
                    ":",
                    entries[i].IsInteractable ? "1" : "0");
            }

            return string.Join("|", parts);
        }
    }
}
