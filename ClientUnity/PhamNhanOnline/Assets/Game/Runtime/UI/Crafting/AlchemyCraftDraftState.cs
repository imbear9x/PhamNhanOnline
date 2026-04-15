using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GameShared.Models;

namespace PhamNhanOnline.Client.UI.Crafting
{
    public sealed class AlchemyCraftDraftState
    {
        private sealed class IngredientSelection
        {
            public readonly List<long> SelectedPlayerItemIds = new List<long>(4);
            public bool Armed;
            public int AssignedQuantity;
        }

        public readonly struct SelectionSnapshot
        {
            public SelectionSnapshot(bool armed, int assignedQuantity, IReadOnlyList<long> selectedPlayerItemIds)
            {
                Armed = armed;
                AssignedQuantity = assignedQuantity;
                SelectedPlayerItemIds = selectedPlayerItemIds ?? Array.Empty<long>();
            }

            public bool Armed { get; }
            public int AssignedQuantity { get; }
            public IReadOnlyList<long> SelectedPlayerItemIds { get; }
        }

        public readonly struct AssignInventoryItemResult
        {
            public AssignInventoryItemResult(bool success, bool requiresQuantityPrompt)
            {
                Success = success;
                RequiresQuantityPrompt = requiresQuantityPrompt;
            }

            public bool Success { get; }
            public bool RequiresQuantityPrompt { get; }
        }

        private readonly Dictionary<int, IngredientSelection> selectionsByInputId = new Dictionary<int, IngredientSelection>();

        public bool IsEmpty => selectionsByInputId.Count == 0;

        public void Clear()
        {
            selectionsByInputId.Clear();
        }

        public bool ClearInput(int inputId)
        {
            return selectionsByInputId.Remove(inputId);
        }

        public bool TryGetSelection(int inputId, out SelectionSnapshot selection)
        {
            if (selectionsByInputId.TryGetValue(inputId, out var value))
            {
                selection = new SelectionSnapshot(value.Armed, value.AssignedQuantity, value.SelectedPlayerItemIds);
                return true;
            }

            selection = default;
            return false;
        }

        public void SetAssignedQuantity(int inputId, int quantity)
        {
            if (!selectionsByInputId.TryGetValue(inputId, out var selection))
                return;

            selection.Armed = quantity > 0;
            selection.AssignedQuantity = Math.Max(0, quantity);
            if (selection.AssignedQuantity <= 0 && selection.SelectedPlayerItemIds.Count == 0)
                selectionsByInputId.Remove(inputId);
        }

        public AssignInventoryItemResult TryAssignInventoryItemToInput(
            IReadOnlyList<PillRecipeInputModel> inputs,
            int inputId,
            InventoryItemModel item)
        {
            if (inputs == null)
                return default;

            for (var i = 0; i < inputs.Count; i++)
            {
                var input = inputs[i];
                if (input.InputId != inputId || input.RequiredItem.ItemTemplateId != item.ItemTemplateId)
                    continue;

                if (!selectionsByInputId.TryGetValue(input.InputId, out var selection))
                {
                    selection = new IngredientSelection();
                    selectionsByInputId[input.InputId] = selection;
                }

                selection.Armed = true;
                if (input.RequiredItem.IsStackable)
                {
                    if (selection.AssignedQuantity <= 0)
                        selection.AssignedQuantity = Math.Max(1, input.RequiredQuantity);

                    return new AssignInventoryItemResult(success: true, requiresQuantityPrompt: true);
                }

                if (selection.SelectedPlayerItemIds.Contains(item.PlayerItemId))
                    return default;

                selection.SelectedPlayerItemIds.Add(item.PlayerItemId);
                selection.AssignedQuantity = selection.SelectedPlayerItemIds.Count;
                return new AssignInventoryItemResult(success: true, requiresQuantityPrompt: false);
            }

            return default;
        }

        public int ResolveAssignedQuantity(
            PillRecipeInputModel input,
            PracticeSessionModel? activeSession,
            IReadOnlyList<AlchemyConsumedItemModel> consumedItems,
            IReadOnlyList<InventoryItemModel> inventoryItems)
        {
            if (activeSession.HasValue)
                return ResolveConsumedQuantity(input, consumedItems);

            if (!selectionsByInputId.TryGetValue(input.InputId, out var selection) || !selection.Armed)
                return 0;

            if (!input.RequiredItem.IsStackable)
                return selection.SelectedPlayerItemIds.Count;

            if (input.IsOptional)
                return Math.Max(0, selection.AssignedQuantity);

            return ResolveInventoryQuantity(inventoryItems, input.RequiredItem.ItemTemplateId);
        }

        public bool ResolveInputArmed(
            PillRecipeInputModel input,
            PracticeSessionModel? activeSession,
            IReadOnlyList<AlchemyConsumedItemModel> consumedItems)
        {
            if (activeSession.HasValue)
                return ResolveConsumedQuantity(input, consumedItems) > 0;

            return selectionsByInputId.TryGetValue(input.InputId, out var selection) && selection.Armed;
        }

        public bool AreRequiredInputsReady(
            PillRecipeDetailModel detail,
            PracticeSessionModel? activeSession,
            IReadOnlyList<AlchemyConsumedItemModel> consumedItems,
            IReadOnlyList<InventoryItemModel> inventoryItems)
        {
            if (detail.Inputs == null || detail.Inputs.Count == 0)
                return true;

            for (var i = 0; i < detail.Inputs.Count; i++)
            {
                var input = detail.Inputs[i];
                if (input.IsOptional)
                    continue;

                var requiredQuantity = Math.Max(1, input.RequiredQuantity);
                if (ResolveAssignedQuantity(input, activeSession, consumedItems, inventoryItems) < requiredQuantity)
                    return false;
            }

            return true;
        }

        public int ResolvePreviewRequestedCraftCount(PillRecipeDetailModel detail, IReadOnlyList<InventoryItemModel> inventoryItems)
        {
            if (detail.Inputs == null || detail.Inputs.Count == 0)
                return 1;

            var maxCraftableCount = int.MaxValue;
            for (var i = 0; i < detail.Inputs.Count; i++)
            {
                var input = detail.Inputs[i];
                if (input.IsOptional)
                    continue;

                var availableQuantity = input.RequiredItem.IsStackable
                    ? ResolveInventoryQuantity(inventoryItems, input.RequiredItem.ItemTemplateId)
                    : (selectionsByInputId.TryGetValue(input.InputId, out var selection)
                        ? selection.SelectedPlayerItemIds.Count
                        : 0);
                var craftableForInput = availableQuantity / Math.Max(1, input.RequiredQuantity);
                maxCraftableCount = Math.Min(maxCraftableCount, craftableForInput);
            }

            return Math.Max(1, maxCraftableCount == int.MaxValue ? 1 : maxCraftableCount);
        }

        public long[] BuildSelectedPlayerItemIds()
        {
            return selectionsByInputId.Values
                .SelectMany(static selection => selection.SelectedPlayerItemIds)
                .Distinct()
                .OrderBy(static id => id)
                .ToArray();
        }

        public AlchemyOptionalInputSelectionModel[] BuildSelectedOptionalInputs(PillRecipeDetailModel detail)
        {
            if (detail.Inputs == null)
                return Array.Empty<AlchemyOptionalInputSelectionModel>();

            return detail.Inputs
                .Where(static input => input.IsOptional)
                .Where(input => selectionsByInputId.TryGetValue(input.InputId, out var selection) && selection.Armed)
                .Select(input => new AlchemyOptionalInputSelectionModel
                {
                    InputId = input.InputId,
                    Quantity = ResolveOptionalApplicationCount(input),
                })
                .Where(static selection => selection.Quantity > 0)
                .OrderBy(static selection => selection.InputId)
                .ToArray();
        }

        public string BuildSnapshot()
        {
            if (selectionsByInputId.Count == 0)
                return string.Empty;

            return string.Join(
                ";",
                selectionsByInputId
                    .OrderBy(static pair => pair.Key)
                    .Select(pair => string.Concat(
                        pair.Key.ToString(CultureInfo.InvariantCulture),
                        ":",
                        pair.Value.Armed ? "1" : "0",
                        ":",
                        pair.Value.AssignedQuantity.ToString(CultureInfo.InvariantCulture),
                        ":",
                        string.Join(",", pair.Value.SelectedPlayerItemIds.OrderBy(static id => id).Select(static id => id.ToString(CultureInfo.InvariantCulture))))));
        }

        public int ResolveOptionalApplicationCount(PillRecipeInputModel input)
        {
            if (!input.IsOptional ||
                !selectionsByInputId.TryGetValue(input.InputId, out var selection) ||
                !selection.Armed)
            {
                return 0;
            }

            var assignedQuantity = input.RequiredItem.IsStackable
                ? Math.Max(0, selection.AssignedQuantity)
                : selection.SelectedPlayerItemIds.Count;
            return Math.Max(0, assignedQuantity / Math.Max(1, input.RequiredQuantity));
        }

        private static int ResolveConsumedQuantity(PillRecipeInputModel input, IReadOnlyList<AlchemyConsumedItemModel> consumedItems)
        {
            if (consumedItems == null)
                return 0;

            var total = 0;
            for (var i = 0; i < consumedItems.Count; i++)
            {
                var entry = consumedItems[i];
                if (entry.Item.ItemTemplateId != input.RequiredItem.ItemTemplateId)
                    continue;

                total += Math.Max(0, entry.Quantity);
            }

            return total;
        }

        private static int ResolveInventoryQuantity(IReadOnlyList<InventoryItemModel> inventoryItems, int itemTemplateId)
        {
            if (inventoryItems == null)
                return 0;

            var total = 0;
            for (var i = 0; i < inventoryItems.Count; i++)
            {
                if (inventoryItems[i].IsEquipped || inventoryItems[i].ItemTemplateId != itemTemplateId)
                    continue;

                total += Math.Max(0, inventoryItems[i].Quantity);
            }

            return total;
        }
    }
}
