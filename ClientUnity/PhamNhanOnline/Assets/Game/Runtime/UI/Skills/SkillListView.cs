using System;
using System.Collections.Generic;
using System.Globalization;
using GameShared.Models;
using PhamNhanOnline.Client.UI.Common;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PhamNhanOnline.Client.UI.Skills
{
    public sealed class SkillListView : MonoBehaviour, IDropHandler
    {
        [Header("References")]
        [SerializeField] private Transform contentRoot;
        [SerializeField] private SkillListItemView itemTemplate;

        [Header("Behavior")]
        [SerializeField] private bool hideTemplateObject = true;

        private readonly List<SkillListItemView> spawnedItems = new List<SkillListItemView>(8);
        private string lastSnapshot = string.Empty;
        private int lastItemCount = -1;
        private long? selectedPlayerSkillId;

        public event Action<PlayerSkillModel> ItemClicked;
        public event Action<PlayerSkillModel> ItemHovered;
        public event Action ItemHoverExited;
        public event Action<PlayerSkillModel> EquippedSkillDroppedToList;

        private void Awake()
        {
            if (contentRoot == null && itemTemplate != null)
                contentRoot = itemTemplate.transform.parent;

            if (hideTemplateObject && itemTemplate != null)
                itemTemplate.gameObject.SetActive(false);
        }

        public void SetItems(
            IReadOnlyList<PlayerSkillModel> items,
            long? selectedSkillId,
            SkillPresentationCatalog presentationCatalog,
            bool force = false)
        {
            items ??= Array.Empty<PlayerSkillModel>();
            var snapshot = BuildSnapshot(items);
            var selectionChanged = selectedPlayerSkillId != selectedSkillId;
            selectedPlayerSkillId = selectedSkillId;

            if (!force &&
                !selectionChanged &&
                lastItemCount == items.Count &&
                string.Equals(lastSnapshot, snapshot, StringComparison.Ordinal))
            {
                UpdateSelectionVisuals(false);
                return;
            }

            lastItemCount = items.Count;
            lastSnapshot = snapshot;

            EnsureItemCount(items.Count);
            for (var i = 0; i < spawnedItems.Count; i++)
            {
                var itemView = spawnedItems[i];
                if (itemView == null)
                    continue;

                var shouldBeVisible = i < items.Count;
                if (itemView.gameObject.activeSelf != shouldBeVisible)
                    itemView.gameObject.SetActive(shouldBeVisible);

                if (!shouldBeVisible)
                {
                    itemView.Clear(force: true);
                    continue;
                }

                var presentation = presentationCatalog != null
                    ? presentationCatalog.Resolve(items[i])
                    : new SkillPresentation(null);
                itemView.SetItem(items[i], presentation, force: true);
                itemView.SetSelected(
                    selectedPlayerSkillId.HasValue && items[i].PlayerSkillId == selectedPlayerSkillId.Value,
                    force: true);
            }
        }

        public void Clear(bool force = false)
        {
            lastItemCount = 0;
            lastSnapshot = string.Empty;
            selectedPlayerSkillId = null;

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

        public void OnDrop(PointerEventData eventData)
        {
            if (!UIDragPayloadResolver.TryResolve(eventData, out var payload) ||
                payload.Kind != UIDragPayloadKind.Skill ||
                payload.SourceKind != UIDragSourceKind.SkillLoadoutSlot ||
                !payload.HasSkill)
            {
                return;
            }

            var handler = EquippedSkillDroppedToList;
            if (handler != null)
                handler(payload.Skill);
        }

        private void EnsureItemCount(int targetCount)
        {
            if (targetCount <= spawnedItems.Count)
                return;

            if (itemTemplate == null)
            {
                Debug.LogWarning("SkillListView is missing itemTemplate.");
                return;
            }

            var parent = contentRoot != null ? contentRoot : itemTemplate.transform.parent;
            for (var i = spawnedItems.Count; i < targetCount; i++)
            {
                var instance = Instantiate(itemTemplate, parent);
                instance.name = string.Format("{0}_{1}", itemTemplate.name, i);
                instance.gameObject.SetActive(true);
                instance.Clicked += HandleItemClicked;
                instance.Hovered += HandleItemHovered;
                instance.HoverExited += HandleItemHoverExited;
                spawnedItems.Add(instance);
            }
        }

        private void UpdateSelectionVisuals(bool force)
        {
            for (var i = 0; i < spawnedItems.Count; i++)
            {
                var itemView = spawnedItems[i];
                if (itemView == null || !itemView.gameObject.activeSelf || !itemView.HasItem)
                    continue;

                itemView.SetSelected(
                    selectedPlayerSkillId.HasValue && itemView.Item.PlayerSkillId == selectedPlayerSkillId.Value,
                    force);
            }
        }

        private void HandleItemClicked(SkillListItemView itemView)
        {
            if (itemView == null || !itemView.HasItem)
                return;

            var handler = ItemClicked;
            if (handler != null)
                handler(itemView.Item);
        }

        private void HandleItemHovered(SkillListItemView itemView)
        {
            if (itemView == null || !itemView.HasItem)
                return;

            var handler = ItemHovered;
            if (handler != null)
                handler(itemView.Item);
        }

        private void HandleItemHoverExited(SkillListItemView itemView)
        {
            var handler = ItemHoverExited;
            if (handler != null)
                handler();
        }

        private static string BuildSnapshot(IReadOnlyList<PlayerSkillModel> items)
        {
            if (items == null || items.Count == 0)
                return string.Empty;

            var parts = new string[items.Count];
            for (var i = 0; i < items.Count; i++)
            {
                parts[i] = string.Concat(
                    items[i].PlayerSkillId.ToString(CultureInfo.InvariantCulture),
                    ":",
                    items[i].SkillId.ToString(CultureInfo.InvariantCulture),
                    ":",
                    items[i].SkillLevel.ToString(CultureInfo.InvariantCulture),
                    ":",
                    items[i].CooldownMs.ToString(CultureInfo.InvariantCulture),
                    ":",
                    items[i].EquippedSlotIndex.ToString(CultureInfo.InvariantCulture),
                    ":",
                    items[i].Code ?? string.Empty,
                    ":",
                    items[i].Name ?? string.Empty,
                    ":",
                    items[i].SkillGroupCode ?? string.Empty,
                    ":",
                    items[i].SourceMartialArtName ?? string.Empty,
                    ":",
                    items[i].Description ?? string.Empty);
            }

            return string.Join("|", parts);
        }
    }
}
