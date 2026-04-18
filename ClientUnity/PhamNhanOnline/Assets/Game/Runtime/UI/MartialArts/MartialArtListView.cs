using System;
using System.Collections.Generic;
using System.Globalization;
using GameShared.Models;
using PhamNhanOnline.Client.UI.Common;
using PhamNhanOnline.Client.UI.World;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PhamNhanOnline.Client.UI.MartialArts
{
    [RequireComponent(typeof(LoopGridView))]
    public sealed class MartialArtListView : MonoBehaviour, IDropHandler
    {
        [Header("References")]
        [SerializeField] private LoopGridView loopGridView;

        private readonly HashSet<MartialArtListItemView> subscribedItems = new HashSet<MartialArtListItemView>();
        private IReadOnlyList<PlayerMartialArtModel> items = Array.Empty<PlayerMartialArtModel>();
        private MartialArtPresentationCatalog presentationCatalog;
        private string lastSnapshot = string.Empty;
        private int lastItemCount = -1;
        private int? selectedMartialArtId;
        private bool loopInitialized;

        public event Action<PlayerMartialArtModel> ItemClicked;
        public event Action<PlayerMartialArtModel> ItemHovered;
        public event Action ItemHoverExited;
        public event Action<PlayerMartialArtModel> ActiveMartialArtDroppedToList;

        private void Awake()
        {
            if (loopGridView == null)
                loopGridView = GetComponent<LoopGridView>();
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
            this.items = items;
            this.presentationCatalog = presentationCatalog;

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

            EnsureLoopInitialized();
            WorldModalUIManager.Instance?.HideItemTooltip(force: true);
            loopGridView.SetListItemCount(items.Count, keepPosition: true);
            loopGridView.RefreshAllShownItem();
        }

        public void Clear(bool force = false)
        {
            items = Array.Empty<PlayerMartialArtModel>();
            presentationCatalog = null;
            lastItemCount = 0;
            lastSnapshot = string.Empty;
            selectedMartialArtId = null;

            if (!ShouldKeepDebugGeneratedItems())
            {
                EnsureLoopInitialized();
                loopGridView.SetListItemCount(0, keepPosition: false);
                loopGridView.RefreshAllShownItem();
            }

            WorldModalUIManager.Instance?.HideItemTooltip(force: force);
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (!UIDragPayloadResolver.TryResolve(eventData, out var payload) ||
                payload.Kind != UIDragPayloadKind.MartialArt ||
                payload.SourceKind != UIDragSourceKind.ActiveMartialArtSlot ||
                !payload.HasMartialArt)
            {
                return;
            }

            var handler = ActiveMartialArtDroppedToList;
            if (handler != null)
                handler(payload.MartialArt);
        }

        private void EnsureLoopInitialized()
        {
            if (loopInitialized || loopGridView == null)
                return;

            if (ShouldKeepDebugGeneratedItems())
                return;

            loopGridView.InitGridView(items.Count, OnGetItemByIndex);
            loopInitialized = true;
        }

        private bool ShouldKeepDebugGeneratedItems()
        {
            return loopGridView != null &&
                   loopGridView.DebugUseGeneratedItemsEnabled &&
                   (items == null || items.Count == 0);
        }

        private void UpdateSelectionVisuals(bool force)
        {
            for (var i = 0; i < items.Count; i++)
            {
                var itemView = loopGridView.GetShownItemByItemIndex(i) as MartialArtListItemView;
                if (itemView == null || !itemView.HasItem)
                    continue;

                itemView.SetSelected(
                    selectedMartialArtId.HasValue && itemView.Item.MartialArtId == selectedMartialArtId.Value,
                    force);
            }
        }

        private LoopScrollViewItem OnGetItemByIndex(LoopGridView gridView, int itemIndex)
        {
            if (itemIndex < 0 || itemIndex >= items.Count)
                return null;

            var itemView = gridView.NewListViewItem() as MartialArtListItemView;
            if (itemView == null)
                return null;

            SubscribeItem(itemView);

            var item = items[itemIndex];
            var presentation = presentationCatalog != null
                ? presentationCatalog.Resolve(item)
                : new MartialArtPresentation(null);
            itemView.SetItem(item, presentation, force: true);
            itemView.SetSelected(
                selectedMartialArtId.HasValue && selectedMartialArtId.Value == item.MartialArtId,
                force: true);
            return itemView;
        }

        private void SubscribeItem(MartialArtListItemView itemView)
        {
            if (itemView == null || !subscribedItems.Add(itemView))
                return;

            itemView.Clicked += HandleItemClicked;
            itemView.Hovered += HandleItemHovered;
            itemView.HoverExited += HandleItemHoverExited;
            itemView.ActiveMartialArtDropped += HandleActiveMartialArtDropped;
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
