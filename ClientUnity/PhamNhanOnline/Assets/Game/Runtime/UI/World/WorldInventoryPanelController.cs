using System;
using System.Globalization;
using System.Linq;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Features.Inventory.Application;
using PhamNhanOnline.Client.Network.Session;
using PhamNhanOnline.Client.UI.Inventory;
using PhamNhanOnline.Client.UI.Potential;
using TMPro;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.World
{
    public sealed partial class WorldInventoryPanelController : MonoBehaviour
    {
        private const string MissingCharacterName = "Chua co nhan vat";
        private const string LoadingCharacterName = "Dang tai...";
        private const string InventoryNotLoadedText = "Kho do chua duoc tai.";
        private const string InventoryLoadingText = "Dang tai kho do...";
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

        [Header("Character References")]
        [SerializeField] private InventoryCharacterSummaryView characterSummaryView;

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
        private float lastInventoryReloadAttemptTime = float.NegativeInfinity;
        private bool inventoryReloadInFlight;
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

        private void RefreshFromRuntime(bool force)
        {
            if (!ClientRuntime.IsInitialized)
            {
                ApplyCharacterName(MissingCharacterName, force);
                ApplyStats("-", "-", "-", "-", "-", "-", force);
                ApplyLifespan(null, force);
                return;
            }

            var selectedCharacter = ClientRuntime.Character.SelectedCharacter;
            var baseStats = ClientRuntime.Character.BaseStats;
            var currentState = ClientRuntime.Character.CurrentState;

            var isMissingData = !selectedCharacter.HasValue || !baseStats.HasValue;
            var displayName = isMissingData
                ? (reloadInFlight ? LoadingCharacterName : MissingCharacterName)
                : ResolveCharacterName(selectedCharacter.Value.Name);

            ApplyCharacterName(displayName, force);
            ApplyLifespan(currentState.HasValue ? currentState.Value.LifespanEndUnixMs : null, force);

            if (!baseStats.HasValue)
            {
                ApplyStats("-", "-", "-", "-", "-", "-", force);
                return;
            }

            var stats = baseStats.Value;
            var hpValue = GetTotalHp(stats);
            var mpValue = GetTotalMp(stats);
            ApplyStats(
                hpValue.ToString(CultureInfo.InvariantCulture),
                mpValue.ToString(CultureInfo.InvariantCulture),
                GetTotalAttack(stats).ToString(CultureInfo.InvariantCulture),
                GetTotalSpeed(stats).ToString(CultureInfo.InvariantCulture),
                GetTotalLuck(stats).ToString("0.##", CultureInfo.InvariantCulture),
                GetTotalSense(stats).ToString(CultureInfo.InvariantCulture),
                force);
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
                (modalUIManager == null || !modalUIManager.IsInventoryItemOptionsPopupVisible))
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
            ApplyInventoryStatus(InventoryLoadingText, force: true);

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
