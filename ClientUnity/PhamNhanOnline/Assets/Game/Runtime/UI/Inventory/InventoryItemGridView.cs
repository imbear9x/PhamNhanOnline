using System;
using System.Collections.Generic;
using GameShared.Models;
using PhamNhanOnline.Client.UI.Common;
using PhamNhanOnline.Client.UI.World;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.Inventory
{
    [RequireComponent(typeof(LoopGridView))]
    public sealed class InventoryItemGridView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LoopGridView loopGridView;

        private readonly HashSet<InventoryItemSlotView> subscribedItems = new HashSet<InventoryItemSlotView>();
        private IReadOnlyList<InventoryItemModel> items = Array.Empty<InventoryItemModel>();
        private InventoryItemPresentationCatalog presentationCatalog;
        private int lastItemCount = -1;
        private string lastSnapshot = string.Empty;
        private long? selectedPlayerItemId;
        private bool loopInitialized;

        public event Action<InventoryItemModel> ItemClicked;

        private void Awake()
        {
            if (loopGridView == null)
                loopGridView = GetComponent<LoopGridView>();
        }

        private void Start()
        {
            ValidateSerializedReferences();
            EnsureLoopInitialized();
        }

        public void SetItems(IReadOnlyList<InventoryItemModel> items, InventoryItemPresentationCatalog presentationCatalog, bool force = false)
        {
            items ??= Array.Empty<InventoryItemModel>();

            var snapshot = BuildSnapshot(items);
            if (!force && lastItemCount == items.Count && string.Equals(lastSnapshot, snapshot, StringComparison.Ordinal))
            {
                UpdateSelectionVisuals(force: false);
                return;
            }

            this.items = items;
            this.presentationCatalog = presentationCatalog;
            lastItemCount = items.Count;
            lastSnapshot = snapshot;

            EnsureLoopInitialized();
            WorldModalUIManager.Instance?.HideItemTooltip(force: true);
            loopGridView.SetListItemCount(items.Count, keepPosition: true);
            loopGridView.RefreshAllShownItem();
        }

        public void SetSelectedItem(long? playerItemId, bool force = false)
        {
            if (!force && selectedPlayerItemId == playerItemId)
                return;

            selectedPlayerItemId = playerItemId;
            UpdateSelectionVisuals(force: true);
        }

        public void Clear(bool force = false)
        {
            items = Array.Empty<InventoryItemModel>();
            presentationCatalog = null;
            lastItemCount = 0;
            lastSnapshot = string.Empty;
            selectedPlayerItemId = null;

            EnsureLoopInitialized();
            loopGridView.SetListItemCount(0, keepPosition: false);
            loopGridView.RefreshAllShownItem();
            WorldModalUIManager.Instance?.HideItemTooltip(force: force);
        }

        private void UpdateSelectionVisuals(bool force)
        {
            if (loopGridView == null || items == null || items.Count == 0)
                return;

            for (var i = 0; i < items.Count; i++)
            {
                var slot = loopGridView.GetShownItemByItemIndex(i) as InventoryItemSlotView;
                if (slot == null || !slot.HasItem)
                    continue;

                slot.SetSelected(
                    selectedPlayerItemId.HasValue &&
                    slot.HasItem &&
                    slot.Item.PlayerItemId == selectedPlayerItemId.Value,
                    force);
            }
        }

        private void HandleSlotClicked(InventoryItemSlotView slot)
        {
            if (slot == null || !slot.HasItem)
                return;

            var handler = ItemClicked;
            if (handler != null)
                handler(slot.Item);
        }

        private void EnsureLoopInitialized()
        {
            if (loopInitialized || loopGridView == null)
                return;

            loopGridView.InitGridView(items.Count, OnGetItemByIndex);
            loopInitialized = true;
        }

        private LoopScrollViewItem OnGetItemByIndex(LoopGridView gridView, int itemIndex)
        {
            if (itemIndex < 0 || itemIndex >= items.Count)
                return null;

            var slotView = gridView.NewListViewItem() as InventoryItemSlotView;
            if (slotView == null)
                return null;

            SubscribeItem(slotView);

            var item = items[itemIndex];
            var presentation = presentationCatalog != null
                ? presentationCatalog.Resolve(item)
                : new InventoryItemPresentation(null, null, Color.white);
            slotView.SetItem(item, presentation, force: true);
            slotView.SetSelected(selectedPlayerItemId.HasValue && selectedPlayerItemId.Value == item.PlayerItemId, force: true);
            return slotView;
        }

        private void SubscribeItem(InventoryItemSlotView slotView)
        {
            if (slotView == null || !subscribedItems.Add(slotView))
                return;

            slotView.Clicked += HandleSlotClicked;
        }

        private void ValidateSerializedReferences()
        {
            if (loopGridView == null)
                throw new InvalidOperationException($"{nameof(InventoryItemGridView)} on '{gameObject.name}' is missing required reference '{nameof(loopGridView)}'.");
        }

        private static string BuildSnapshot(IReadOnlyList<InventoryItemModel> items)
        {
            if (items == null || items.Count == 0)
                return string.Empty;

            var parts = new string[items.Count];
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                parts[i] = string.Concat(
                    item.PlayerItemId.ToString(),
                    ":",
                    item.ItemTemplateId.ToString(),
                    ":",
                    item.Quantity.ToString(),
                    ":",
                    item.IsEquipped ? "1" : "0",
                    ":",
                    item.EnhanceLevel.ToString(),
                    ":",
                    item.Durability.HasValue ? item.Durability.Value.ToString() : "-",
                    ":",
                    item.Icon ?? string.Empty,
                    ":",
                    item.BackgroundIcon ?? string.Empty,
                    ":",
                    item.Name ?? string.Empty,
                    ":",
                    item.Description ?? string.Empty);
            }

            return string.Join("|", parts);
        }
    }
}
