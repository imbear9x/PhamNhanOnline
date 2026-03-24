using System;
using System.Collections.Generic;
using GameShared.Models;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PhamNhanOnline.Client.UI.Inventory
{
    public sealed class InventoryItemGridView : MonoBehaviour, IDropHandler
    {
        [Header("References")]
        [SerializeField] private Transform contentRoot;
        [SerializeField] private InventoryItemSlotView itemTemplate;

        [Header("Behavior")]
        [SerializeField] private bool hideTemplateObject = true;

        private readonly List<InventoryItemSlotView> spawnedItems = new List<InventoryItemSlotView>(24);
        private int lastItemCount = -1;
        private string lastSnapshot = string.Empty;
        private long? selectedPlayerItemId;

        public event Action<InventoryItemModel> ItemClicked;
        public event Action<InventoryItemModel> ItemHovered;
        public event Action ItemHoverExited;
        public event Action<InventoryEquipmentSlot> EquippedItemDropped;

        private void Awake()
        {
            if (contentRoot == null && itemTemplate != null)
                contentRoot = itemTemplate.transform.parent;

            if (hideTemplateObject && itemTemplate != null)
                itemTemplate.gameObject.SetActive(false);
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

            lastItemCount = items.Count;
            lastSnapshot = snapshot;

            EnsureItemCount(items.Count);
            for (var i = 0; i < spawnedItems.Count; i++)
            {
                var slot = spawnedItems[i];
                if (slot == null)
                    continue;

                var shouldBeVisible = i < items.Count;
                if (slot.gameObject.activeSelf != shouldBeVisible)
                    slot.gameObject.SetActive(shouldBeVisible);

                if (!shouldBeVisible)
                {
                    slot.Clear(force: true);
                    continue;
                }

                var presentation = presentationCatalog != null
                    ? presentationCatalog.Resolve(items[i])
                    : new InventoryItemPresentation(null, null, Color.white);
                slot.SetItem(items[i], presentation, force: true);
                slot.SetSelected(selectedPlayerItemId.HasValue && selectedPlayerItemId.Value == items[i].PlayerItemId, force: true);
            }
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
            lastItemCount = 0;
            lastSnapshot = string.Empty;
            selectedPlayerItemId = null;

            for (var i = 0; i < spawnedItems.Count; i++)
            {
                var slot = spawnedItems[i];
                if (slot == null)
                    continue;

                slot.Clear(force: true);
                if (slot.gameObject.activeSelf)
                    slot.gameObject.SetActive(false);
            }
        }

        private void EnsureItemCount(int targetCount)
        {
            if (targetCount <= spawnedItems.Count)
                return;

            if (itemTemplate == null)
            {
                Debug.LogWarning("InventoryItemGridView is missing itemTemplate.");
                return;
            }

            var parent = contentRoot != null ? contentRoot : itemTemplate.transform.parent;
            for (var i = spawnedItems.Count; i < targetCount; i++)
            {
                var instance = Instantiate(itemTemplate, parent);
                instance.name = string.Format("{0}_{1}", itemTemplate.name, i);
                instance.gameObject.SetActive(true);
                instance.Clicked += HandleSlotClicked;
                instance.Hovered += HandleSlotHovered;
                instance.HoverExited += HandleSlotHoverExited;
                instance.EquippedItemDroppedOnInventory += HandleEquippedItemDroppedOnInventory;
                spawnedItems.Add(instance);
            }
        }

        private void UpdateSelectionVisuals(bool force)
        {
            for (var i = 0; i < spawnedItems.Count; i++)
            {
                var slot = spawnedItems[i];
                if (slot == null || !slot.gameObject.activeSelf)
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

        private void HandleSlotHovered(InventoryItemSlotView slot)
        {
            if (slot == null || !slot.HasItem)
                return;

            var handler = ItemHovered;
            if (handler != null)
                handler(slot.Item);
        }

        private void HandleSlotHoverExited(InventoryItemSlotView slot)
        {
            var handler = ItemHoverExited;
            if (handler != null)
                handler();
        }

        private void HandleEquippedItemDroppedOnInventory(InventoryEquipmentSlot slot)
        {
            var handler = EquippedItemDropped;
            if (handler != null)
                handler(slot);
        }

        public void OnDrop(PointerEventData eventData)
        {
            var equipmentSlotView = eventData.pointerDrag != null
                ? eventData.pointerDrag.GetComponent<EquipmentSlotView>()
                : null;

            if (equipmentSlotView == null || !equipmentSlotView.HasItem)
                return;

            HandleEquippedItemDroppedOnInventory(equipmentSlotView.SlotType);
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
