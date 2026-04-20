using System;
using System.Threading.Tasks;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.UI.Inventory;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.World
{
    public sealed class WorldCharacterEquipController : MonoBehaviour
    {
        private static WorldCharacterEquipController instance;
        private bool actionInFlight;

        public static WorldCharacterEquipController Instance
        {
            get
            {
                if (instance != null)
                    return instance;

                instance = FindFirstObjectByType<WorldCharacterEquipController>();
                if (instance != null)
                    return instance;

                var go = new GameObject(nameof(WorldCharacterEquipController));
                instance = go.AddComponent<WorldCharacterEquipController>();
                return instance;
            }
        }

        public bool IsBusy => actionInFlight;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        public async Task<bool> TryEquipItemAsync(InventoryItemModel item, int slotIndex)
        {
            if (!CanEquipItemToSlot(item, slotIndex, out _))
                return false;

            if (!ClientRuntime.IsInitialized || actionInFlight)
                return false;

            actionInFlight = true;
            WorldModalUIManager.Instance?.HideAllViews(force: true);

            try
            {
                var result = await ClientRuntime.InventoryService.EquipItemAsync(item.PlayerItemId, slotIndex);
                if (!result.Success)
                {
                    ClientLog.Warn($"WorldCharacterEquipController equip failed: {result.Message}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                ClientLog.Warn($"WorldCharacterEquipController equip exception: {ex.Message}");
                return false;
            }
            finally
            {
                actionInFlight = false;
            }
        }

        public async Task<bool> TryUnequipItemAsync(InventoryItemModel item)
        {
            if (!item.IsEquipped || !item.EquippedSlot.HasValue)
                return false;

            return await TryUnequipSlotAsync(item.EquippedSlot.Value);
        }

        public async Task<bool> TryUnequipSlotAsync(int slotIndex)
        {
            if (slotIndex <= 0 || !ClientRuntime.IsInitialized || actionInFlight)
                return false;

            actionInFlight = true;
            WorldModalUIManager.Instance?.HideAllViews(force: true);

            try
            {
                var result = await ClientRuntime.InventoryService.UnequipItemAsync(slotIndex);
                if (!result.Success)
                {
                    ClientLog.Warn($"WorldCharacterEquipController unequip failed: {result.Message}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                ClientLog.Warn($"WorldCharacterEquipController unequip exception: {ex.Message}");
                return false;
            }
            finally
            {
                actionInFlight = false;
            }
        }

        public bool CanEquipItemToSlot(InventoryItemModel item, int slotIndex, out string blockedReason)
        {
            if (item.ItemType != (int)InventoryItemType.Equipment)
            {
                blockedReason = "Khong phai trang bi.";
                return false;
            }

            if (slotIndex <= 0)
            {
                blockedReason = "O trang bi khong hop le.";
                return false;
            }

            if (ClientRuntime.IsInitialized &&
                ClientRuntime.Inventory.HasLoadedInventory &&
                ClientRuntime.Inventory.EquipmentSlotCount > 0 &&
                slotIndex > ClientRuntime.Inventory.EquipmentSlotCount)
            {
                blockedReason = "O trang bi vuot qua so o hien co.";
                return false;
            }

            blockedReason = string.Empty;
            return true;
        }
    }
}
