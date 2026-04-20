using System;
using System.Collections.Generic;
using GameShared.Models;
using PhamNhanOnline.Client.UI.World;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.Inventory
{
    public sealed class CharacterEquipmentLoadoutView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform slotsRoot;
        [SerializeField] private EquipmentSlotView slotTemplate;
        [SerializeField] private InventoryItemPresentationCatalog itemPresentationCatalog;
        [SerializeField] private bool hideTemplateObject = true;

        [Header("Display Text")]
        [SerializeField] private string unequipOptionText = "Go trang bi";

        private readonly List<EquipmentSlotView> slotInstances = new();
        private IReadOnlyList<InventoryItemModel> items = Array.Empty<InventoryItemModel>();
        private int slotCount;
        private long? popupPlayerItemId;

        private void Awake()
        {
            if (slotsRoot == null)
                slotsRoot = transform;

            if (slotTemplate != null && hideTemplateObject)
                slotTemplate.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!popupPlayerItemId.HasValue)
                return;

            var modalUIManager = WorldModalUIManager.Instance;
            if (modalUIManager != null && modalUIManager.IsItemOptionsPopupVisible)
                return;

            popupPlayerItemId = null;
            ApplySelection(force: true);
        }

        private void OnDisable()
        {
            popupPlayerItemId = null;
            WorldModalUIManager.Instance?.HideItemOptionsPopup(force: true);
            ApplySelection(force: true);
        }

        private void OnDestroy()
        {
            for (var i = 0; i < slotInstances.Count; i++)
                UnbindSlot(slotInstances[i]);
        }

        public void SetItems(IReadOnlyList<InventoryItemModel> equippedItems, int equipmentSlotCount, bool force = false)
        {
            items = equippedItems ?? Array.Empty<InventoryItemModel>();
            slotCount = Math.Max(0, equipmentSlotCount);
            EnsureSlotCount(slotCount);
            ApplySelection(force);
        }

        public void Clear(bool force = false)
        {
            items = Array.Empty<InventoryItemModel>();
            slotCount = 0;
            popupPlayerItemId = null;
            WorldModalUIManager.Instance?.HideItemOptionsPopup(force: true);
            for (var i = 0; i < slotInstances.Count; i++)
            {
                var slotView = slotInstances[i];
                if (slotView == null)
                    continue;

                slotView.Clear(force);
                slotView.gameObject.SetActive(false);
            }
        }

        private void HandleItemClicked(InventoryItemModel item)
        {
            if (!item.IsEquipped)
                return;

            var modalUIManager = WorldModalUIManager.Instance;
            if (modalUIManager != null && modalUIManager.IsItemOptionsPopupVisible && popupPlayerItemId == item.PlayerItemId)
            {
                modalUIManager.HideItemOptionsPopup(force: true);
                popupPlayerItemId = null;
                ApplySelection(force: true);
                return;
            }

            popupPlayerItemId = item.PlayerItemId;
            ApplySelection(force: true);
            ShowItemOptions(item);
        }

        private void HandleInventoryItemDroppedOnSlot(int slotIndex, InventoryItemModel item)
        {
            popupPlayerItemId = null;
            WorldModalUIManager.Instance?.HideItemOptionsPopup(force: true);
            ApplySelection(force: true);
            _ = WorldCharacterEquipController.Instance.TryEquipItemAsync(item, slotIndex);
        }

        private void ShowItemOptions(InventoryItemModel item)
        {
            var modalUIManager = WorldModalUIManager.Instance;
            if (modalUIManager == null)
                return;

            var options = new List<ItemOptionEntry>(1)
            {
                new ItemOptionEntry(unequipOptionText, () => _ = UnequipItemAsync(item))
            };

            modalUIManager.ShowItemOptionsPopup(options, force: true);
        }

        private async System.Threading.Tasks.Task UnequipItemAsync(InventoryItemModel item)
        {
            popupPlayerItemId = null;
            WorldModalUIManager.Instance?.HideItemOptionsPopup(force: true);
            ApplySelection(force: true);
            await WorldCharacterEquipController.Instance.TryUnequipItemAsync(item);
        }

        private void ApplySelection(bool force)
        {
            EnsureSlotCount(slotCount);
            for (var i = 0; i < slotInstances.Count; i++)
            {
                var slotView = slotInstances[i];
                if (slotView == null)
                    continue;

                var shouldBeActive = i < slotCount;
                slotView.gameObject.SetActive(shouldBeActive);
                if (!shouldBeActive)
                    continue;

                var currentSlotIndex = i + 1;
                slotView.SetSlotIndex(currentSlotIndex);

                InventoryItemModel item;
                if (!TryFindEquippedItem(items, currentSlotIndex, out item))
                {
                    slotView.Clear(force: true);
                    continue;
                }

                var presentation = itemPresentationCatalog != null
                    ? itemPresentationCatalog.Resolve(item)
                    : new InventoryItemPresentation(null, null, Color.white);
                slotView.SetItem(item, presentation, force: force);
                slotView.SetSelected(popupPlayerItemId.HasValue && popupPlayerItemId.Value == item.PlayerItemId, force: force);
            }
        }

        private void EnsureSlotCount(int targetSlotCount)
        {
            var desiredCount = Math.Max(0, targetSlotCount);
            if (slotTemplate == null)
                return;

            while (slotInstances.Count < desiredCount)
            {
                var instance = Instantiate(slotTemplate, slotsRoot != null ? slotsRoot : transform);
                instance.gameObject.SetActive(true);
                BindSlot(instance);
                slotInstances.Add(instance);
            }
        }

        private void BindSlot(EquipmentSlotView slotView)
        {
            if (slotView == null)
                return;

            slotView.Clicked += HandleSlotClicked;
            slotView.InventoryItemDropped += HandleSlotDropped;
        }

        private void UnbindSlot(EquipmentSlotView slotView)
        {
            if (slotView == null)
                return;

            slotView.Clicked -= HandleSlotClicked;
            slotView.InventoryItemDropped -= HandleSlotDropped;
        }

        private void HandleSlotClicked(EquipmentSlotView slotView)
        {
            if (slotView != null && slotView.HasItem)
                HandleItemClicked(slotView.Item);
        }

        private void HandleSlotDropped(EquipmentSlotView slotView, InventoryItemModel item)
        {
            if (slotView != null)
                HandleInventoryItemDroppedOnSlot(slotView.SlotIndex, item);
        }

        private static bool TryFindEquippedItem(IReadOnlyList<InventoryItemModel> equippedItems, int currentSlotIndex, out InventoryItemModel item)
        {
            if (equippedItems != null)
            {
                for (var i = 0; i < equippedItems.Count; i++)
                {
                    if (!equippedItems[i].IsEquipped || equippedItems[i].EquippedSlot != currentSlotIndex)
                        continue;

                    item = equippedItems[i];
                    return true;
                }
            }

            item = default;
            return false;
        }
    }
}
