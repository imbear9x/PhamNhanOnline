using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Features.Inventory.Application;
using PhamNhanOnline.Client.Features.MartialArts.Application;
using PhamNhanOnline.Client.Network.Session;
using PhamNhanOnline.Client.UI.Common;
using PhamNhanOnline.Client.UI.Inventory;
using PhamNhanOnline.Client.UI.Potential;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PhamNhanOnline.Client.UI.World
{
    public sealed class WorldInventoryPanelController : MonoBehaviour
    {
        [Header("Character References")]
        [SerializeField] private TMP_Text characterNameText;
        [SerializeField] private StatLineListView statListView;

        [Header("Inventory References")]
        [SerializeField] private RectTransform inventoryPanelBounds;
        [SerializeField] private TMP_Text inventoryStatusText;
        [SerializeField] private InventoryItemGridView inventoryGridView;
        [SerializeField] private EquipmentSlotsPanelView equipmentSlotsView;
        [SerializeField] private InventoryDropZoneView inventoryDropZoneView;
        [SerializeField] private InventoryItemTooltipView itemTooltipView;
        [SerializeField] private PotentialUpgradeOptionsPopupView itemOptionsPopupView;
        [SerializeField] private InventoryDropQuantityPopupView dropQuantityPopupView;
        [SerializeField] private InventoryItemPresentationCatalog itemPresentationCatalog;

        [Header("Character Display")]
        [SerializeField] private bool useCurrentValuesForHpMp;
        [SerializeField] private string missingCharacterName = "Chua co nhan vat";
        [SerializeField] private string loadingCharacterName = "Dang tai...";

        [Header("Inventory Display")]
        [SerializeField] private string inventoryNotLoadedText = "Kho do chua duoc tai.";
        [SerializeField] private string inventoryLoadingText = "Dang tai kho do...";
        [SerializeField] private string emptyInventoryText = "Kho do dang trong.";
        [SerializeField] private string inventoryActionInProgressText = "Dang cap nhat trang bi...";
        [SerializeField] private string inventoryDropActionText = "Dang vut vat pham...";
        [SerializeField] private string inventoryDropSuccessText = "Da vut vat pham.";
        [SerializeField] private string inventoryDropUnavailableText = "Vat pham nay khong the vut.";
        [SerializeField] private string inventoryUnsupportedUseText = "Vat pham nay chua co logic su dung o phase nay.";
        [SerializeField] private string inventoryMartialArtAlreadyLearnedText = "Cong phap nay da hoc roi, khong the dung them sach.";
        [SerializeField] private string inventoryAlreadyEquippedText = "Trang bi nay dang duoc mac.";
        [SerializeField] private string inventoryUnequipActionText = "Dang go trang bi...";

        [Header("Character Reload")]
        [SerializeField] private bool autoLoadMissingCharacterData = true;
        [SerializeField] private float reloadRetryCooldownSeconds = 2f;

        [Header("Inventory Reload")]
        [SerializeField] private bool autoLoadMissingInventoryData = true;
        [SerializeField] private bool forceRefreshInventoryOnEnable;
        [SerializeField] private float inventoryReloadRetryCooldownSeconds = 2f;

        private Guid? lastRequestedCharacterId;
        private float lastReloadAttemptTime = float.NegativeInfinity;
        private bool reloadInFlight;
        private string lastCharacterName = string.Empty;
        private string lastStatsSnapshot = string.Empty;
        private float lastInventoryReloadAttemptTime = float.NegativeInfinity;
        private bool inventoryReloadInFlight;
        private string lastInventorySnapshot = string.Empty;
        private string lastInventoryStatus = string.Empty;
        private long? previewPlayerItemId;
        private long? popupPlayerItemId;
        private bool suppressTooltipUntilHoverReset;
        private bool inventoryActionInFlight;
        private long? dropQuantityPopupPlayerItemId;

        private void Awake()
        {
            if (inventoryGridView != null)
            {
                inventoryGridView.ItemClicked += HandleInventoryItemClicked;
                inventoryGridView.ItemHovered += HandleInventoryItemHovered;
                inventoryGridView.ItemHoverExited += HandleInventoryItemHoverExited;
                inventoryGridView.EquippedItemDropped += HandleEquippedItemDroppedOnInventory;
            }

            if (equipmentSlotsView != null)
            {
                equipmentSlotsView.ItemClicked += HandleInventoryItemClicked;
                equipmentSlotsView.ItemHovered += HandleInventoryItemHovered;
                equipmentSlotsView.ItemHoverExited += HandleInventoryItemHoverExited;
                equipmentSlotsView.InventoryItemDroppedOnSlot += HandleInventoryItemDroppedOnEquipmentSlot;
            }

            if (inventoryDropZoneView != null)
            {
                inventoryDropZoneView.EquippedItemDropped += HandleEquippedItemDroppedOnInventory;
            }

            if (inventoryPanelBounds == null)
                inventoryPanelBounds = transform as RectTransform;
        }

        private void OnEnable()
        {
            RefreshFromRuntime(force: true);
            RefreshInventory(force: true);
            TryReloadMissingData();

            if (forceRefreshInventoryOnEnable)
                _ = ReloadInventoryAsync(forceRefresh: true);
            else
                TryReloadInventory();
        }

        private void Update()
        {
            if (!isActiveAndEnabled)
                return;

            RefreshFromRuntime(force: false);
            RefreshInventory(force: false);
            TryReloadMissingData();
            TryReloadInventory();
            UpdateItemPopupVisibility();
        }

        private void OnDisable()
        {
            HideItemOptionsPopup(force: true);
            HideDropQuantityPopup(force: true);
        }

        private void OnDestroy()
        {
            if (inventoryGridView != null)
            {
                inventoryGridView.ItemClicked -= HandleInventoryItemClicked;
                inventoryGridView.ItemHovered -= HandleInventoryItemHovered;
                inventoryGridView.ItemHoverExited -= HandleInventoryItemHoverExited;
                inventoryGridView.EquippedItemDropped -= HandleEquippedItemDroppedOnInventory;
            }

            if (equipmentSlotsView != null)
            {
                equipmentSlotsView.ItemClicked -= HandleInventoryItemClicked;
                equipmentSlotsView.ItemHovered -= HandleInventoryItemHovered;
                equipmentSlotsView.ItemHoverExited -= HandleInventoryItemHoverExited;
                equipmentSlotsView.InventoryItemDroppedOnSlot -= HandleInventoryItemDroppedOnEquipmentSlot;
            }

            if (inventoryDropZoneView != null)
            {
                inventoryDropZoneView.EquippedItemDropped -= HandleEquippedItemDroppedOnInventory;
            }
        }

        private void RefreshFromRuntime(bool force)
        {
            if (!ClientRuntime.IsInitialized)
            {
                ApplyCharacterName(missingCharacterName, force);
                ApplyStatEntries(Array.Empty<StatLineListView.Entry>(), force);
                return;
            }

            var selectedCharacter = ClientRuntime.Character.SelectedCharacter;
            var baseStats = ClientRuntime.Character.BaseStats;
            var currentState = ClientRuntime.Character.CurrentState;

            var isMissingData = !selectedCharacter.HasValue || !baseStats.HasValue;
            var displayName = isMissingData
                ? (reloadInFlight ? loadingCharacterName : missingCharacterName)
                : ResolveCharacterName(selectedCharacter.Value.Name);

            ApplyCharacterName(displayName, force);

            if (!baseStats.HasValue)
            {
                ApplyStatEntries(Array.Empty<StatLineListView.Entry>(), force);
                return;
            }

            var stats = baseStats.Value;
            var hpValue = useCurrentValuesForHpMp && currentState.HasValue
                ? currentState.Value.CurrentHp
                : GetTotalHp(stats);
            var mpValue = useCurrentValuesForHpMp && currentState.HasValue
                ? currentState.Value.CurrentMp
                : GetTotalMp(stats);

            var entries = new[]
            {
                new StatLineListView.Entry("HP", hpValue.ToString(CultureInfo.InvariantCulture)),
                new StatLineListView.Entry("MP", mpValue.ToString(CultureInfo.InvariantCulture)),
                new StatLineListView.Entry("ATK", GetTotalAttack(stats).ToString(CultureInfo.InvariantCulture)),
                new StatLineListView.Entry("Speed", GetTotalSpeed(stats).ToString(CultureInfo.InvariantCulture)),
                new StatLineListView.Entry("Co duyen", GetTotalFortune(stats).ToString("0.##", CultureInfo.InvariantCulture)),
                new StatLineListView.Entry("Than thuc", GetTotalSpiritualSense(stats).ToString(CultureInfo.InvariantCulture)),
            };

            ApplyStatEntries(entries, force);
        }

        private void RefreshInventory(bool force)
        {
            if (!ClientRuntime.IsInitialized)
            {
                ApplyInventoryStatus(inventoryNotLoadedText, force);
                ClearInventoryVisuals(force);
                return;
            }

            var inventoryState = ClientRuntime.Inventory;
            var allItems = SortInventoryItems(inventoryState.Items);
            var equippedItems = allItems.Where(x => x.IsEquipped).OrderBy(x => x.EquippedSlot ?? int.MaxValue).ThenBy(x => x.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase).ToList();
            var bagItems = allItems.Where(x => !x.IsEquipped).ToList();
            var status = ResolveInventoryStatus(inventoryState, bagItems.Count, equippedItems.Count);
            var snapshot = BuildInventorySnapshot(inventoryState, allItems, inventoryActionInFlight);

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

            InventoryItemModel activeTooltipItem;
            if (TryResolveActiveTooltipItem(allItems, out activeTooltipItem))
                ShowTooltip(activeTooltipItem, force: true);
            else if (itemTooltipView != null)
                itemTooltipView.Hide(force: true);
        }

        private void TryReloadMissingData()
        {
            if (!autoLoadMissingCharacterData || reloadInFlight || !ClientRuntime.IsInitialized)
                return;

            if (ClientRuntime.Connection.State != ClientConnectionState.Connected)
                return;

            var selectedCharacterId = ClientRuntime.Character.SelectedCharacterId;
            if (!selectedCharacterId.HasValue)
                return;

            if (ClientRuntime.Character.SelectedCharacter.HasValue && ClientRuntime.Character.BaseStats.HasValue)
                return;

            if (lastRequestedCharacterId == selectedCharacterId &&
                Time.unscaledTime - lastReloadAttemptTime < reloadRetryCooldownSeconds)
            {
                return;
            }

            _ = ReloadCharacterDataAsync(selectedCharacterId.Value);
        }

        private void TryReloadInventory()
        {
            if (!autoLoadMissingInventoryData || inventoryReloadInFlight || !ClientRuntime.IsInitialized)
                return;

            if (ClientRuntime.Connection.State != ClientConnectionState.Connected)
                return;

            if (ClientRuntime.Inventory.HasLoadedInventory)
                return;

            if (Time.unscaledTime - lastInventoryReloadAttemptTime < inventoryReloadRetryCooldownSeconds)
                return;

            _ = ReloadInventoryAsync(forceRefresh: false);
        }

        private async System.Threading.Tasks.Task ReloadCharacterDataAsync(Guid characterId)
        {
            reloadInFlight = true;
            lastRequestedCharacterId = characterId;
            lastReloadAttemptTime = Time.unscaledTime;
            RefreshFromRuntime(force: true);

            try
            {
                var result = await ClientRuntime.CharacterService.LoadCharacterDataAsync(characterId);
                if (!result.Success)
                    ClientLog.Warn($"WorldInventoryPanelController failed to load character data: {result.Message}");
            }
            catch (Exception ex)
            {
                ClientLog.Warn($"WorldInventoryPanelController reload exception: {ex.Message}");
            }
            finally
            {
                reloadInFlight = false;
                RefreshFromRuntime(force: true);
            }
        }

        private async System.Threading.Tasks.Task ReloadInventoryAsync(bool forceRefresh)
        {
            if (!ClientRuntime.IsInitialized)
                return;

            inventoryReloadInFlight = true;
            lastInventoryReloadAttemptTime = Time.unscaledTime;
            RefreshInventory(force: true);

            try
            {
                var result = await ClientRuntime.InventoryService.LoadInventoryAsync(forceRefresh);
                if (!result.Success)
                    ClientLog.Warn($"WorldInventoryPanelController failed to load inventory: {result.Message}");
            }
            catch (Exception ex)
            {
                ClientLog.Warn($"WorldInventoryPanelController inventory reload exception: {ex.Message}");
            }
            finally
            {
                inventoryReloadInFlight = false;
                RefreshInventory(force: true);
            }
        }

        private void ApplyCharacterName(string characterName, bool force)
        {
            characterName = ResolveCharacterName(characterName);
            if (!force && string.Equals(lastCharacterName, characterName, StringComparison.Ordinal))
                return;

            lastCharacterName = characterName;
            if (characterNameText != null)
                characterNameText.text = characterName;
        }

        private void ApplyStatEntries(IReadOnlyList<StatLineListView.Entry> entries, bool force)
        {
            var snapshot = BuildStatSnapshot(entries);
            if (!force && string.Equals(lastStatsSnapshot, snapshot, StringComparison.Ordinal))
                return;

            lastStatsSnapshot = snapshot;
            if (statListView != null)
                statListView.SetEntries(entries, force: true);
        }

        private void ApplyInventoryStatus(string status, bool force)
        {
            status = string.IsNullOrWhiteSpace(status) ? string.Empty : status.Trim();
            if (!force && string.Equals(lastInventoryStatus, status, StringComparison.Ordinal))
                return;

            lastInventoryStatus = status;
            if (inventoryStatusText != null)
                inventoryStatusText.text = status;
        }

        private void ClearInventoryVisuals(bool force)
        {
            if (inventoryGridView != null)
                inventoryGridView.Clear(force: true);

            if (equipmentSlotsView != null)
                equipmentSlotsView.Clear(force: true);

            if (itemTooltipView != null)
                itemTooltipView.Hide(force: true);
        }

        private void ShowTooltip(InventoryItemModel item, bool force)
        {
            if (itemTooltipView == null)
                return;

            var presentation = itemPresentationCatalog != null
                ? itemPresentationCatalog.Resolve(item)
                : new InventoryItemPresentation(null, null, Color.white);
            itemTooltipView.Show(item, presentation, force);
        }

        private void UpdateItemPopupVisibility()
        {
            if (itemOptionsPopupView == null || !itemOptionsPopupView.IsVisible)
            {
                UpdateDropQuantityPopupVisibility();
                return;
            }

            if (popupPlayerItemId.HasValue &&
                (!ClientRuntime.IsInitialized || !ClientRuntime.Inventory.TryGetItem(popupPlayerItemId.Value, out _)))
            {
                HideItemOptionsPopup(force: true);
                UpdateDropQuantityPopupVisibility();
                return;
            }

            if (DidClickBlankSpaceInsideInventoryPanel())
                HideItemOptionsPopup();

            UpdateDropQuantityPopupVisibility();
        }

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

            var options = new List<PotentialUpgradeOptionsPopupView.OptionEntry>(2);
            options.Add(BuildUseOption(item));
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

        private bool DidClickBlankSpaceInsideInventoryPanel()
        {
            if (!Input.GetMouseButtonDown(0) || inventoryPanelBounds == null)
                return false;

            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;

            var canvas = inventoryPanelBounds.GetComponentInParent<Canvas>();
            var eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            if (!RectTransformUtility.RectangleContainsScreenPoint(inventoryPanelBounds, Input.mousePosition, eventCamera))
                return false;

            var pointerData = new PointerEventData(eventSystem)
            {
                position = Input.mousePosition
            };
            var results = new List<RaycastResult>(8);
            eventSystem.RaycastAll(pointerData, results);

            for (var i = 0; i < results.Count; i++)
            {
                var hitTransform = results[i].gameObject != null ? results[i].gameObject.transform : null;
                if (hitTransform == null)
                    continue;

                if (hitTransform.GetComponentInParent<InventoryItemSlotView>() != null)
                    return false;

                if (hitTransform.GetComponentInParent<EquipmentSlotView>() != null)
                    return false;

                if (itemOptionsPopupView != null && hitTransform.IsChildOf(itemOptionsPopupView.transform))
                    return false;
            }

            return true;
        }

        private static List<InventoryItemModel> SortInventoryItems(IReadOnlyList<InventoryItemModel> items)
        {
            if (items == null || items.Count == 0)
                return new List<InventoryItemModel>(0);

            return items
                .OrderByDescending(x => x.Rarity)
                .ThenBy(x => x.ItemType)
                .ThenBy(x => x.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.PlayerItemId)
                .ToList();
        }

        private string ResolveInventoryStatus(ClientInventoryState inventoryState, int bagItemCount, int equippedItemCount)
        {
            if (inventoryActionInFlight)
                return inventoryActionInProgressText;

            if (!inventoryState.HasLoadedInventory)
                return inventoryState.IsLoading || inventoryReloadInFlight ? inventoryLoadingText : inventoryNotLoadedText;

            if (bagItemCount <= 0 && equippedItemCount <= 0)
                return emptyInventoryText;

            if (bagItemCount <= 0)
                return string.Format(CultureInfo.InvariantCulture, "Balo dang trong | {0} trang bi dang mac", equippedItemCount);

            if (equippedItemCount <= 0)
                return string.Format(CultureInfo.InvariantCulture, "{0} vat pham", bagItemCount);

            return string.Format(CultureInfo.InvariantCulture, "{0} vat pham | {1} trang bi dang mac", bagItemCount, equippedItemCount);
        }

        private bool TryResolveActiveTooltipItem(IReadOnlyList<InventoryItemModel> items, out InventoryItemModel item)
        {
            if (popupPlayerItemId.HasValue || suppressTooltipUntilHoverReset)
            {
                item = default;
                return false;
            }

            if (TryFindInventoryItemById(items, previewPlayerItemId, out item))
                return true;

            item = default;
            return false;
        }

        private static bool TryFindInventoryItemById(IReadOnlyList<InventoryItemModel> items, long? playerItemId, out InventoryItemModel item)
        {
            if (!playerItemId.HasValue)
            {
                item = default;
                return false;
            }

            for (var i = 0; i < items.Count; i++)
            {
                if (items[i].PlayerItemId != playerItemId.Value)
                    continue;

                item = items[i];
                return true;
            }

            item = default;
            return false;
        }

        private static string ResolveCharacterName(string rawName)
        {
            return string.IsNullOrWhiteSpace(rawName) ? "-" : rawName.Trim();
        }

        private static int GetTotalHp(CharacterBaseStatsModel stats)
        {
            return stats.FinalHp;
        }

        private static int GetTotalMp(CharacterBaseStatsModel stats)
        {
            return stats.FinalMp;
        }

        private static int GetTotalAttack(CharacterBaseStatsModel stats)
        {
            return stats.FinalAttack;
        }

        private static int GetTotalSpeed(CharacterBaseStatsModel stats)
        {
            return stats.FinalSpeed;
        }

        private static int GetTotalSpiritualSense(CharacterBaseStatsModel stats)
        {
            return stats.FinalSpiritualSense;
        }

        private static double GetTotalFortune(CharacterBaseStatsModel stats)
        {
            return stats.FinalFortune;
        }

        private static string BuildStatSnapshot(IReadOnlyList<StatLineListView.Entry> entries)
        {
            if (entries == null || entries.Count == 0)
                return string.Empty;

            var parts = new List<string>(entries.Count);
            for (var i = 0; i < entries.Count; i++)
                parts.Add(string.Concat(entries[i].Name ?? string.Empty, "=", entries[i].Value ?? string.Empty));

            return string.Join("|", parts);
        }

        private static string BuildInventorySnapshot(ClientInventoryState inventoryState, IReadOnlyList<InventoryItemModel> items, bool inventoryActionInFlight)
        {
            var parts = new List<string>(items.Count + 3)
            {
                inventoryState.HasLoadedInventory ? "loaded" : "not-loaded",
                inventoryState.IsLoading ? "loading" : "idle",
                inventoryActionInFlight ? "action" : "stable",
                inventoryState.LastStatusMessage ?? string.Empty
            };

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                parts.Add(string.Concat(
                    item.PlayerItemId.ToString(CultureInfo.InvariantCulture),
                    ":",
                    item.ItemTemplateId.ToString(CultureInfo.InvariantCulture),
                    ":",
                    item.Quantity.ToString(CultureInfo.InvariantCulture),
                    ":",
                    item.IsEquipped ? "1" : "0",
                    ":",
                    item.EnhanceLevel.ToString(CultureInfo.InvariantCulture),
                    ":",
                    item.Durability.HasValue ? item.Durability.Value.ToString(CultureInfo.InvariantCulture) : "-",
                    ":",
                    item.MartialArtBookMartialArtId.HasValue ? item.MartialArtBookMartialArtId.Value.ToString(CultureInfo.InvariantCulture) : "-",
                    ":",
                    item.Icon ?? string.Empty,
                    ":",
                    item.BackgroundIcon ?? string.Empty,
                    ":",
                    item.Name ?? string.Empty,
                    ":",
                    item.Description ?? string.Empty));
            }

            return string.Join("|", parts);
        }
    }
}
