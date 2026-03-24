using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Features.Inventory.Application;
using PhamNhanOnline.Client.Network.Session;
using PhamNhanOnline.Client.UI.Common;
using PhamNhanOnline.Client.UI.Inventory;
using TMPro;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.World
{
    public sealed class WorldInventoryPanelController : MonoBehaviour
    {
        [Header("Character References")]
        [SerializeField] private TMP_Text characterNameText;
        [SerializeField] private StatLineListView statListView;

        [Header("Inventory References")]
        [SerializeField] private TMP_Text inventoryStatusText;
        [SerializeField] private InventoryItemGridView inventoryGridView;
        [SerializeField] private EquipmentSlotsPanelView equipmentSlotsView;
        [SerializeField] private InventoryDropZoneView inventoryDropZoneView;
        [SerializeField] private InventoryItemTooltipView itemTooltipView;
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
        private bool inventoryActionInFlight;

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

        private void HandleInventoryItemClicked(InventoryItemModel item)
        {
            // Tooltip/highlight are hover-only in this phase.
        }

        private void HandleInventoryItemHovered(InventoryItemModel item)
        {
            previewPlayerItemId = item.PlayerItemId;
            RefreshInventory(force: true);
        }

        private void HandleInventoryItemHoverExited()
        {
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
                RefreshFromRuntime(force: true);
                RefreshInventory(force: true);
            }
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
