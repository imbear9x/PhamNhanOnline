using System;
using System.Collections.Generic;
using GameShared.Models;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.Inventory
{
    public sealed class EquipmentSlotsPanelView : MonoBehaviour
    {
        [Serializable]
        public sealed class SlotBinding
        {
            [SerializeField] private InventoryEquipmentSlot slot = InventoryEquipmentSlot.None;
            [SerializeField] private EquipmentSlotView view;

            public InventoryEquipmentSlot Slot => slot;
            public EquipmentSlotView View => view;
        }

        [SerializeField] private List<SlotBinding> slots = new List<SlotBinding>(4);

        public event Action<InventoryItemModel> ItemClicked;
        public event Action<InventoryItemModel> ItemHovered;
        public event Action ItemHoverExited;
        public event Action<InventoryEquipmentSlot, InventoryItemModel> InventoryItemDroppedOnSlot;

        private void Awake()
        {
            for (var i = 0; i < slots.Count; i++)
            {
                var binding = slots[i];
                if (binding == null || binding.View == null)
                    continue;

                binding.View.Clicked += HandleSlotClicked;
                binding.View.Hovered += HandleSlotHovered;
                binding.View.HoverExited += HandleSlotHoverExited;
                binding.View.InventoryItemDropped += HandleInventoryItemDropped;
            }
        }

        private void OnDestroy()
        {
            for (var i = 0; i < slots.Count; i++)
            {
                var binding = slots[i];
                if (binding == null || binding.View == null)
                    continue;

                binding.View.Clicked -= HandleSlotClicked;
                binding.View.Hovered -= HandleSlotHovered;
                binding.View.HoverExited -= HandleSlotHoverExited;
                binding.View.InventoryItemDropped -= HandleInventoryItemDropped;
            }
        }

        public void SetItems(IReadOnlyList<InventoryItemModel> equippedItems, InventoryItemPresentationCatalog catalog, long? selectedPlayerItemId, bool force = false)
        {
            for (var i = 0; i < slots.Count; i++)
            {
                var binding = slots[i];
                if (binding == null || binding.View == null)
                    continue;

                InventoryItemModel item;
                if (!TryFindEquippedItem(equippedItems, binding.Slot, out item))
                {
                    binding.View.Clear(force: true);
                    continue;
                }

                var presentation = catalog != null
                    ? catalog.Resolve(item)
                    : new InventoryItemPresentation(null, null, Color.white);
                binding.View.SetItem(item, presentation, force: force);
                binding.View.SetSelected(selectedPlayerItemId.HasValue && selectedPlayerItemId.Value == item.PlayerItemId, force: force);
            }
        }

        public void Clear(bool force = false)
        {
            for (var i = 0; i < slots.Count; i++)
            {
                var binding = slots[i];
                if (binding == null || binding.View == null)
                    continue;

                binding.View.Clear(force: force);
            }
        }

        private void HandleSlotClicked(EquipmentSlotView slotView)
        {
            var handler = ItemClicked;
            if (handler != null && slotView != null && slotView.HasItem)
                handler(slotView.Item);
        }

        private void HandleSlotHovered(EquipmentSlotView slotView)
        {
            var handler = ItemHovered;
            if (handler != null && slotView != null && slotView.HasItem)
                handler(slotView.Item);
        }

        private void HandleSlotHoverExited(EquipmentSlotView slotView)
        {
            var handler = ItemHoverExited;
            if (handler != null)
                handler();
        }

        private void HandleInventoryItemDropped(EquipmentSlotView slotView, InventoryItemModel item)
        {
            var handler = InventoryItemDroppedOnSlot;
            if (handler != null && slotView != null)
                handler(slotView.SlotType, item);
        }

        private static bool TryFindEquippedItem(IReadOnlyList<InventoryItemModel> items, InventoryEquipmentSlot slot, out InventoryItemModel item)
        {
            if (items != null)
            {
                for (var i = 0; i < items.Count; i++)
                {
                    if (!items[i].IsEquipped || items[i].EquippedSlot != (int)slot)
                        continue;

                    item = items[i];
                    return true;
                }
            }

            item = default;
            return false;
        }
    }
}
