using System;
using System.Collections.Generic;
using System.Globalization;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Features.MartialArts.Application;
using PhamNhanOnline.Client.UI.Inventory;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.World
{
    public sealed partial class WorldInventoryPanelController
    {
        private enum QuantityPopupAction
        {
            None = 0,
            Drop = 1,
            Use = 2
        }

        private void HandleInventoryItemClicked(InventoryItemModel item)
        {
            if (inventoryActionInFlight)
                return;

            previewPlayerItemId = item.PlayerItemId;
            var modalUiManager = WorldModalUIManager.Instance;

            if (modalUiManager != null && modalUiManager.IsInventoryItemOptionsPopupVisible && popupPlayerItemId == item.PlayerItemId)
            {
                HideItemOptionsPopup();
                ApplyPreviewSelectionState(force: true);
                return;
            }

            ShowItemOptions(item);
            ApplyPreviewSelectionState(force: true);
        }

        private async void HandleInventoryItemDroppedOnEquipmentSlot(InventoryEquipmentSlot slot, InventoryItemModel item)
        {
            if (inventoryActionInFlight || !ClientRuntime.IsInitialized)
                return;

            if (item.EquipmentSlotType != (int)slot)
                return;

            inventoryActionInFlight = true;
            ApplyInventoryStatus(inventoryActionInProgressText, force: true);

            try
            {
                var result = await ClientRuntime.InventoryService.EquipItemAsync(item.PlayerItemId, (int)slot);
                if (!result.Success)
                    ClientLog.Warn($"WorldInventoryPanelController failed to equip item: {result.Message}");

                previewPlayerItemId = null;
            }
            catch (Exception ex)
            {
                ClientLog.Warn($"WorldInventoryPanelController equip exception: {ex.Message}");
            }
            finally
            {
                inventoryActionInFlight = false;
                HideItemOptionsPopup(force: true);
                RefreshFromRuntime(force: true);
                RefreshInventory(force: true);
            }
        }

        private async void HandleEquippedItemDroppedOnInventory(InventoryEquipmentSlot slot)
        {
            if (inventoryActionInFlight || !ClientRuntime.IsInitialized)
                return;

            inventoryActionInFlight = true;
            ApplyInventoryStatus(inventoryActionInProgressText, force: true);

            try
            {
                var result = await ClientRuntime.InventoryService.UnequipItemAsync((int)slot);
                if (!result.Success)
                    ClientLog.Warn($"WorldInventoryPanelController failed to unequip item: {result.Message}");

                previewPlayerItemId = null;
            }
            catch (Exception ex)
            {
                ClientLog.Warn($"WorldInventoryPanelController unequip exception: {ex.Message}");
            }
            finally
            {
                inventoryActionInFlight = false;
                HideItemOptionsPopup(force: true);
                RefreshFromRuntime(force: true);
                RefreshInventory(force: true);
            }
        }

        private void ShowItemOptions(InventoryItemModel item)
        {
            var modalUiManager = WorldModalUIManager.Instance;
            if (modalUiManager == null)
                return;

            var options = BuildItemOptions(item);
            if (options.Count == 0)
            {
                HideItemOptionsPopup(force: true);
                return;
            }

            popupPlayerItemId = item.PlayerItemId;
            previewPlayerItemId = item.PlayerItemId;
            modalUiManager.SetItemTooltipSuppressed(this, suppressed: true, force: true);
            modalUiManager.HideItemTooltip(force: true);
            modalUiManager.ShowInventoryItemOptionsPopup(
                inventoryPanelBounds != null ? inventoryPanelBounds : transform as RectTransform,
                item.Name,
                options,
                force: true);
        }

        private List<InventoryItemOptionsPopupController.OptionEntry> BuildItemOptions(InventoryItemModel item)
        {
            if (item.IsEquipped && item.ItemType == (int)InventoryItemType.Equipment)
            {
                return new List<InventoryItemOptionsPopupController.OptionEntry>(1)
                {
                    new InventoryItemOptionsPopupController.OptionEntry(unequipOptionText, () => _ = UnequipItemAsync(item))
                };
            }

            var options = new List<InventoryItemOptionsPopupController.OptionEntry>(2);
            var useOption = BuildUseOption(item);
            if (useOption.HasValue)
                options.Add(useOption.Value);

            if (item.IsDroppable)
                options.Add(new InventoryItemOptionsPopupController.OptionEntry(dropOptionText, () => HandleDropItemClicked(item)));

            return options;
        }

        private InventoryItemOptionsPopupController.OptionEntry? BuildUseOption(InventoryItemModel item)
        {
            string blockedReason;
            if (!CanUseItem(item, out blockedReason))
                return null;

            return new InventoryItemOptionsPopupController.OptionEntry(
                useOptionText,
                () => _ = UseItemAsync(item));
        }

        private bool CanUseItem(InventoryItemModel item, out string blockedReason)
        {
            if (item.IsEquipped && item.ItemType == (int)InventoryItemType.Equipment)
            {
                blockedReason = "dang mac";
                return false;
            }

            if (item.ItemType == (int)InventoryItemType.MartialArtBook &&
                item.MartialArtBookMartialArtId.HasValue &&
                HasLearnedMartialArt(item.MartialArtBookMartialArtId.Value))
            {
                blockedReason = "da hoc";
                return false;
            }

            switch ((InventoryItemType)item.ItemType)
            {
                case InventoryItemType.Equipment:
                case InventoryItemType.MartialArtBook:
                case InventoryItemType.Consumable:
                case InventoryItemType.PillRecipeBook:
                    blockedReason = string.Empty;
                    return true;
                default:
                    blockedReason = "chua ho tro";
                    return false;
            }
        }

        private bool TryResolveUseQuantityRequirement(InventoryItemModel item, out int suggestedQuantity)
        {
            if ((InventoryItemType)item.ItemType == InventoryItemType.Consumable && item.Quantity > 1)
            {
                suggestedQuantity = Mathf.Max(1, item.Quantity);
                return true;
            }

            suggestedQuantity = 1;
            return false;
        }

        private static string ResolveUseSuccessText(InventoryItemModel item, int quantity)
        {
            switch ((InventoryItemType)item.ItemType)
            {
                case InventoryItemType.Equipment:
                    return "Da trang bi vat pham.";
                case InventoryItemType.MartialArtBook:
                    return "Da su dung sach cong phap.";
                case InventoryItemType.PillRecipeBook:
                    return "Da su dung sach cong thuc.";
                case InventoryItemType.Consumable:
                    return quantity > 1
                        ? string.Format(CultureInfo.InvariantCulture, "Da su dung {0} vat pham.", quantity)
                        : "Da su dung vat pham.";
                default:
                    return "Da su dung vat pham.";
            }
        }

        private static string ResolveQuantityPopupTitle(QuantityPopupAction action)
        {
            switch (action)
            {
                case QuantityPopupAction.Use:
                    return "So luong su dung";
                default:
                    return null;
            }
        }

        private static bool IsUseActionSupported(InventoryItemModel item)
        {
            switch ((InventoryItemType)item.ItemType)
            {
                case InventoryItemType.Equipment:
                case InventoryItemType.MartialArtBook:
                case InventoryItemType.Consumable:
                case InventoryItemType.PillRecipeBook:
                    return true;
                default:
                    return false;
            }
        }

        private async System.Threading.Tasks.Task ExecuteUseItemAsync(InventoryItemModel item, int quantity)
        {
            if (inventoryActionInFlight || !ClientRuntime.IsInitialized)
                return;

            inventoryActionInFlight = true;
            HideItemOptionsPopup(force: true);
            HideQuantityPopup(force: true);
            ApplyInventoryStatus(inventoryUseActionText, force: true);

            try
            {
                var result = await ClientRuntime.InventoryService.UseItemAsync(item.PlayerItemId, quantity);
                if (!result.Success)
                {
                    ClientLog.Warn($"WorldInventoryPanelController failed to use item: {result.Message}");
                    ApplyInventoryStatus(result.Message, force: true);
                    return;
                }

                previewPlayerItemId = null;
                ApplyInventoryStatus(ResolveUseSuccessText(item, quantity), force: true);
            }
            catch (Exception ex)
            {
                ClientLog.Warn($"WorldInventoryPanelController use item execution exception: {ex.Message}");
                ApplyInventoryStatus(ex.Message, force: true);
            }
            finally
            {
                inventoryActionInFlight = false;
                RefreshFromRuntime(force: true);
                RefreshInventory(force: true);
            }
        }

        private void ShowQuantityPopup(InventoryItemModel item, QuantityPopupAction action)
        {
            var modalUiManager = WorldModalUIManager.Instance;
            if (modalUiManager == null)
            {
                if (action == QuantityPopupAction.Drop)
                    _ = DropItemAsync(item.PlayerItemId, 1);
                else if (action == QuantityPopupAction.Use)
                    _ = ExecuteUseItemAsync(item, 1);

                return;
            }

            quantityPopupPlayerItemId = item.PlayerItemId;
            quantityPopupAction = action;
            modalUiManager.ShowQuantityPopup(
                Mathf.Max(1, item.Quantity),
                HandleQuantityConfirmed,
                HandleQuantityCancelled,
                ResolveQuantityPopupTitle(action));
        }

        private void HideQuantityPopup(bool force = false)
        {
            quantityPopupPlayerItemId = null;
            quantityPopupAction = QuantityPopupAction.None;
            WorldModalUIManager.Instance?.HideQuantityPopup(force);
        }

        private void UpdateQuantityPopupVisibility()
        {
            var modalUiManager = WorldModalUIManager.Instance;
            if (modalUiManager == null || !modalUiManager.IsQuantityPopupVisible)
                return;

            if (!quantityPopupPlayerItemId.HasValue ||
                !ClientRuntime.IsInitialized ||
                !ClientRuntime.Inventory.TryGetItem(quantityPopupPlayerItemId.Value, out _))
            {
                HideQuantityPopup(force: true);
            }
        }

        private void HandleQuantityConfirmed(int quantity)
        {
            InventoryItemModel item;
            if (!quantityPopupPlayerItemId.HasValue ||
                !ClientRuntime.IsInitialized ||
                !ClientRuntime.Inventory.TryGetItem(quantityPopupPlayerItemId.Value, out item))
            {
                HideQuantityPopup(force: true);
                return;
            }

            var action = quantityPopupAction;
            HideQuantityPopup(force: true);

            var resolvedQuantity = Mathf.Clamp(quantity, 1, Mathf.Max(1, item.Quantity));
            switch (action)
            {
                case QuantityPopupAction.Drop:
                    _ = DropItemAsync(item.PlayerItemId, resolvedQuantity);
                    break;
                case QuantityPopupAction.Use:
                    _ = ExecuteUseItemAsync(item, resolvedQuantity);
                    break;
            }
        }

        private void HandleQuantityCancelled()
        {
            HideQuantityPopup(force: true);
        }

        private bool HasLearnedMartialArt(int martialArtId)
        {
            var ownedMartialArts = ClientRuntime.IsInitialized
                ? ClientRuntime.MartialArts.OwnedMartialArts
                : Array.Empty<PlayerMartialArtModel>();

            for (var i = 0; i < ownedMartialArts.Length; i++)
            {
                if (ownedMartialArts[i].MartialArtId == martialArtId)
                    return true;
            }

            return false;
        }

        private async System.Threading.Tasks.Task UseItemAsync(InventoryItemModel item)
        {
            if (inventoryActionInFlight || !ClientRuntime.IsInitialized)
                return;

            HideItemOptionsPopup(force: true);

            try
            {
                switch ((InventoryItemType)item.ItemType)
                {
                    case InventoryItemType.Equipment:
                        await UseEquipmentItemAsync(item);
                        break;
                    case InventoryItemType.MartialArtBook:
                        await UseMartialArtBookItemAsync(item);
                        break;
                    case InventoryItemType.Consumable:
                        await UseConsumableItemAsync(item);
                        break;
                    case InventoryItemType.Talisman:
                        await UseTalismanItemAsync(item);
                        break;
                    case InventoryItemType.PillRecipeBook:
                        await UseBookItemAsync(item);
                        break;
                    default:
                        ApplyInventoryStatus(inventoryUnsupportedUseText, force: true);
                        break;
                }
            }
            catch (Exception ex)
            {
                ClientLog.Warn($"WorldInventoryPanelController use item exception: {ex.Message}");
                ApplyInventoryStatus(ex.Message, force: true);
            }
        }

        private async System.Threading.Tasks.Task UnequipItemAsync(InventoryItemModel item)
        {
            if (inventoryActionInFlight || !ClientRuntime.IsInitialized)
                return;

            if (!item.IsEquipped || !item.EquippedSlot.HasValue)
            {
                ApplyInventoryStatus(inventoryUnsupportedUseText, force: true);
                return;
            }

            inventoryActionInFlight = true;
            HideItemOptionsPopup(force: true);
            ApplyInventoryStatus(inventoryUnequipActionText, force: true);

            try
            {
                var result = await ClientRuntime.InventoryService.UnequipItemAsync(item.EquippedSlot.Value);
                if (!result.Success)
                {
                    ClientLog.Warn($"WorldInventoryPanelController failed to unequip item from popup: {result.Message}");
                    ApplyInventoryStatus(result.Message, force: true);
                    return;
                }

                previewPlayerItemId = null;
                ApplyInventoryStatus("Da go trang bi.", force: true);
            }
            catch (Exception ex)
            {
                ClientLog.Warn($"WorldInventoryPanelController unequip from popup exception: {ex.Message}");
                ApplyInventoryStatus(ex.Message, force: true);
            }
            finally
            {
                inventoryActionInFlight = false;
                RefreshFromRuntime(force: true);
                RefreshInventory(force: true);
            }
        }

        private async System.Threading.Tasks.Task UseEquipmentItemAsync(InventoryItemModel item)
        {
            if (!item.EquipmentSlotType.HasValue)
            {
                ApplyInventoryStatus(inventoryUnsupportedUseText, force: true);
                return;
            }

            if (item.IsEquipped)
            {
                ApplyInventoryStatus(inventoryAlreadyEquippedText, force: true);
                return;
            }

            await ExecuteUseItemAsync(item, 1);
        }

        private async System.Threading.Tasks.Task UseMartialArtBookItemAsync(InventoryItemModel item)
        {
            if (item.MartialArtBookMartialArtId.HasValue && HasLearnedMartialArt(item.MartialArtBookMartialArtId.Value))
            {
                ApplyInventoryStatus(inventoryMartialArtAlreadyLearnedText, force: true);
                return;
            }

            await ExecuteUseItemAsync(item, 1);
        }

        private System.Threading.Tasks.Task UseConsumableItemAsync(InventoryItemModel item)
        {
            int suggestedQuantity;
            if (TryResolveUseQuantityRequirement(item, out suggestedQuantity))
            {
                ShowQuantityPopup(item, QuantityPopupAction.Use);
                return System.Threading.Tasks.Task.CompletedTask;
            }

            return ExecuteUseItemAsync(item, suggestedQuantity);
        }

        private System.Threading.Tasks.Task UseTalismanItemAsync(InventoryItemModel item)
        {
            if (!IsUseActionSupported(item))
            {
                ApplyInventoryStatus(inventoryUnsupportedUseText, force: true);
                return System.Threading.Tasks.Task.CompletedTask;
            }

            return ExecuteUseItemAsync(item, 1);
        }

        private System.Threading.Tasks.Task UseBookItemAsync(InventoryItemModel item)
        {
            if (!IsUseActionSupported(item))
            {
                ApplyInventoryStatus(inventoryUnsupportedUseText, force: true);
                return System.Threading.Tasks.Task.CompletedTask;
            }

            return ExecuteUseItemAsync(item, 1);
        }

        private void HandleDropItemClicked(InventoryItemModel item)
        {
            HideItemOptionsPopup(force: true);
            if (!item.IsDroppable)
            {
                ApplyInventoryStatus(inventoryDropUnavailableText, force: true);
                return;
            }

            if (item.Quantity > 1)
            {
                ShowQuantityPopup(item, QuantityPopupAction.Drop);
                return;
            }

            _ = DropItemAsync(item.PlayerItemId, 1);
        }

        private void HideItemOptionsPopup(bool force = false)
        {
            popupPlayerItemId = null;
            previewPlayerItemId = null;
            WorldModalUIManager.Instance?.HideInventoryItemOptionsPopup(force);
            var modalUiManager = WorldModalUIManager.Instance;
            if (modalUiManager != null)
            {
                modalUiManager.SetItemTooltipSuppressed(this, suppressed: false, force: true);
                modalUiManager.HideItemTooltip(force: true);
            }

            ApplyPreviewSelectionState(force: true);
        }

        private async System.Threading.Tasks.Task DropItemAsync(long playerItemId, int quantity)
        {
            if (inventoryActionInFlight || !ClientRuntime.IsInitialized)
                return;

            InventoryItemModel item;
            if (!ClientRuntime.Inventory.TryGetItem(playerItemId, out item) || !item.IsDroppable)
            {
                ApplyInventoryStatus(inventoryDropUnavailableText, force: true);
                return;
            }

            inventoryActionInFlight = true;
            HideItemOptionsPopup(force: true);
            HideQuantityPopup(force: true);
            ApplyInventoryStatus(inventoryDropActionText, force: true);

            try
            {
                var result = await ClientRuntime.InventoryService.DropItemAsync(playerItemId, quantity);
                if (!result.Success)
                {
                    ClientLog.Warn($"WorldInventoryPanelController failed to drop item: {result.Message}");
                    ApplyInventoryStatus(result.Message, force: true);
                    return;
                }

                previewPlayerItemId = null;
                ApplyInventoryStatus(inventoryDropSuccessText, force: true);
            }
            catch (Exception ex)
            {
                ClientLog.Warn($"WorldInventoryPanelController drop exception: {ex.Message}");
                ApplyInventoryStatus(ex.Message, force: true);
            }
            finally
            {
                inventoryActionInFlight = false;
                RefreshFromRuntime(force: true);
                RefreshInventory(force: true);
            }
        }
    }
}
