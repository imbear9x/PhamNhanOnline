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
        private long? selectedPlayerItemId;
        private long? previewPlayerItemId;

        private void Awake()
        {
            if (inventoryGridView != null)
            {
                inventoryGridView.ItemClicked += HandleInventoryItemClicked;
                inventoryGridView.ItemHovered += HandleInventoryItemHovered;
                inventoryGridView.ItemHoverExited += HandleInventoryItemHoverExited;
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
                : stats.BaseHp;
            var mpValue = useCurrentValuesForHpMp && currentState.HasValue
                ? currentState.Value.CurrentMp
                : stats.BaseMp;

            var entries = new[]
            {
                new StatLineListView.Entry("HP", hpValue.ToString(CultureInfo.InvariantCulture)),
                new StatLineListView.Entry("MP", mpValue.ToString(CultureInfo.InvariantCulture)),
                new StatLineListView.Entry("ATK", stats.BaseAttack.ToString(CultureInfo.InvariantCulture)),
                new StatLineListView.Entry("Speed", stats.BaseSpeed.ToString(CultureInfo.InvariantCulture)),
                new StatLineListView.Entry("Co duyen", stats.BaseFortune.ToString("0.##", CultureInfo.InvariantCulture)),
                new StatLineListView.Entry("Than thuc", stats.BaseSpiritualSense.ToString(CultureInfo.InvariantCulture)),
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
            var items = SortInventoryItems(inventoryState.Items);
            var status = ResolveInventoryStatus(inventoryState, items.Count);
            var snapshot = BuildInventorySnapshot(inventoryState, items);

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

            if (items.Count == 0)
            {
                ClearInventoryVisuals(force: true);
                return;
            }

            if (inventoryGridView != null)
            {
                inventoryGridView.SetItems(items, itemPresentationCatalog, force: true);
                inventoryGridView.SetSelectedItem(selectedPlayerItemId, force: true);
            }

            InventoryItemModel activeTooltipItem;
            if (TryResolveActiveTooltipItem(items, out activeTooltipItem))
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
            selectedPlayerItemId = item.PlayerItemId;
            previewPlayerItemId = item.PlayerItemId;
            RefreshInventory(force: true);
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

        private static List<InventoryItemModel> SortInventoryItems(IReadOnlyList<InventoryItemModel> items)
        {
            if (items == null || items.Count == 0)
                return new List<InventoryItemModel>(0);

            return items
                .OrderByDescending(x => x.IsEquipped)
                .ThenByDescending(x => x.Rarity)
                .ThenBy(x => x.ItemType)
                .ThenBy(x => x.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.PlayerItemId)
                .ToList();
        }

        private string ResolveInventoryStatus(ClientInventoryState inventoryState, int itemCount)
        {
            if (!inventoryState.HasLoadedInventory)
                return inventoryState.IsLoading || inventoryReloadInFlight ? inventoryLoadingText : inventoryNotLoadedText;

            if (itemCount <= 0)
                return emptyInventoryText;

            return string.Format(CultureInfo.InvariantCulture, "{0} vat pham", itemCount);
        }

        private bool TryResolveActiveTooltipItem(IReadOnlyList<InventoryItemModel> items, out InventoryItemModel item)
        {
            if (TryFindInventoryItemById(items, previewPlayerItemId, out item))
                return true;

            if (TryFindInventoryItemById(items, selectedPlayerItemId, out item))
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

        private static string BuildStatSnapshot(IReadOnlyList<StatLineListView.Entry> entries)
        {
            if (entries == null || entries.Count == 0)
                return string.Empty;

            var parts = new List<string>(entries.Count);
            for (var i = 0; i < entries.Count; i++)
                parts.Add(string.Concat(entries[i].Name ?? string.Empty, "=", entries[i].Value ?? string.Empty));

            return string.Join("|", parts);
        }

        private static string BuildInventorySnapshot(ClientInventoryState inventoryState, IReadOnlyList<InventoryItemModel> items)
        {
            var parts = new List<string>(items.Count + 3)
            {
                inventoryState.HasLoadedInventory ? "loaded" : "not-loaded",
                inventoryState.IsLoading ? "loading" : "idle",
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
