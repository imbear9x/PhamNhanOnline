using System;
using System.Linq;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Features.Inventory.Application;
using PhamNhanOnline.Client.UI.Inventory;
using PhamNhanOnline.Client.UI.Potential;
using TMPro;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.World
{
    public sealed partial class WorldInventoryPanelController : MonoBehaviour
    {
        private const string InventoryNotLoadedText = "Kho do chua duoc tai.";
        private const string EmptyInventoryText = "Kho do dang trong.";
        private const string InventoryActionInProgressText = "Dang cap nhat trang bi...";
        private const string InventoryUseActionText = "Dang su dung vat pham...";
        private const string InventoryDropActionText = "Dang vut vat pham...";
        private const string InventoryDropSuccessText = "Da vut vat pham.";
        private const string InventoryDropUnavailableText = "Vat pham nay khong the vut.";
        private const string InventoryUnsupportedUseText = "Vat pham nay chua co logic su dung o phase nay.";
        private const string InventoryMartialArtAlreadyLearnedText = "Cong phap nay da hoc roi, khong the dung them sach.";
        private const string InventoryAlreadyEquippedText = "Trang bi nay dang duoc mac.";
        private const string InventoryUnequipActionText = "Dang go trang bi...";

        [Header("Inventory References")]
        [SerializeField] private RectTransform inventoryPanelBounds;
        [SerializeField] private TMP_Text inventoryStatusText;
        [SerializeField] private InventoryItemGridView inventoryGridView;
        [SerializeField] private EquipmentSlotsPanelView equipmentSlotsView;
        [SerializeField] private InventoryItemPresentationCatalog itemPresentationCatalog;

        [Header("Inventory Option Labels")]
        [SerializeField] private string useOptionText = "Su dung";
        [SerializeField] private string unequipOptionText = "Go trang bi";
        [SerializeField] private string dropOptionText = "Vut ra";

        private string lastInventorySnapshot = string.Empty;
        private string lastInventoryStatus = string.Empty;
        private long? previewPlayerItemId;
        private long? popupPlayerItemId;
        private bool inventoryActionInFlight;
        private long? quantityPopupPlayerItemId;
        private QuantityPopupAction quantityPopupAction;

        private void Awake()
        {
            if (inventoryGridView != null)
                inventoryGridView.ItemClicked += HandleInventoryItemClicked;

            if (equipmentSlotsView != null)
            {
                equipmentSlotsView.ItemClicked += HandleInventoryItemClicked;
                equipmentSlotsView.InventoryItemDroppedOnSlot += HandleInventoryItemDroppedOnEquipmentSlot;
            }

            if (inventoryPanelBounds == null)
                inventoryPanelBounds = transform as RectTransform;
        }

        private void OnEnable()
        {
            RefreshInventory(force: true);
        }

        private void Update()
        {
            if (!isActiveAndEnabled)
                return;

            RefreshInventory(force: false);
            // Deliberately disabled for now.
            // We only want to close the quantity popup from explicit action flows,
            // not from a polling check that might mis-detect state and hide it early.
            // Re-enable UpdateQuantityPopupVisibility() here if a real runtime stale-popup bug appears.
            // UpdateQuantityPopupVisibility();
        }

        private void OnDisable()
        {
            HideItemOptionsPopup(force: true);
            HideQuantityPopup(force: true);
        }

        private void OnDestroy()
        {
            if (inventoryGridView != null)
                inventoryGridView.ItemClicked -= HandleInventoryItemClicked;

            if (equipmentSlotsView != null)
            {
                equipmentSlotsView.ItemClicked -= HandleInventoryItemClicked;
                equipmentSlotsView.InventoryItemDroppedOnSlot -= HandleInventoryItemDroppedOnEquipmentSlot;
            }

        }

        private void RefreshInventory(bool force)
        {
            if (!ClientRuntime.IsInitialized)
            {
                ApplyInventoryStatus(InventoryNotLoadedText, force);
                ClearInventoryVisuals(force);
                return;
            }

            var inventoryState = ClientRuntime.Inventory;
            var allItems = SortInventoryItems(inventoryState.Items);
            var equippedItems = allItems.Where(x => x.IsEquipped).OrderBy(x => x.EquippedSlot ?? int.MaxValue).ThenBy(x => x.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase).ToList();
            var bagItems = allItems.Where(x => !x.IsEquipped).ToList();
            var status = ResolveInventoryStatus(inventoryState, bagItems.Count, equippedItems.Count);
            var snapshot = BuildInventorySnapshot(inventoryState, allItems, inventoryActionInFlight);
            var modalUIManager = WorldModalUIManager.Instance;
            if (popupPlayerItemId.HasValue &&
                (modalUIManager == null || !modalUIManager.IsItemOptionsPopupVisible))
            {
                popupPlayerItemId = null;
                previewPlayerItemId = null;
                force = true;
            }

            if (!force &&
                string.Equals(lastInventorySnapshot, snapshot, StringComparison.Ordinal) &&
                string.Equals(lastInventoryStatus, status, StringComparison.Ordinal))
            {
                return;
            }

            lastInventorySnapshot = snapshot;
            ApplyInventoryStatus(status, force: true);

            if (!inventoryState.HasLoadedInventory)
            {
                HideItemOptionsPopup(force: true);
                ClearInventoryVisuals(force: true);
                return;
            }

            if (inventoryGridView != null)
            {
                inventoryGridView.SetItems(bagItems, itemPresentationCatalog, force: true);
                inventoryGridView.SetSelectedItem(previewPlayerItemId, force: true);
            }

            if (equipmentSlotsView != null)
                equipmentSlotsView.SetItems(equippedItems, itemPresentationCatalog, previewPlayerItemId, force: true);

            if (popupPlayerItemId.HasValue && !TryFindInventoryItemById(allItems, popupPlayerItemId, out _))
                HideItemOptionsPopup(force: true);

        }

        private void ApplyPreviewSelectionState(bool force)
        {
            if (inventoryGridView != null)
                inventoryGridView.SetSelectedItem(previewPlayerItemId, force);

            if (equipmentSlotsView == null || !ClientRuntime.IsInitialized)
                return;

            var equippedItems = SortInventoryItems(ClientRuntime.Inventory.Items)
                .Where(x => x.IsEquipped)
                .OrderBy(x => x.EquippedSlot ?? int.MaxValue)
                .ThenBy(x => x.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList();
            equipmentSlotsView.SetItems(equippedItems, itemPresentationCatalog, previewPlayerItemId, force);
        }

    }
}
