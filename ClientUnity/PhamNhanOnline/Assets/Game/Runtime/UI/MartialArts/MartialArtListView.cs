using System;
using System.Collections.Generic;
using System.Globalization;
using GameShared.Models;
using PhamNhanOnline.Client.UI.Common;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PhamNhanOnline.Client.UI.MartialArts
{
    public sealed class MartialArtListView : MonoBehaviour, IDropHandler
    {
        [Header("References")]
        [SerializeField] private Transform contentRoot;
        [SerializeField] private MartialArtListItemView itemTemplate;

        [Header("Behavior")]
        [SerializeField] private bool hideTemplateObject = true;

        private readonly List<MartialArtListItemView> spawnedItems = new List<MartialArtListItemView>(8);
        private string lastSnapshot = string.Empty;
        private int lastItemCount = -1;
        private int? selectedMartialArtId;

        public event Action<PlayerMartialArtModel> ItemClicked;
        public event Action<PlayerMartialArtModel> ItemHovered;
        public event Action ItemHoverExited;
        public event Action<PlayerMartialArtModel> ActiveMartialArtDroppedToList;

        private void Awake()
        {
            if (contentRoot == null && itemTemplate != null)
                contentRoot = itemTemplate.transform.parent;

            if (hideTemplateObject && itemTemplate != null)
                itemTemplate.gameObject.SetActive(false);
        }

        public void SetItems(
            IReadOnlyList<PlayerMartialArtModel> items,
            int? selectedActiveMartialArtId,
            MartialArtPresentationCatalog presentationCatalog,
            bool force = false)
        {
            items ??= Array.Empty<PlayerMartialArtModel>();
            var snapshot = BuildSnapshot(items);
            var selectionChanged = selectedMartialArtId != selectedActiveMartialArtId;
            selectedMartialArtId = selectedActiveMartialArtId;

            if (!force &&
                !selectionChanged &&
                lastItemCount == items.Count &&
                string.Equals(lastSnapshot, snapshot, StringComparison.Ordinal))
            {
                UpdateSelectionVisuals(force: false);
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
                    : new MartialArtPresentation(null);
                itemView.SetItem(items[i], presentation, force: true);
                itemView.SetSelected(
                    selectedMartialArtId.HasValue && items[i].MartialArtId == selectedMartialArtId.Value,
                    force: true);
            }
        }

        public void Clear(bool force = false)
        {
            lastItemCount = 0;
            lastSnapshot = string.Empty;
            selectedMartialArtId = null;

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
                Debug.LogWarning("MartialArtListView is missing itemTemplate.");
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
                instance.ActiveMartialArtDropped += HandleActiveMartialArtDropped;
                spawnedItems.Add(instance);
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (!UiDragPayloadResolver.TryResolve(eventData, out var payload) ||
                payload.Kind != UiDragPayloadKind.MartialArt ||
                payload.SourceKind != UiDragSourceKind.ActiveMartialArtSlot ||
                !payload.HasMartialArt)
            {
                return;
            }

            var handler = ActiveMartialArtDroppedToList;
            if (handler != null)
                handler(payload.MartialArt);
        }

        private void UpdateSelectionVisuals(bool force)
        {
            for (var i = 0; i < spawnedItems.Count; i++)
            {
                var itemView = spawnedItems[i];
                if (itemView == null || !itemView.gameObject.activeSelf || !itemView.HasItem)
                    continue;

                itemView.SetSelected(
                    selectedMartialArtId.HasValue && itemView.Item.MartialArtId == selectedMartialArtId.Value,
                    force);
            }
        }

        private void HandleItemClicked(MartialArtListItemView itemView)
        {
            if (itemView == null || !itemView.HasItem)
                return;

            var handler = ItemClicked;
            if (handler != null)
                handler(itemView.Item);
        }

        private void HandleItemHovered(MartialArtListItemView itemView)
        {
            if (itemView == null || !itemView.HasItem)
                return;

            var handler = ItemHovered;
            if (handler != null)
                handler(itemView.Item);
        }

        private void HandleItemHoverExited(MartialArtListItemView itemView)
        {
            var handler = ItemHoverExited;
            if (handler != null)
                handler();
        }

        private void HandleActiveMartialArtDropped(PlayerMartialArtModel martialArt)
        {
            var handler = ActiveMartialArtDroppedToList;
            if (handler != null)
                handler(martialArt);
        }

        private static string BuildSnapshot(IReadOnlyList<PlayerMartialArtModel> items)
        {
            if (items == null || items.Count == 0)
                return string.Empty;

            var parts = new string[items.Count];
            for (var i = 0; i < items.Count; i++)
            {
                parts[i] = string.Concat(
                    items[i].MartialArtId.ToString(CultureInfo.InvariantCulture),
                    ":",
                    items[i].CurrentStage.ToString(CultureInfo.InvariantCulture),
                    ":",
                    items[i].MaxStage.ToString(CultureInfo.InvariantCulture),
                    ":",
                    items[i].QiAbsorptionRate.ToString("0.####", CultureInfo.InvariantCulture),
                    ":",
                    items[i].IsActive ? "1" : "0",
                    ":",
                    items[i].Icon ?? string.Empty,
                    ":",
                    items[i].Name ?? string.Empty,
                    ":",
                    items[i].Category ?? string.Empty,
                    ":",
                    items[i].Description ?? string.Empty);
            }

            return string.Join("|", parts);
        }
    }
}
