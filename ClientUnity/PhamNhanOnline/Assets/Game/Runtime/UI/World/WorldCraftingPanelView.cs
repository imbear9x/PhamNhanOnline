using System;
using System.Collections.Generic;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.UI.Common;
using PhamNhanOnline.Client.UI.Crafting;
using PhamNhanOnline.Client.UI.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.World
{
    [DisallowMultipleComponent]
    public sealed class WorldCraftingPanelView : ViewModelBase
    {
        private static readonly InventoryItemPresentation FallbackPresentation = new InventoryItemPresentation(null, null, Color.white);

        [Header("Recipe References")]
        [SerializeField] private TMP_Text panelTitleText;
        [SerializeField] private CraftRecipeListView recipeListView;
        [SerializeField] private CraftRecipeSlotView selectedRecipeSlotView;

        [Header("Inventory References")]
        [SerializeField] private InventoryItemGridView inventoryGridView;
        [SerializeField] private InventoryItemPresentationCatalog itemPresentationCatalog;

        [Header("Input References")]
        [FormerlySerializedAs("ingredientPanelView")]
        [SerializeField] private CraftInputPanelView inputPanelView;

        [Header("Result Preview")]
        [SerializeField] private CraftResultPreviewView craftingResultPreviewView;

        [Header("Drop Zone")]
        [SerializeField] private DropZoneView dropZoneView;

        [Header("Practice Controls")]
        [SerializeField] private UIButtonView closeButton;
        [SerializeField] private UIButtonView craftButton;
        [SerializeField] private UIButtonView pauseResumeButton;
        [SerializeField] private TMP_Text pauseResumeButtonText;
        [SerializeField] private UIButtonView cancelButton;

        public event Action<LearnedPillRecipeModel> RecipeListClicked;
        public event Action<LearnedPillRecipeModel> RecipeListHovered;
        public event Action RecipeHoverExited;
        public event Action<LearnedPillRecipeModel> SelectedRecipeDropped;
        public event Action SelectedRecipeClicked;
        public event Action SelectedRecipeHovered;
        public event Action SelectedRecipeHoverExited;
        public event Action SelectedRecipeDroppedBackToList;
        public event Action<int, InventoryItemModel> InputInventoryItemDropped;
        public event Action<int, bool, PointerEventData.InputButton> InputSlotClicked;
        public event Action<int, bool> InputSlotHovered;
        public event Action InputSlotHoverExited;
        public event Action<InventoryItemModel> InventoryItemClicked;
        public event Action<UIDragPayload> DropZonePayloadDropped;
        public event Action<PointerEventData.InputButton> DropZoneClicked;
        public event Action CloseClicked;
        public event Action CraftClicked;
        public event Action PauseResumeClicked;
        public event Action CancelClicked;

        protected override void Awake()
        {
            base.Awake();
            BindChildEvents();
        }

        private void OnDestroy()
        {
            UnbindChildEvents();
        }

        public void ValidateSerializedReferences()
        {
            ThrowIfMissing(recipeListView, nameof(recipeListView));
            ThrowIfMissing(selectedRecipeSlotView, nameof(selectedRecipeSlotView));
            ThrowIfMissing(inventoryGridView, nameof(inventoryGridView));
            ThrowIfMissing(itemPresentationCatalog, nameof(itemPresentationCatalog));
            ThrowIfMissing(inputPanelView, nameof(inputPanelView));
            ThrowIfMissing(dropZoneView, nameof(dropZoneView));
            ThrowIfMissing(closeButton, nameof(closeButton));
            ThrowIfMissing(craftButton, nameof(craftButton));
            ThrowIfMissing(pauseResumeButton, nameof(pauseResumeButton));
            ThrowIfMissing(pauseResumeButtonText, nameof(pauseResumeButtonText));
            ThrowIfMissing(cancelButton, nameof(cancelButton));
            ThrowIfMissing(craftingResultPreviewView, nameof(craftingResultPreviewView));
        }

        public void AdoptLegacyBindings(
            TMP_Text legacyPanelTitleText,
            CraftRecipeListView legacyRecipeListView,
            CraftRecipeSlotView legacySelectedRecipeSlotView,
            InventoryItemGridView legacyInventoryGridView,
            InventoryItemPresentationCatalog legacyItemPresentationCatalog,
            CraftInputPanelView legacyInputPanelView,
            CraftResultPreviewView legacyCraftingResultPreviewView,
            DropZoneView legacyDropZoneView,
            UIButtonView legacyCloseButton,
            UIButtonView legacyCraftButton,
            UIButtonView legacyPauseResumeButton,
            TMP_Text legacyPauseResumeButtonText,
            UIButtonView legacyCancelButton)
        {
            panelTitleText ??= legacyPanelTitleText;
            recipeListView ??= legacyRecipeListView;
            selectedRecipeSlotView ??= legacySelectedRecipeSlotView;
            inventoryGridView ??= legacyInventoryGridView;
            itemPresentationCatalog ??= legacyItemPresentationCatalog;
            inputPanelView ??= legacyInputPanelView;
            craftingResultPreviewView ??= legacyCraftingResultPreviewView;
            dropZoneView ??= legacyDropZoneView;
            closeButton ??= legacyCloseButton;
            craftButton ??= legacyCraftButton;
            pauseResumeButton ??= legacyPauseResumeButton;
            pauseResumeButtonText ??= legacyPauseResumeButtonText;
            cancelButton ??= legacyCancelButton;

            UnbindChildEvents();
            BindChildEvents();
        }

        public void Show()
        {
            ShowView();
        }

        public void Hide(bool force = false)
        {
            SetViewVisible(false, force);
        }

        public void SetPanelTitle(string text, bool force)
        {
            ApplyText(panelTitleText, text, force);
        }

        public void SetRecipeList(IReadOnlyList<LearnedPillRecipeModel> recipes, int? selectedRecipeId, bool force)
        {
            if (recipeListView == null)
                return;

            recipeListView.SetItems(recipes, selectedRecipeId, itemPresentationCatalog, force);
        }

        public void SetRecipeListInteractionLocked(bool locked)
        {
            recipeListView?.SetInteractionLocked(locked);
        }

        public void ClearRecipeList(bool force)
        {
            if (recipeListView != null)
                recipeListView.Clear(force);
        }

        public void SetSelectedRecipe(
            LearnedPillRecipeModel recipe,
            ItemTemplateSummaryModel resultItem,
            bool interactionLocked)
        {
            if (selectedRecipeSlotView == null)
                return;

            selectedRecipeSlotView.SetRecipe(
                recipe,
                ResolvePresentation(resultItem));
            selectedRecipeSlotView.SetInteractionLocked(interactionLocked);
        }

        public void ClearSelectedRecipe()
        {
            if (selectedRecipeSlotView != null)
                selectedRecipeSlotView.Clear();
        }

        public void SetSelectedRecipeInteractionLocked(bool locked)
        {
            if (selectedRecipeSlotView != null)
                selectedRecipeSlotView.SetInteractionLocked(locked);
        }

        public void SetInventoryItems(IReadOnlyList<InventoryItemModel> items, bool force)
        {
            if (inventoryGridView == null)
                return;

            inventoryGridView.SetItems(items, itemPresentationCatalog, force);
        }

        public void SetInventoryInteractionLocked(bool locked)
        {
            inventoryGridView?.SetInteractionLocked(locked);
        }

        public void ClearInventory(bool force)
        {
            if (inventoryGridView != null)
                inventoryGridView.Clear(force);
        }

        public void SetInputSlots(
            IReadOnlyList<CraftInputPanelView.SlotState> requiredSlots,
            IReadOnlyList<CraftInputPanelView.SlotState> optionalSlots)
        {
            if (inputPanelView != null)
                inputPanelView.SetSlots(requiredSlots, optionalSlots);
        }

        public void ClearInputs()
        {
            if (inputPanelView != null)
                inputPanelView.Clear();
        }

        public void SetCraftingResultPreview(
            ItemTemplateSummaryModel resultItem,
            string itemName,
            string successRateText,
            string durationText,
            int quantity,
            float progressFillAmount,
            string progressText,
            string statusText)
        {
            if (craftingResultPreviewView == null)
                return;

            craftingResultPreviewView.SetState(
                ResolvePresentation(resultItem),
                itemName,
                successRateText,
                durationText,
                quantity,
                progressFillAmount,
                progressText,
                statusText);
        }

        public void ClearCraftingResultPreview()
        {
            if (craftingResultPreviewView != null)
                craftingResultPreviewView.Clear();
        }

        public void SetCraftButtonState(bool visible, bool interactable, string label)
        {
            if (craftButton != null)
            {
                craftButton.gameObject.SetActive(visible);
                craftButton.SetInteractable(interactable, force: true);
            }
        }

        public void SetPauseResumeButtonState(bool visible, bool interactable, string label)
        {
            if (pauseResumeButton != null)
            {
                pauseResumeButton.gameObject.SetActive(visible);
                pauseResumeButton.SetInteractable(interactable, force: true);
            }

            if (pauseResumeButtonText != null)
                pauseResumeButtonText.text = label ?? string.Empty;
        }

        public void SetCloseButtonVisible(bool visible)
        {
            if (closeButton != null && closeButton.gameObject.activeSelf != visible)
                closeButton.gameObject.SetActive(visible);
        }

        public void SetCancelButtonState(bool visible, bool interactable, string label)
        {
            if (cancelButton != null)
            {
                cancelButton.gameObject.SetActive(visible);
                cancelButton.SetInteractable(interactable, force: true);
            }
        }

        public void ShowRecipeTooltip(
            PillRecipeDetailModel detail,
            Func<PillRecipeInputModel, int> quantityResolver,
            bool force = false)
        {
            WorldModalUIManager.Instance?.ShowRecipeTooltip(detail, quantityResolver, force);
        }

        public void HideRecipeTooltip(bool force = false)
        {
            WorldModalUIManager.Instance?.HideRecipeTooltip(force);
        }

        public void HideInventoryTooltip(bool force = false)
        {
            WorldModalUIManager.Instance?.HideItemTooltip(force: force);
        }

        public void ShowQuantityPopup(
            int maxQuantityValue,
            Action<int> onConfirm,
            Action onCancel = null,
            string titleOverride = null,
            string headerOverride = null,
            int initialQuantity = 1)
        {
            WorldModalUIManager.Instance?.ShowQuantityPopup(
                maxQuantityValue,
                onConfirm,
                onCancel,
                titleOverride,
                headerOverride,
                initialQuantity);
        }

        public void HideQuantityPopup(bool force = false)
        {
            WorldModalUIManager.Instance?.HideQuantityPopup(force);
        }

        public void ShowItemOptionsPopup(IReadOnlyList<ItemOptionEntry> options, bool force = false)
        {
            WorldModalUIManager.Instance?.ShowItemOptionsPopup(options, force);
        }

        public void HideItemOptionsPopup(bool force = false)
        {
            WorldModalUIManager.Instance?.HideItemOptionsPopup(force);
        }

        public InventoryItemPresentation ResolvePresentation(ItemTemplateSummaryModel item)
        {
            return itemPresentationCatalog != null ? itemPresentationCatalog.Resolve(item) : FallbackPresentation;
        }

        public InventoryItemPresentation ResolvePresentation(InventoryItemModel item)
        {
            return itemPresentationCatalog != null ? itemPresentationCatalog.Resolve(item) : FallbackPresentation;
        }

        private void BindChildEvents()
        {
            if (recipeListView != null)
            {
                recipeListView.ItemClicked -= HandleRecipeListClicked;
                recipeListView.ItemClicked += HandleRecipeListClicked;
                recipeListView.ItemHovered -= HandleRecipeListHovered;
                recipeListView.ItemHovered += HandleRecipeListHovered;
                recipeListView.ItemHoverExited -= HandleRecipeHoverExited;
                recipeListView.ItemHoverExited += HandleRecipeHoverExited;
                recipeListView.SelectedRecipeDroppedBackToList -= HandleSelectedRecipeDroppedBackToList;
                recipeListView.SelectedRecipeDroppedBackToList += HandleSelectedRecipeDroppedBackToList;
            }

            if (selectedRecipeSlotView != null)
            {
                selectedRecipeSlotView.RecipeDropped -= HandleSelectedRecipeDropped;
                selectedRecipeSlotView.RecipeDropped += HandleSelectedRecipeDropped;
                selectedRecipeSlotView.Clicked -= HandleSelectedRecipeClicked;
                selectedRecipeSlotView.Clicked += HandleSelectedRecipeClicked;
                selectedRecipeSlotView.Hovered -= HandleSelectedRecipeHovered;
                selectedRecipeSlotView.Hovered += HandleSelectedRecipeHovered;
                selectedRecipeSlotView.HoverExited -= HandleSelectedRecipeHoverExited;
                selectedRecipeSlotView.HoverExited += HandleSelectedRecipeHoverExited;
            }

            if (inputPanelView != null)
            {
                inputPanelView.InventoryItemDropped -= HandleInputInventoryItemDropped;
                inputPanelView.InventoryItemDropped += HandleInputInventoryItemDropped;
                inputPanelView.SlotClicked -= HandleInputSlotClicked;
                inputPanelView.SlotClicked += HandleInputSlotClicked;
                inputPanelView.SlotHovered -= HandleInputSlotHovered;
                inputPanelView.SlotHovered += HandleInputSlotHovered;
                inputPanelView.SlotHoverExited -= HandleInputSlotHoverExited;
                inputPanelView.SlotHoverExited += HandleInputSlotHoverExited;
            }

            if (inventoryGridView != null)
            {
                inventoryGridView.ItemClicked -= HandleInventoryItemClicked;
                inventoryGridView.ItemClicked += HandleInventoryItemClicked;
            }

            if (dropZoneView != null)
            {
                dropZoneView.PayloadDropped -= HandleDropZonePayloadDropped;
                dropZoneView.PayloadDropped += HandleDropZonePayloadDropped;
                dropZoneView.Clicked -= HandleDropZoneClicked;
                dropZoneView.Clicked += HandleDropZoneClicked;
            }

            if (closeButton != null)
            {
                closeButton.Clicked -= HandleCloseClicked;
                closeButton.Clicked += HandleCloseClicked;
            }

            if (craftButton != null)
            {
                craftButton.Clicked -= HandleCraftClicked;
                craftButton.Clicked += HandleCraftClicked;
            }

            if (pauseResumeButton != null)
            {
                pauseResumeButton.Clicked -= HandlePauseResumeClicked;
                pauseResumeButton.Clicked += HandlePauseResumeClicked;
            }

            if (cancelButton != null)
            {
                cancelButton.Clicked -= HandleCancelClicked;
                cancelButton.Clicked += HandleCancelClicked;
            }
        }

        private void UnbindChildEvents()
        {
            if (recipeListView != null)
            {
                recipeListView.ItemClicked -= HandleRecipeListClicked;
                recipeListView.ItemHovered -= HandleRecipeListHovered;
                recipeListView.ItemHoverExited -= HandleRecipeHoverExited;
                recipeListView.SelectedRecipeDroppedBackToList -= HandleSelectedRecipeDroppedBackToList;
            }

            if (selectedRecipeSlotView != null)
            {
                selectedRecipeSlotView.RecipeDropped -= HandleSelectedRecipeDropped;
                selectedRecipeSlotView.Clicked -= HandleSelectedRecipeClicked;
                selectedRecipeSlotView.Hovered -= HandleSelectedRecipeHovered;
                selectedRecipeSlotView.HoverExited -= HandleSelectedRecipeHoverExited;
            }

            if (inputPanelView != null)
            {
                inputPanelView.InventoryItemDropped -= HandleInputInventoryItemDropped;
                inputPanelView.SlotClicked -= HandleInputSlotClicked;
                inputPanelView.SlotHovered -= HandleInputSlotHovered;
                inputPanelView.SlotHoverExited -= HandleInputSlotHoverExited;
            }

            if (inventoryGridView != null)
                inventoryGridView.ItemClicked -= HandleInventoryItemClicked;

            if (dropZoneView != null)
            {
                dropZoneView.PayloadDropped -= HandleDropZonePayloadDropped;
                dropZoneView.Clicked -= HandleDropZoneClicked;
            }

            if (closeButton != null)
                closeButton.Clicked -= HandleCloseClicked;

            if (craftButton != null)
                craftButton.Clicked -= HandleCraftClicked;

            if (pauseResumeButton != null)
                pauseResumeButton.Clicked -= HandlePauseResumeClicked;

            if (cancelButton != null)
                cancelButton.Clicked -= HandleCancelClicked;
        }

        private void HandleRecipeListClicked(LearnedPillRecipeModel recipe)
        {
            ClientLog.Info($"[CraftRecipeSelect] panel-view-forward-list-click recipe={DescribeRecipe(recipe)}.");
            RecipeListClicked?.Invoke(recipe);
        }

        private void HandleRecipeListHovered(LearnedPillRecipeModel recipe)
        {
            RecipeListHovered?.Invoke(recipe);
        }

        private void HandleRecipeHoverExited()
        {
            RecipeHoverExited?.Invoke();
        }

        private void HandleSelectedRecipeDropped(LearnedPillRecipeModel recipe)
        {
            ClientLog.Info($"[CraftRecipeSelect] panel-view-forward-selected-drop recipe={DescribeRecipe(recipe)}.");
            SelectedRecipeDropped?.Invoke(recipe);
        }

        private void HandleSelectedRecipeClicked()
        {
            SelectedRecipeClicked?.Invoke();
        }

        private void HandleSelectedRecipeHovered()
        {
            SelectedRecipeHovered?.Invoke();
        }

        private void HandleSelectedRecipeHoverExited()
        {
            SelectedRecipeHoverExited?.Invoke();
        }

        private void HandleSelectedRecipeDroppedBackToList()
        {
            SelectedRecipeDroppedBackToList?.Invoke();
        }

        private void HandleInputInventoryItemDropped(int inputId, InventoryItemModel item)
        {
            InputInventoryItemDropped?.Invoke(inputId, item);
        }

        private void HandleInputSlotClicked(int inputId, bool isOptional, PointerEventData.InputButton button)
        {
            InputSlotClicked?.Invoke(inputId, isOptional, button);
        }

        private void HandleInputSlotHovered(int inputId, bool isOptional)
        {
            InputSlotHovered?.Invoke(inputId, isOptional);
        }

        private void HandleInputSlotHoverExited()
        {
            InputSlotHoverExited?.Invoke();
        }

        private void HandleInventoryItemClicked(InventoryItemModel item)
        {
            InventoryItemClicked?.Invoke(item);
        }

        private void HandleDropZonePayloadDropped(UIDragPayload payload)
        {
            DropZonePayloadDropped?.Invoke(payload);
        }

        private void HandleDropZoneClicked(PointerEventData.InputButton button)
        {
            DropZoneClicked?.Invoke(button);
        }

        private void HandleCloseClicked()
        {
            CloseClicked?.Invoke();
        }

        private void HandleCraftClicked()
        {
            CraftClicked?.Invoke();
        }

        private void HandlePauseResumeClicked()
        {
            PauseResumeClicked?.Invoke();
        }

        private void HandleCancelClicked()
        {
            CancelClicked?.Invoke();
        }

        private static void ApplyText(TMP_Text text, string value, bool force)
        {
            if (text == null)
                return;

            if (!force && string.Equals(text.text, value, StringComparison.Ordinal))
                return;

            text.text = value ?? string.Empty;
        }

        private static string DescribeRecipe(LearnedPillRecipeModel recipe)
        {
            return string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "id={0} code='{1}' name='{2}' resultItem={3}",
                recipe.PillRecipeTemplateId,
                recipe.Code ?? string.Empty,
                recipe.Name ?? string.Empty,
                recipe.ResultPill.ItemTemplateId);
        }

        private void ThrowIfMissing(UnityEngine.Object value, string fieldName)
        {
            if (value == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(WorldCraftingPanelView)} on '{gameObject.name}' is missing required reference '{fieldName}'.");
            }
        }
    }
}
