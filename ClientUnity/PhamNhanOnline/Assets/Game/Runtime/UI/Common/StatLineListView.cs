using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.Common
{
    public sealed class StatLineListView : MonoBehaviour
    {
        [Serializable]
        public struct Entry
        {
            [SerializeField] private string name;
            [SerializeField] private string value;

            public Entry(string name, string value)
            {
                this.name = name;
                this.value = value;
            }

            public string Name => name;
            public string Value => value;
        }

        [Header("References")]
        [SerializeField] private Transform contentRoot;
        [SerializeField] private StatLineView itemTemplate;

        [Header("Behavior")]
        [SerializeField] private bool hideTemplateObject = true;

        private readonly List<StatLineView> spawnedItems = new List<StatLineView>(8);
        private int lastEntryCount = -1;
        private string lastSnapshot = string.Empty;

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
            if (!force && lastEntryCount == entries.Count && string.Equals(lastSnapshot, snapshot, StringComparison.Ordinal))
                return;

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

                item.SetValues(entries[i].Name, entries[i].Value, force: true);
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
                Debug.LogWarning("StatLineListView is missing itemTemplate.");
                return;
            }

            var parent = contentRoot != null ? contentRoot : itemTemplate.transform.parent;
            for (var i = spawnedItems.Count; i < targetCount; i++)
            {
                var instance = Instantiate(itemTemplate, parent);
                instance.name = string.Format("{0}_{1}", itemTemplate.name, i);
                instance.gameObject.SetActive(true);
                spawnedItems.Add(instance);
            }
        }

        private static string BuildSnapshot(IReadOnlyList<Entry> entries)
        {
            if (entries == null || entries.Count == 0)
                return string.Empty;

            var parts = new string[entries.Count];
            for (var i = 0; i < entries.Count; i++)
                parts[i] = string.Concat(entries[i].Name ?? string.Empty, "=", entries[i].Value ?? string.Empty);

            return string.Join("|", parts);
        }
    }
}
