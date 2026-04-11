using System;
using System.Collections.Generic;
using System.Globalization;
using GameShared.Models;
using PhamNhanOnline.Client.UI.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PhamNhanOnline.Client.UI.Crafting
{
    public sealed class CraftRecipeListView : MonoBehaviour, IDropHandler
    {
        [Header("References")]
        [SerializeField] private Transform contentRoot;
        [SerializeField] private CraftRecipeListItemView itemTemplate;

        [Header("Behavior")]
        [SerializeField] private bool hideTemplateObject = true;

        private readonly List<CraftRecipeListItemView> spawnedItems = new List<CraftRecipeListItemView>(8);
        private string lastSnapshot = string.Empty;
        private int lastItemCount = -1;
        private int? selectedRecipeId;

        public event Action<LearnedPillRecipeModel> ItemClicked;
        public event Action<LearnedPillRecipeModel> ItemHovered;
        public event Action ItemHoverExited;
        public event Action SelectedRecipeDroppedBackToList;

        private void Awake()
        {
            if (contentRoot == null && itemTemplate != null)
                contentRoot = itemTemplate.transform.parent;

            if (hideTemplateObject && itemTemplate != null)
                itemTemplate.gameObject.SetActive(false);
        }

        public void SetItems(
            IReadOnlyList<LearnedPillRecipeModel> items,
            int? selectedPillRecipeTemplateId,
            InventoryItemPresentationCatalog presentationCatalog,
            bool force = false)
        {
            items ??= Array.Empty<LearnedPillRecipeModel>();
            var snapshot = BuildSnapshot(items);
            var selectionChanged = selectedRecipeId != selectedPillRecipeTemplateId;
            selectedRecipeId = selectedPillRecipeTemplateId;
            if (!force &&
                !selectionChanged &&
                lastItemCount == items.Count &&
                string.Equals(lastSnapshot, snapshot, StringComparison.Ordinal))
            {
                UpdateSelectionVisuals(force: false);
                return;
            }

            lastItemCount = items.Count;
            lastSnapshot = snapshot;

            EnsureItemCount(items.Count);
            for (var i = 0; i < spawnedItems.Count; i++)
            {
                var itemView = spawnedItems[i];
                if (itemView == null)
                    continue;

                var shouldBeVisible = i < items.Count;
                if (itemView.gameObject.activeSelf != shouldBeVisible)
                    itemView.gameObject.SetActive(shouldBeVisible);

                if (!shouldBeVisible)
                {
                    itemView.Clear(force: true);
                    continue;
                }

                var presentation = presentationCatalog != null
                    ? presentationCatalog.Resolve(items[i].ResultPill)
                    : new InventoryItemPresentation(null, null, Color.white);
                itemView.SetRecipe(items[i], presentation, force: true);
                itemView.SetSelected(
                    selectedRecipeId.HasValue && items[i].PillRecipeTemplateId == selectedRecipeId.Value,
                    force: true);
            }
        }

        public void Clear(bool force = false)
        {
            lastItemCount = 0;
            lastSnapshot = string.Empty;
            selectedRecipeId = null;

            for (var i = 0; i < spawnedItems.Count; i++)
            {
                var itemView = spawnedItems[i];
                if (itemView == null)
                    continue;

                itemView.Clear(force: true);
                if (itemView.gameObject.activeSelf)
                    itemView.gameObject.SetActive(false);
            }
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

        private void EnsureItemCount(int targetCount)
        {
            if (targetCount <= spawnedItems.Count)
                return;

            if (itemTemplate == null)
            {
                Debug.LogWarning("CraftRecipeListView is missing itemTemplate.");
                return;
            }

            var parent = contentRoot != null ? contentRoot : itemTemplate.transform.parent;
            for (var i = spawnedItems.Count; i < targetCount; i++)
            {
                var instance = Instantiate(itemTemplate, parent);
                instance.name = string.Concat(itemTemplate.name, "_", i.ToString(CultureInfo.InvariantCulture));
                instance.gameObject.SetActive(true);
                instance.Clicked += HandleItemClicked;
                instance.Hovered += HandleItemHovered;
                instance.HoverExited += HandleItemHoverExited;
                spawnedItems.Add(instance);
            }
        }

        private void UpdateSelectionVisuals(bool force)
        {
            for (var i = 0; i < spawnedItems.Count; i++)
            {
                var itemView = spawnedItems[i];
                if (itemView == null || !itemView.gameObject.activeSelf || !itemView.HasRecipe)
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

        private static string BuildSnapshot(IReadOnlyList<LearnedPillRecipeModel> items)
        {
            if (items == null || items.Count == 0)
                return string.Empty;

            var parts = new string[items.Count];
            for (var i = 0; i < items.Count; i++)
            {
                parts[i] = string.Concat(
                    items[i].PillRecipeTemplateId.ToString(CultureInfo.InvariantCulture),
                    ":",
                    items[i].Code ?? string.Empty,
                    ":",
                    items[i].Name ?? string.Empty,
                    ":",
                    items[i].Description ?? string.Empty,
                    ":",
                    items[i].CraftDurationSeconds.ToString(CultureInfo.InvariantCulture),
                    ":",
                    items[i].ResultPill.ItemTemplateId.ToString(CultureInfo.InvariantCulture),
                    ":",
                    items[i].TotalCraftCount.ToString(CultureInfo.InvariantCulture));
            }

            return string.Join("|", parts);
        }
    }
}
