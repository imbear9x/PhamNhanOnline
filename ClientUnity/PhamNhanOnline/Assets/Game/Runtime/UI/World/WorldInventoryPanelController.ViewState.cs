using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Features.Inventory.Application;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.World
{
    public sealed partial class WorldInventoryPanelController
    {
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
                return InventoryActionInProgressText;

            if (!inventoryState.HasLoadedInventory)
                return InventoryNotLoadedText;

            if (bagItemCount <= 0 && equippedItemCount <= 0)
                return EmptyInventoryText;

            if (bagItemCount <= 0)
                return string.Format(CultureInfo.InvariantCulture, "Balo dang trong | {0} trang bi dang mac", equippedItemCount);

            if (equippedItemCount <= 0)
                return string.Format(CultureInfo.InvariantCulture, "{0} vat pham", bagItemCount);

            return string.Format(CultureInfo.InvariantCulture, "{0} vat pham | {1} trang bi dang mac", bagItemCount, equippedItemCount);
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
