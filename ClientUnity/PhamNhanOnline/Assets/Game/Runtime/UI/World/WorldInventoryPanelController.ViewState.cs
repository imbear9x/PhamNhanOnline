using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Features.Inventory.Application;
using PhamNhanOnline.Client.UI.Common;
using PhamNhanOnline.Client.UI.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PhamNhanOnline.Client.UI.World
{
    public sealed partial class WorldInventoryPanelController
    {
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
                UpdateQuantityPopupVisibility();
                return;
            }

            if (popupPlayerItemId.HasValue &&
                (!ClientRuntime.IsInitialized || !ClientRuntime.Inventory.TryGetItem(popupPlayerItemId.Value, out _)))
            {
                HideItemOptionsPopup(force: true);
                UpdateQuantityPopupVisibility();
                return;
            }

            if (DidClickBlankSpaceInsideInventoryPanel())
                HideItemOptionsPopup();

            UpdateQuantityPopupVisibility();
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

        private static int GetTotalHp(CharacterBaseStatsModel stats) => stats.FinalHp;
        private static int GetTotalMp(CharacterBaseStatsModel stats) => stats.FinalMp;
        private static int GetTotalAttack(CharacterBaseStatsModel stats) => stats.FinalAttack;
        private static int GetTotalSpeed(CharacterBaseStatsModel stats) => stats.FinalSpeed;
        private static int GetTotalSpiritualSense(CharacterBaseStatsModel stats) => stats.FinalSpiritualSense;
        private static double GetTotalFortune(CharacterBaseStatsModel stats) => stats.FinalFortune;

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


