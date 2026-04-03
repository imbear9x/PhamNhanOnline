using System;
using System.Collections.Generic;
using System.Globalization;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Features.MartialArts.Application;
using PhamNhanOnline.Client.UI.Inventory;
using PhamNhanOnline.Client.UI.Potential;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.World
{
    public sealed partial class WorldInventoryPanelController
    {
        private void HandleInventoryItemClicked(InventoryItemModel item)
        {
            if (inventoryActionInFlight)
                return;

            previewPlayerItemId = item.PlayerItemId;

            if (itemOptionsPopupView != null && itemOptionsPopupView.IsVisible && popupPlayerItemId == item.PlayerItemId)
            {
                HideItemOptionsPopup();
                RefreshInventory(force: true);
                return;
            }

            ShowItemOptions(item);
            RefreshInventory(force: true);
        }

        private void HandleInventoryItemHovered(InventoryItemModel item)
        {
            if (popupPlayerItemId.HasValue)
                return;

            suppressTooltipUntilHoverReset = false;
            previewPlayerItemId = item.PlayerItemId;
            RefreshInventory(force: true);
        }

        private void HandleInventoryItemHoverExited()
        {
            if (popupPlayerItemId.HasValue)
                return;

            suppressTooltipUntilHoverReset = false;
            previewPlayerItemId = null;
            RefreshInventory(force: true);
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
            if (itemOptionsPopupView == null)
                return;

            var options = BuildItemOptions(item);
            if (options.Count == 0)
            {
                HideItemOptionsPopup(force: true);
                return;
            }

            popupPlayerItemId = item.PlayerItemId;
            previewPlayerItemId = item.PlayerItemId;
            suppressTooltipUntilHoverReset = true;
            if (itemTooltipView != null)
                itemTooltipView.Hide(force: true);
            itemOptionsPopupView.Show(
                inventoryPanelBounds != null ? inventoryPanelBounds : transform as RectTransform,
                item.Name,
                options,
                force: true);
        }

        private List<PotentialUpgradeOptionsPopupView.OptionEntry> BuildItemOptions(InventoryItemModel item)
        {
            if (item.IsEquipped && item.ItemType == (int)InventoryItemType.Equipment)
            {
                return new List<PotentialUpgradeOptionsPopupView.OptionEntry>(1)
                {
                    new PotentialUpgradeOptionsPopupView.OptionEntry("Go trang bi", () => _ = UnequipItemAsync(item))
                };
            }

            var options = new List<PotentialUpgradeOptionsPopupView.OptionEntry>(2)
            {
                BuildUseOption(item)
            };
            if (item.IsDroppable)
                options.Add(new PotentialUpgradeOptionsPopupView.OptionEntry("Vut ra", () => HandleDropItemClicked(item)));

            return options;
        }

        private PotentialUpgradeOptionsPopupView.OptionEntry BuildUseOption(InventoryItemModel item)
        {
            string blockedReason;
            var canUse = CanUseItem(item, out blockedReason);
            var label = canUse
                ? "Su dung"
                : string.Format(CultureInfo.InvariantCulture, "Su dung ({0})", blockedReason);

            return new PotentialUpgradeOptionsPopupView.OptionEntry(
                label,
                () => _ = UseItemAsync(item),
                canUse);
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

            blockedReason = string.Empty;
            return true;
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

            inventoryActionInFlight = true;
            HideItemOptionsPopup(force: true);
            ApplyInventoryStatus(inventoryActionInProgressText, force: true);

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
            finally
            {
                inventoryActionInFlight = false;
                RefreshFromRuntime(force: true);
                RefreshInventory(force: true);
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

            var result = await ClientRuntime.InventoryService.EquipItemAsync(item.PlayerItemId, item.EquipmentSlotType.Value);
            if (!result.Success)
            {
                ClientLog.Warn($"WorldInventoryPanelController failed to equip item from popup: {result.Message}");
                ApplyInventoryStatus(result.Message, force: true);
                return;
            }

            previewPlayerItemId = null;
            ApplyInventoryStatus("Da trang bi vat pham.", force: true);
        }

        private async System.Threading.Tasks.Task UseMartialArtBookItemAsync(InventoryItemModel item)
        {
            if (item.MartialArtBookMartialArtId.HasValue && HasLearnedMartialArt(item.MartialArtBookMartialArtId.Value))
            {
                ApplyInventoryStatus(inventoryMartialArtAlreadyLearnedText, force: true);
                return;
            }

            var result = await ClientRuntime.InventoryService.UseMartialArtBookAsync(item.PlayerItemId);
            if (!result.Success)
            {
                ClientLog.Warn($"WorldInventoryPanelController failed to use martial art book: {result.Message}");
                ApplyInventoryStatus(result.Message, force: true);
                return;
            }

            previewPlayerItemId = null;
            ApplyInventoryStatus("Da su dung sach cong phap.", force: true);
        }

        private System.Threading.Tasks.Task UseConsumableItemAsync(InventoryItemModel item)
        {
            ApplyInventoryStatus(inventoryUnsupportedUseText, force: true);
            return System.Threading.Tasks.Task.CompletedTask;
        }

        private System.Threading.Tasks.Task UseTalismanItemAsync(InventoryItemModel item)
        {
            ApplyInventoryStatus(inventoryUnsupportedUseText, force: true);
            return System.Threading.Tasks.Task.CompletedTask;
        }

        private System.Threading.Tasks.Task UseBookItemAsync(InventoryItemModel item)
        {
            ApplyInventoryStatus(inventoryUnsupportedUseText, force: true);
            return System.Threading.Tasks.Task.CompletedTask;
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
                ShowDropQuantityPopup(item);
                return;
            }

            _ = DropItemAsync(item.PlayerItemId, 1);
        }

        private void HideItemOptionsPopup(bool force = false)
        {
            popupPlayerItemId = null;
            suppressTooltipUntilHoverReset = true;
            if (itemOptionsPopupView != null)
                itemOptionsPopupView.Hide(force);
            if (itemTooltipView != null)
                itemTooltipView.Hide(force: true);
        }

        private void ShowDropQuantityPopup(InventoryItemModel item)
        {
            if (dropQuantityPopupView == null)
            {
                _ = DropItemAsync(item.PlayerItemId, 1);
                return;
            }

            dropQuantityPopupPlayerItemId = item.PlayerItemId;
            dropQuantityPopupView.Show(
                item.Name,
                Mathf.Max(1, item.Quantity),
                HandleDropQuantityConfirmed,
                HandleDropQuantityCancelled);
        }

        private void HideDropQuantityPopup(bool force = false)
        {
            dropQuantityPopupPlayerItemId = null;
            if (dropQuantityPopupView != null)
                dropQuantityPopupView.Hide(force);
        }

        private void UpdateDropQuantityPopupVisibility()
        {
            if (dropQuantityPopupView == null || !dropQuantityPopupView.IsVisible)
                return;

            if (!dropQuantityPopupPlayerItemId.HasValue ||
                !ClientRuntime.IsInitialized ||
                !ClientRuntime.Inventory.TryGetItem(dropQuantityPopupPlayerItemId.Value, out _))
            {
                HideDropQuantityPopup(force: true);
            }
        }

        private void HandleDropQuantityConfirmed(int quantity)
        {
            InventoryItemModel item;
            if (!dropQuantityPopupPlayerItemId.HasValue ||
                !ClientRuntime.IsInitialized ||
                !ClientRuntime.Inventory.TryGetItem(dropQuantityPopupPlayerItemId.Value, out item))
            {
                HideDropQuantityPopup(force: true);
                return;
            }

            HideDropQuantityPopup(force: true);
            _ = DropItemAsync(item.PlayerItemId, Mathf.Clamp(quantity, 1, Mathf.Max(1, item.Quantity)));
        }

        private void HandleDropQuantityCancelled()
        {
            HideDropQuantityPopup(force: true);
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
            HideDropQuantityPopup(force: true);
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
