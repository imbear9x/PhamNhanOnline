using System;
using System.Globalization;
using System.Linq;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Features.Inventory.Application;
using PhamNhanOnline.Client.Network.Session;
using PhamNhanOnline.Client.UI.Common;
using PhamNhanOnline.Client.UI.Inventory;
using PhamNhanOnline.Client.UI.Potential;
using TMPro;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.World
{
    public sealed partial class WorldInventoryPanelController : MonoBehaviour
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
                inventoryDropZoneView.EquippedItemDropped += HandleEquippedItemDroppedOnInventory;

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
                inventoryDropZoneView.EquippedItemDropped -= HandleEquippedItemDroppedOnInventory;
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
            if (!ClientRuntime.IsInitialized || ClientRuntime.Connection.State != ClientConnectionState.Connected)
                return;

            var selectedCharacterId = ClientRuntime.Character.SelectedCharacterId;
            if (!selectedCharacterId.HasValue)
                return;

            if (autoLoadMissingCharacterData &&
                !reloadInFlight &&
                (!ClientRuntime.Character.BaseStats.HasValue || !ClientRuntime.Character.CurrentState.HasValue) &&
                (lastRequestedCharacterId != selectedCharacterId.Value ||
                 Time.unscaledTime - lastReloadAttemptTime >= reloadRetryCooldownSeconds))
            {
                _ = ReloadCharacterDataAsync(selectedCharacterId.Value);
            }
        }

        private void TryReloadInventory()
        {
            if (!ClientRuntime.IsInitialized || ClientRuntime.Connection.State != ClientConnectionState.Connected)
                return;

            if (!autoLoadMissingInventoryData || inventoryReloadInFlight || ClientRuntime.Inventory.HasLoadedInventory)
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
                ClientLog.Warn($"WorldInventoryPanelController character reload exception: {ex.Message}");
            }
            finally
            {
                reloadInFlight = false;
                RefreshFromRuntime(force: true);
            }
        }

        private async System.Threading.Tasks.Task ReloadInventoryAsync(bool forceRefresh)
        {
            inventoryReloadInFlight = true;
            lastInventoryReloadAttemptTime = Time.unscaledTime;
            ApplyInventoryStatus(inventoryLoadingText, force: true);

            try
            {
                var result = await ClientRuntime.InventoryService.LoadInventoryAsync(forceRefresh: forceRefresh);
                if (!result.Success)
                {
                    ClientLog.Warn($"WorldInventoryPanelController failed to load inventory: {result.Message}");
                    ApplyInventoryStatus(result.Message, force: true);
                }
            }
            catch (Exception ex)
            {
                ApplyInventoryStatus(ex.Message, force: true);
                ClientLog.Warn($"WorldInventoryPanelController inventory reload exception: {ex.Message}");
            }
            finally
            {
                inventoryReloadInFlight = false;
                RefreshInventory(force: true);
            }
        }
    }
}
