using System;
using System.Collections.Generic;
using System.Globalization;
using GameShared.Models;
using PhamNhanOnline.Client.UI.Common;
using PhamNhanOnline.Client.UI.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PhamNhanOnline.Client.UI.Crafting
{
    [RequireComponent(typeof(LoopVerticalListView))]
    public sealed class CraftRecipeListView : MonoBehaviour, IDropHandler
    {
        [Header("References")]
        [SerializeField] private LoopVerticalListView loopListView;

        [Header("Debug")]
        [SerializeField] private bool debugUsePlaceholderItems = true;
        [SerializeField, Min(0)] private int debugPlaceholderItemCount = 100;

        private readonly HashSet<CraftRecipeListItemView> subscribedItems = new HashSet<CraftRecipeListItemView>();
        private LearnedPillRecipeModel[] debugItems = Array.Empty<LearnedPillRecipeModel>();
        private IReadOnlyList<LearnedPillRecipeModel> items = Array.Empty<LearnedPillRecipeModel>();
        private InventoryItemPresentationCatalog presentationCatalog;
        private string lastSnapshot = string.Empty;
        private int lastItemCount = -1;
        private int? selectedRecipeId;
        private bool loopInitialized;

        public event Action<LearnedPillRecipeModel> ItemClicked;
        public event Action<LearnedPillRecipeModel> ItemHovered;
        public event Action ItemHoverExited;
        public event Action SelectedRecipeDroppedBackToList;

        private void Awake()
        {
            if (loopListView == null)
                loopListView = GetComponent<LoopVerticalListView>();
        }

        private void Start()
        {
            ValidateSerializedReferences();
            EnsureLoopInitialized();
        }

        public void SetItems(
            IReadOnlyList<LearnedPillRecipeModel> value,
            int? selectedPillRecipeTemplateId,
            InventoryItemPresentationCatalog valuePresentationCatalog,
            bool force = false)
        {
            value ??= Array.Empty<LearnedPillRecipeModel>();
            var resolvedItems = ResolveDisplayItems(value);
            var snapshot = BuildSnapshot(resolvedItems);
            var selectionChanged = selectedRecipeId != selectedPillRecipeTemplateId;

            items = resolvedItems;
            presentationCatalog = valuePresentationCatalog;
            selectedRecipeId = selectedPillRecipeTemplateId;

            EnsureLoopInitialized();

            if (!force &&
                !selectionChanged &&
                lastItemCount == resolvedItems.Count &&
                string.Equals(lastSnapshot, snapshot, StringComparison.Ordinal))
            {
                UpdateVisibleSelectionVisuals(force: false);
                return;
            }

            lastItemCount = resolvedItems.Count;
            lastSnapshot = snapshot;

            loopListView.SetListItemCount(resolvedItems.Count, keepPosition: true);
            loopListView.RefreshAllShownItem();
        }

        public void Clear(bool force = false)
        {
            items = ResolveDisplayItems(Array.Empty<LearnedPillRecipeModel>());
            presentationCatalog = null;
            lastItemCount = items.Count;
            lastSnapshot = BuildSnapshot(items);
            selectedRecipeId = null;

            EnsureLoopInitialized();
            loopListView.SetListItemCount(items.Count, keepPosition: false);
            loopListView.RefreshAllShownItem();
        }

        public void OnDrop(PointerEventData eventData)
        {
            var slotView = eventData.pointerDrag != null
                ? eventData.pointerDrag.GetComponentInParent<CraftRecipeSlotView>()
                : null;
            if (slotView == null || !slotView.HasRecipe)
                return;

            SelectedRecipeDroppedBackToList?.Invoke();
        }

        private void EnsureLoopInitialized()
        {
            if (loopInitialized || loopListView == null)
                return;

            loopListView.InitListView(items.Count, OnGetItemByIndex);
            loopInitialized = true;
        }

        private LoopScrollViewItem OnGetItemByIndex(LoopVerticalListView listView, int itemIndex)
        {
            if (itemIndex < 0 || itemIndex >= items.Count)
                return null;

            var itemView = listView.NewListViewItem() as CraftRecipeListItemView;
            if (itemView == null)
                return null;

            SubscribeItem(itemView);

            var item = items[itemIndex];
            var presentation = presentationCatalog != null
                ? presentationCatalog.Resolve(item.ResultPill)
                : new InventoryItemPresentation(null, null, default);
            itemView.SetRecipe(item, presentation, force: true);
            itemView.SetSelected(
                selectedRecipeId.HasValue && item.PillRecipeTemplateId == selectedRecipeId.Value,
                force: true);
            return itemView;
        }

        private void SubscribeItem(CraftRecipeListItemView itemView)
        {
            if (itemView == null || !subscribedItems.Add(itemView))
                return;

            itemView.Clicked += HandleItemClicked;
            itemView.Hovered += HandleItemHovered;
            itemView.HoverExited += HandleItemHoverExited;
        }

        private void UpdateVisibleSelectionVisuals(bool force)
        {
            if (loopListView == null || items == null || items.Count == 0)
                return;

            for (var i = 0; i < items.Count; i++)
            {
                var itemView = loopListView.GetShownItemByItemIndex(i) as CraftRecipeListItemView;
                if (itemView == null || !itemView.HasRecipe)
                    continue;

                itemView.SetSelected(
                    selectedRecipeId.HasValue && itemView.Recipe.PillRecipeTemplateId == selectedRecipeId.Value,
                    force);
            }
        }

        private void HandleItemClicked(CraftRecipeListItemView itemView)
        {
            if (itemView == null || !itemView.HasRecipe)
                return;

            ItemClicked?.Invoke(itemView.Recipe);
        }

        private void HandleItemHovered(CraftRecipeListItemView itemView)
        {
            if (itemView == null || !itemView.HasRecipe)
                return;

            ItemHovered?.Invoke(itemView.Recipe);
        }

        private void HandleItemHoverExited(CraftRecipeListItemView itemView)
        {
            ItemHoverExited?.Invoke();
        }

        private void ValidateSerializedReferences()
        {
            if (loopListView == null)
                throw new InvalidOperationException($"{nameof(CraftRecipeListView)} on '{gameObject.name}' is missing required reference '{nameof(loopListView)}'.");
        }

        private IReadOnlyList<LearnedPillRecipeModel> ResolveDisplayItems(IReadOnlyList<LearnedPillRecipeModel> sourceItems)
        {
            if (sourceItems != null && sourceItems.Count > 0)
                return sourceItems;

            if (!debugUsePlaceholderItems || debugPlaceholderItemCount <= 0)
                return Array.Empty<LearnedPillRecipeModel>();

            if (debugItems.Length == debugPlaceholderItemCount)
                return debugItems;

            debugItems = new LearnedPillRecipeModel[debugPlaceholderItemCount];
            for (var i = 0; i < debugItems.Length; i++)
            {
                debugItems[i] = new LearnedPillRecipeModel
                {
                    PillRecipeTemplateId = 100000 + i,
                    Code = string.Concat("debug_recipe_", (i + 1).ToString(CultureInfo.InvariantCulture)),
                    Name = string.Concat("Dan phuong test ", (i + 1).ToString(CultureInfo.InvariantCulture)),
                    Description = "Placeholder recipe for loop scroll UI test.",
                    CraftDurationSeconds = 30,
                    TotalCraftCount = 0,
                    BaseSuccessRate = 0.5d,
                };
            }

            return debugItems;
        }

        private static string BuildSnapshot(IReadOnlyList<LearnedPillRecipeModel> value)
        {
            if (value == null || value.Count == 0)
                return string.Empty;

            var parts = new string[value.Count];
            for (var i = 0; i < value.Count; i++)
            {
                parts[i] = string.Concat(
                    value[i].PillRecipeTemplateId.ToString(CultureInfo.InvariantCulture),
                    ":",
                    value[i].Code ?? string.Empty,
                    ":",
                    value[i].Name ?? string.Empty,
                    ":",
                    value[i].Description ?? string.Empty,
                    ":",
                    value[i].CraftDurationSeconds.ToString(CultureInfo.InvariantCulture),
                    ":",
                    value[i].ResultPill.ItemTemplateId.ToString(CultureInfo.InvariantCulture),
                    ":",
                    value[i].TotalCraftCount.ToString(CultureInfo.InvariantCulture));
            }

            return string.Join("|", parts);
        }
    }
}
