using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Network.Session;
using PhamNhanOnline.Client.UI.Crafting;
using PhamNhanOnline.Client.UI.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.World
{
    public sealed class WorldCraftingPanelController : MonoBehaviour
    {
        private enum QuantityPopupMode
        {
            None = 0,
            CraftCount = 1,
            OptionalInputQuantity = 2
        }

        [Header("Recipe References")]
        [SerializeField] private TMP_Text panelTitleText;
        [SerializeField] private CraftRecipeListView recipeListView;
        [SerializeField] private CraftRecipeSlotView selectedRecipeSlotView;
        [SerializeField] private CraftRecipeTooltipView recipeTooltipView;

        [Header("Inventory References")]
        [SerializeField] private InventoryItemGridView inventoryGridView;
        [SerializeField] private InventoryItemPresentationCatalog itemPresentationCatalog;

        [Header("Ingredient References")]
        [SerializeField] private CraftIngredientPanelView ingredientPanelView;
        [SerializeField] private InventoryUseQuantityPopupView quantityPopupView;

        [Header("Recipe Detail Text")]
        [SerializeField] private TMP_Text practiceStatusText;
        [SerializeField] private CraftResultPreviewView craftingResultPreviewView;
        [SerializeField] private TMP_Text countdownText;

        [Header("Practice Controls")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button craftButton;
        [SerializeField] private TMP_Text craftButtonText;
        [SerializeField] private Button pauseResumeButton;
        [SerializeField] private TMP_Text pauseResumeButtonText;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TMP_Text cancelButtonText;
        
        

        [Header("Behavior")]
        [SerializeField] private bool autoLoadOnEnable = true;
        [SerializeField] private bool clearDraftWhenClosedWithoutPractice = true;
        [SerializeField] private bool detachFromMainMenuOnAwake = true;
        [SerializeField] private bool hideOnAwake = true;
        [SerializeField] private KeyCode closeKey = KeyCode.Escape;

        [Header("Display Text")]
        [SerializeField] private string alchemyPanelTitle = "Luyen dan that";
        [SerializeField] private string smithingPanelTitle = "Luyen khi that";
        [SerializeField] private string talismanPanelTitle = "Luyen phu that";
        [SerializeField] [TextArea] private string smithingPlaceholderText = "Luyen khi se duoc bo sung sau.";
        [SerializeField] [TextArea] private string talismanPlaceholderText = "Luyen phu se duoc bo sung sau.";
        [SerializeField] private string craftIdleText = "Luyen che";
        [SerializeField] private string pauseIdleText = "Tam dung";
        [SerializeField] private string resumeIdleText = "Tiep tuc";
        [SerializeField] private string cancelIdleText = "Huy bo";
        [SerializeField] [Range(1, 6)] private int maxRequiredIngredientSlots = 6;

        private readonly AlchemyCraftDraftState draftState = new AlchemyCraftDraftState();
        private bool isInitialized;
        private int? selectedRecipeId;
        private float liveSessionAnchorTime;
        private long liveSessionRemainingSeconds;
        private string lastSnapshot = string.Empty;
        private bool craftActionInFlight;
        private bool sessionActionInFlight;
        private long lastDisplayPracticeSessionId;
        private QuantityPopupMode quantityPopupMode;
        private int? quantityPopupInputId;
        private CraftingStationType currentStationType = CraftingStationType.Alchemy;
        private string currentStationTitleOverride;

        public bool IsPanelVisible => gameObject.activeSelf;

        public void ConfigureContext(CraftingPanelContext context)
        {
            var resolvedTitleOverride = string.IsNullOrWhiteSpace(context.TitleOverride) ? null : context.TitleOverride.Trim();
            if (currentStationType == context.StationType &&
                string.Equals(currentStationTitleOverride, resolvedTitleOverride, StringComparison.Ordinal))
            {
                return;
            }

            currentStationType = context.StationType;
            currentStationTitleOverride = resolvedTitleOverride;
            lastSnapshot = string.Empty;
            HideQuantityPopup(force: true);
            HideRecipeTooltip(force: true);
            HideInventoryTooltip(force: true);

            if (gameObject.activeSelf)
                Refresh(force: true);
        }

        private void Awake()
        {
            EnsureInitialized(hideAfterInitialize: hideOnAwake);
        }

        private void Start()
        {
            ValidateSerializedReferences();
        }

        private void OnEnable()
        {
            Refresh(force: true);
            if (autoLoadOnEnable && IsAlchemyStation())
                _ = ReloadAllAsync(forceInventoryRefresh: false);
        }

        private void Update()
        {
            if (gameObject.activeSelf && Input.GetKeyDown(closeKey))
            {
                HidePanel();
                return;
            }

            Refresh(force: false);
        }

        private void OnDisable()
        {
            HideQuantityPopup(force: true);
            if (IsAlchemyStation() && clearDraftWhenClosedWithoutPractice && !HasBlockingAlchemySession())
                ResetToIdleDraftState();
            else
            {
                HideRecipeTooltip(force: true);
                HideInventoryTooltip(force: true);
            }
        }

        private void OnDestroy()
        {
            if (recipeListView != null)
            {
                recipeListView.ItemClicked -= HandleRecipeListClicked;
                recipeListView.ItemHovered -= HandleRecipeListHovered;
                recipeListView.ItemHoverExited -= HandleRecipeHoverExited;
                recipeListView.SelectedRecipeDroppedBackToList -= HandleRecipeDroppedBackToList;
            }

            if (selectedRecipeSlotView != null)
            {
                selectedRecipeSlotView.RecipeDropped -= HandleSelectedRecipeDropped;
                selectedRecipeSlotView.Clicked -= HandleSelectedRecipeClicked;
                selectedRecipeSlotView.Hovered -= HandleSelectedRecipeHovered;
                selectedRecipeSlotView.HoverExited -= HandleRecipeHoverExited;
            }

            if (ingredientPanelView != null)
            {
                ingredientPanelView.InventoryItemDropped -= HandleIngredientInventoryItemDropped;
                ingredientPanelView.SlotClicked -= HandleIngredientSlotClicked;
            }

            if (craftButton != null)
                craftButton.onClick.RemoveListener(HandleCraftButtonClicked);
            if (closeButton != null)
                closeButton.onClick.RemoveListener(HandleCloseButtonClicked);
            if (pauseResumeButton != null)
                pauseResumeButton.onClick.RemoveListener(HandlePauseResumeButtonClicked);
            if (cancelButton != null)
                cancelButton.onClick.RemoveListener(HandleCancelButtonClicked);
        }

        public void ShowPanel()
        {
            EnsureInitialized(hideAfterInitialize: false);
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                return;
            }

            Refresh(force: true);
        }

        public void HidePanel()
        {
            EnsureInitialized(hideAfterInitialize: false);
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        private void EnsureInitialized(bool hideAfterInitialize)
        {
            if (isInitialized)
                return;

            if (detachFromMainMenuOnAwake)
                DetachFromMainMenuRoot();

            if (recipeListView != null)
            {
                recipeListView.ItemClicked += HandleRecipeListClicked;
                recipeListView.ItemHovered += HandleRecipeListHovered;
                recipeListView.ItemHoverExited += HandleRecipeHoverExited;
                recipeListView.SelectedRecipeDroppedBackToList += HandleRecipeDroppedBackToList;
            }

            if (selectedRecipeSlotView != null)
            {
                selectedRecipeSlotView.RecipeDropped += HandleSelectedRecipeDropped;
                selectedRecipeSlotView.Clicked += HandleSelectedRecipeClicked;
                selectedRecipeSlotView.Hovered += HandleSelectedRecipeHovered;
                selectedRecipeSlotView.HoverExited += HandleRecipeHoverExited;
            }

            if (ingredientPanelView != null)
            {
                ingredientPanelView.InventoryItemDropped += HandleIngredientInventoryItemDropped;
                ingredientPanelView.SlotClicked += HandleIngredientSlotClicked;
                ingredientPanelView.Clear();
            }

            if (craftButton != null)
            {
                craftButton.onClick.RemoveListener(HandleCraftButtonClicked);
                craftButton.onClick.AddListener(HandleCraftButtonClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(HandleCloseButtonClicked);
                closeButton.onClick.AddListener(HandleCloseButtonClicked);
            }

            if (pauseResumeButton != null)
            {
                pauseResumeButton.onClick.RemoveListener(HandlePauseResumeButtonClicked);
                pauseResumeButton.onClick.AddListener(HandlePauseResumeButtonClicked);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(HandleCancelButtonClicked);
                cancelButton.onClick.AddListener(HandleCancelButtonClicked);
            }

            isInitialized = true;

            if (hideAfterInitialize)
                gameObject.SetActive(false);
        }

        private void Refresh(bool force)
        {
            if (!IsAlchemyStation())
            {
                var unsupportedSnapshot = BuildUnsupportedSnapshot();
                if (!force && string.Equals(lastSnapshot, unsupportedSnapshot, StringComparison.Ordinal))
                    return;

                lastSnapshot = unsupportedSnapshot;
                ApplyUnsupportedStationState(force: true);
                return;
            }

            if (!ClientRuntime.IsInitialized)
            {
                ApplyMissingState(force);
                return;
            }

            AlignSelectionWithPracticeState();

            var snapshot = BuildSnapshot();
            if (!force && string.Equals(lastSnapshot, snapshot, StringComparison.Ordinal))
                return;

            lastSnapshot = snapshot;
            ApplyLoadedState(force: true);
        }

        private async Task ReloadAllAsync(bool forceInventoryRefresh)
        {
            if (!IsAlchemyStation())
                return;

            if (!ClientRuntime.IsInitialized || ClientRuntime.Connection.State != ClientConnectionState.Connected)
                return;

            try
            {
                if (!ClientRuntime.Inventory.HasLoadedInventory || forceInventoryRefresh)
                    await ClientRuntime.InventoryService.LoadInventoryAsync(forceInventoryRefresh);

                await ClientRuntime.AlchemyService.LoadLearnedRecipesAsync(forceRefresh: true);
                await ClientRuntime.AlchemyService.LoadPracticeStatusAsync();
                if (selectedRecipeId.HasValue)
                    await EnsureRecipeDetailLoadedAsync(selectedRecipeId.Value, forceRefresh: false);
            }
            catch (Exception ex)
            {
                ClientLog.Warn($"WorldCraftingPanelController reload exception: {ex.Message}");
            }
            finally
            {
                Refresh(force: true);
            }
        }

        private void ApplyMissingState(bool force)
        {
            ApplyPanelTitle(force);
            ApplyText(practiceStatusText, "San sang luyen che", force);
            ApplyText(countdownText, "--:--", force);
            if (craftingResultPreviewView != null)
                craftingResultPreviewView.Clear();

            if (recipeListView != null)
                recipeListView.Clear(force: true);
            if (selectedRecipeSlotView != null)
                selectedRecipeSlotView.Clear();
            if (inventoryGridView != null)
                inventoryGridView.Clear(force: true);
            ClearIngredientViews();
            ApplyButtons(false, false, false, false, null);
        }

        private void ApplyUnsupportedStationState(bool force)
        {
            ApplyPanelTitle(force);
            ApplyText(practiceStatusText, ResolveUnsupportedPracticeStatusText(), force);
            ApplyText(countdownText, "--:--", force);
            if (craftingResultPreviewView != null)
                craftingResultPreviewView.Clear();

            if (recipeListView != null)
                recipeListView.Clear(force: true);
            if (selectedRecipeSlotView != null)
                selectedRecipeSlotView.Clear();
            if (inventoryGridView != null)
                inventoryGridView.Clear(force: true);

            ClearIngredientViews();
            HideRecipeTooltip(force: true);
            HideInventoryTooltip(force: true);
            ApplyButtons(false, false, false, false, null);

            if (craftButton != null)
                craftButton.gameObject.SetActive(false);
            if (pauseResumeButton != null)
                pauseResumeButton.gameObject.SetActive(false);
            if (cancelButton != null)
                cancelButton.gameObject.SetActive(false);
            if (countdownText != null)
                countdownText.gameObject.SetActive(false);
        }

        private void ApplyLoadedState(bool force)
        {
            ApplyPanelTitle(force);
            var recipes = ClientRuntime.Alchemy.Recipes ?? Array.Empty<LearnedPillRecipeModel>();
            var selectedDetail = TryGetSelectedRecipeDetail(out var detail) ? detail : (PillRecipeDetailModel?)null;
            var displaySession = GetDisplayAlchemySession();
            var preview = selectedRecipeId.HasValue && ClientRuntime.Alchemy.LastPreview.HasValue &&
                          ClientRuntime.Alchemy.LastPreview.Value.PillRecipeTemplateId == selectedRecipeId.Value
                ? ClientRuntime.Alchemy.LastPreview
                : null;

            if (recipeListView != null)
                recipeListView.SetItems(recipes, selectedRecipeId, itemPresentationCatalog, force: true);

            ApplyInventory(force);
            ApplySelectedRecipe(selectedDetail, displaySession, preview, force);
            ApplyIngredients(selectedDetail, displaySession, force);
            ApplyCraftingResultPreview(selectedDetail, displaySession, preview);
            ApplyPracticeStatus(displaySession, preview, force);
            ApplyButtonsFromState(selectedDetail, displaySession, preview);
        }

        private void ApplyPanelTitle(bool force)
        {
            ApplyText(panelTitleText, ResolvePanelTitle(), force);
        }

        private void ApplyInventory(bool force)
        {
            if (inventoryGridView == null)
                return;

            var inventoryState = ClientRuntime.Inventory;
            var items = inventoryState.Items ?? Array.Empty<InventoryItemModel>();
            var projected = BuildProjectedInventoryItems(items);
            var ordered = projected
                .Where(static x => !x.IsEquipped)
                .Where(static x => x.Quantity > 0)
                .OrderBy(static x => x.ItemType)
                .ThenBy(static x => x.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static x => x.PlayerItemId)
                .ToArray();

            inventoryGridView.SetItems(ordered, itemPresentationCatalog, force: true);
        }

        private IReadOnlyList<InventoryItemModel> BuildProjectedInventoryItems(IReadOnlyList<InventoryItemModel> items)
        {
            if (items == null || items.Count == 0)
                return items ?? Array.Empty<InventoryItemModel>();

            // Once a practice session is active/paused/pending, inventory has already been
            // authoritatively consumed on the server. Do not subtract the local draft again.
            if (HasBlockingAlchemySession())
                return items;

            if (draftState.IsEmpty || !TryGetSelectedRecipeDetail(out var detail) || detail.Inputs == null)
                return items ?? Array.Empty<InventoryItemModel>();

            var reservedStackQuantitiesByTemplateId = new Dictionary<int, int>();
            var reservedNonStackableIds = new HashSet<long>();
            for (var i = 0; i < detail.Inputs.Count; i++)
            {
                var input = detail.Inputs[i];
                if (!draftState.TryGetSelection(input.InputId, out var selection) || !selection.Armed)
                    continue;

                if (input.RequiredItem.IsStackable)
                {
                    var reservedQuantity = input.IsOptional
                        ? Math.Max(0, selection.AssignedQuantity)
                        : Math.Max(0, input.RequiredQuantity);
                    if (reservedQuantity <= 0)
                        continue;

                    reservedStackQuantitiesByTemplateId[input.RequiredItem.ItemTemplateId] =
                        reservedStackQuantitiesByTemplateId.TryGetValue(input.RequiredItem.ItemTemplateId, out var existing)
                            ? existing + reservedQuantity
                            : reservedQuantity;
                    continue;
                }

                for (var selectedIndex = 0; selectedIndex < selection.SelectedPlayerItemIds.Count; selectedIndex++)
                    reservedNonStackableIds.Add(selection.SelectedPlayerItemIds[selectedIndex]);
            }

            if (reservedStackQuantitiesByTemplateId.Count == 0 && reservedNonStackableIds.Count == 0)
                return items;

            var projected = new List<InventoryItemModel>(items.Count);
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (!item.IsEquipped && reservedNonStackableIds.Contains(item.PlayerItemId))
                    continue;

                if (!item.IsEquipped &&
                    item.Quantity > 0 &&
                    reservedStackQuantitiesByTemplateId.TryGetValue(item.ItemTemplateId, out var reservedStackQuantity) &&
                    reservedStackQuantity > 0)
                {
                    var reduction = Math.Min(item.Quantity, reservedStackQuantity);
                    item.Quantity = Math.Max(0, item.Quantity - reduction);
                    reservedStackQuantitiesByTemplateId[item.ItemTemplateId] = Math.Max(0, reservedStackQuantity - reduction);
                }

                projected.Add(item);
            }

            return projected;
        }

        private void ApplySelectedRecipe(
            PillRecipeDetailModel? detail,
            PracticeSessionModel? activeSession,
            AlchemyCraftPreviewModel? preview,
            bool force)
        {
            if (!detail.HasValue)
            {
                if (selectedRecipeSlotView != null)
                    selectedRecipeSlotView.Clear();
                return;
            }

            var learnedRecipe = ResolveLearnedRecipe(detail.Value.PillRecipeTemplateId, detail.Value);
            if (selectedRecipeSlotView != null)
            {
                var presentation = itemPresentationCatalog != null
                    ? itemPresentationCatalog.Resolve(learnedRecipe.ResultPill)
                    : new InventoryItemPresentation(null, null, Color.white);
                selectedRecipeSlotView.SetRecipe(
                    learnedRecipe,
                    presentation,
                    ResolveDurationText(detail.Value, activeSession, preview),
                    ResolveSuccessRateText(detail.Value, preview));
                selectedRecipeSlotView.SetInteractionLocked(activeSession.HasValue);
            }
        }

        private void ApplyIngredients(PillRecipeDetailModel? detail, PracticeSessionModel? activeSession, bool force)
        {
            var inputs = detail.HasValue && detail.Value.Inputs != null
                ? detail.Value.Inputs
                : null;
            if (inputs == null || inputs.Count == 0)
            {
                ClearIngredientViews();
                return;
            }

            var requiredInputs = inputs.Where(static input => !input.IsOptional).ToArray();
            var optionalInputs = inputs.Where(static input => input.IsOptional).ToArray();

            if (ingredientPanelView != null)
            {
                ingredientPanelView.SetSlots(
                    BuildRequiredSlotStates(requiredInputs, activeSession),
                    BuildOptionalSlotStates(optionalInputs, activeSession));
            }
        }

        private void ApplyPracticeStatus(PracticeSessionModel? displaySession, AlchemyCraftPreviewModel? preview, bool force)
        {
            if (!displaySession.HasValue)
            {
                liveSessionAnchorTime = 0f;
                liveSessionRemainingSeconds = 0L;
                ApplyText(practiceStatusText, "San sang luyen che", force);
                ApplyText(countdownText, "--:--", force);
                return;
            }

            if (displaySession.Value.PracticeState == 3)
            {
                liveSessionAnchorTime = 0f;
                liveSessionRemainingSeconds = 0L;
                ApplyText(practiceStatusText, "Dang doi ket qua luyen che", force);
                ApplyText(countdownText, FormatDuration(0L), force);
                return;
            }

            if (displaySession.Value.PracticeSessionId != 0 && (liveSessionRemainingSeconds <= 0L || liveSessionAnchorTime <= 0f))
            {
                liveSessionRemainingSeconds = Math.Max(0L, displaySession.Value.RemainingDurationSeconds);
                liveSessionAnchorTime = Time.unscaledTime;
            }

            var progress = ResolveSessionProgress(displaySession.Value, out var remainingSeconds);

            ApplyText(
                practiceStatusText,
                displaySession.Value.IsPaused
                    ? "Dang tam dung"
                    : "Dang luyen che",
                force);
            ApplyText(countdownText, FormatDuration(remainingSeconds), force);
        }

        private void ApplyCraftingResultPreview(
            PillRecipeDetailModel? detail,
            PracticeSessionModel? displaySession,
            AlchemyCraftPreviewModel? preview)
        {
            if (craftingResultPreviewView == null)
                return;

            if (!detail.HasValue || !HasCraftingResultPreviewData(displaySession, preview))
            {
                craftingResultPreviewView.Clear();
                return;
            }

            var resultItem = ResolveCraftingResultItem(detail.Value, displaySession);
            var presentation = itemPresentationCatalog != null
                ? itemPresentationCatalog.Resolve(resultItem)
                : new InventoryItemPresentation(null, null, Color.white);
            craftingResultPreviewView.SetState(
                presentation,
                ResolveCraftingResultQuantity(displaySession, preview),
                ResolveCraftingResultHiddenFillAmount(displaySession),
                ResolveCraftingResultProgressText(displaySession));
        }

        private void ApplyButtonsFromState(
            PillRecipeDetailModel? selectedDetail,
            PracticeSessionModel? displaySession,
            AlchemyCraftPreviewModel? preview)
        {
            if (displaySession.HasValue)
            {
                var showPracticeButtons = displaySession.Value.PracticeState == 1 || displaySession.Value.PracticeState == 2;
                ApplyButtons(
                    craftInteractable: false,
                    pauseResumeInteractable: showPracticeButtons &&
                                             !sessionActionInFlight &&
                                             (displaySession.Value.IsPaused || displaySession.Value.CanPause),
                    cancelInteractable: showPracticeButtons && !sessionActionInFlight && displaySession.Value.CanCancel,
                    showPracticeButtons: showPracticeButtons,
                    pauseButtonLabel: displaySession.Value.IsPaused ? resumeIdleText : pauseIdleText);
                return;
            }

            var canCraft = selectedDetail.HasValue &&
                           AreRequiredInputsReady(selectedDetail.Value, displaySession) &&
                           preview.HasValue &&
                           preview.Value.PillRecipeTemplateId == selectedDetail.Value.PillRecipeTemplateId &&
                           preview.Value.CanCraft &&
                           preview.Value.MaxCraftableCount > 0 &&
                           !craftActionInFlight;
            ApplyButtons(
                craftInteractable: canCraft,
                pauseResumeInteractable: false,
                cancelInteractable: false,
                showPracticeButtons: false,
                pauseButtonLabel: null);
        }

        private void ApplyButtons(
            bool craftInteractable,
            bool pauseResumeInteractable,
            bool cancelInteractable,
            bool showPracticeButtons,
            string pauseButtonLabel)
        {
            if (craftButton != null)
            {
                craftButton.gameObject.SetActive(!showPracticeButtons && (craftInteractable || craftActionInFlight));
                craftButton.interactable = craftInteractable;
            }
            if (craftButtonText != null)
                craftButtonText.text = craftActionInFlight ? "Dang gui..." : craftIdleText;

            if (pauseResumeButton != null)
            {
                pauseResumeButton.gameObject.SetActive(showPracticeButtons);
                pauseResumeButton.interactable = pauseResumeInteractable;
            }

            if (pauseResumeButtonText != null)
                pauseResumeButtonText.text = sessionActionInFlight ? "Dang gui..." : (pauseButtonLabel ?? pauseIdleText);

            if (cancelButton != null)
            {
                cancelButton.gameObject.SetActive(showPracticeButtons);
                cancelButton.interactable = cancelInteractable;
            }

            if (cancelButtonText != null)
                cancelButtonText.text = sessionActionInFlight ? "Dang gui..." : cancelIdleText;

            if (countdownText != null)
                countdownText.gameObject.SetActive(showPracticeButtons);
        }

        private void HandleRecipeListClicked(LearnedPillRecipeModel recipe)
        {
            if (HasBlockingAlchemySession())
                return;

            SetSelectedRecipe(recipe.PillRecipeTemplateId);
        }

        private void HandleRecipeListHovered(LearnedPillRecipeModel recipe)
        {
            _ = ShowRecipeTooltipAsync(recipe.PillRecipeTemplateId);
        }

        private void HandleSelectedRecipeDropped(LearnedPillRecipeModel recipe)
        {
            if (HasBlockingAlchemySession())
                return;

            SetSelectedRecipe(recipe.PillRecipeTemplateId);
        }

        private void HandleSelectedRecipeClicked()
        {
            if (selectedRecipeId.HasValue)
                _ = ShowRecipeTooltipAsync(selectedRecipeId.Value);
        }

        private void HandleSelectedRecipeHovered()
        {
            if (selectedRecipeId.HasValue)
                _ = ShowRecipeTooltipAsync(selectedRecipeId.Value);
        }

        private void HandleRecipeDroppedBackToList()
        {
            if (HasBlockingAlchemySession())
                return;

            ClearDraft();
            Refresh(force: true);
        }

        private void HandleRecipeHoverExited()
        {
            HideRecipeTooltip(force: true);
        }

        private void HandleIngredientInventoryItemDropped(int inputId, InventoryItemModel item)
        {
            if (HasBlockingAlchemySession() || !TryGetSelectedRecipeDetail(out var detail) || detail.Inputs == null)
                return;

            if (!TryAssignInventoryItemToInput(detail.Inputs, inputId, item))
                return;

            _ = RefreshPreviewAsync();
            Refresh(force: true);
        }

        private void HandleIngredientSlotClicked(int inputId, bool isOptional, PointerEventData.InputButton button)
        {
            if (button == PointerEventData.InputButton.Right)
            {
                if (draftState.ClearInput(inputId))
                {
                    HideQuantityPopup(force: true);
                    _ = RefreshPreviewAsync();
                    Refresh(force: true);
                }

                return;
            }

            if (isOptional &&
                TryGetSelectedRecipeDetail(out var detail) &&
                detail.Inputs != null)
            {
                var optionalInput = detail.Inputs.FirstOrDefault(input => input.InputId == inputId);
                if (optionalInput.InputId > 0 &&
                    optionalInput.RequiredItem.IsStackable &&
                    draftState.TryGetSelection(inputId, out var selection) &&
                    selection.Armed)
                {
                    ShowOptionalInputQuantityPopup(optionalInput, selection);
                    return;
                }
            }

            if (selectedRecipeId.HasValue)
                _ = ShowRecipeTooltipAsync(selectedRecipeId.Value);
        }

        private void HandleCraftButtonClicked()
        {
            if (!IsAlchemyStation())
                return;

            if (craftActionInFlight || !selectedRecipeId.HasValue || !TryGetSelectedRecipeDetail(out var detail))
                return;

            var preview = ClientRuntime.Alchemy.LastPreview;
            if (!preview.HasValue ||
                !AreRequiredInputsReady(detail, GetActiveAlchemySession()) ||
                preview.Value.PillRecipeTemplateId != detail.PillRecipeTemplateId ||
                !preview.Value.CanCraft)
            {
                Refresh(force: true);
                return;
            }

            var maxCraftableCount = Mathf.Max(1, preview.Value.MaxCraftableCount);
            if (maxCraftableCount <= 1)
            {
                _ = StartCraftAsync(detail.PillRecipeTemplateId, 1);
                return;
            }

            ShowCraftCountPopup(detail, preview.Value, maxCraftableCount);
        }

        private void HandleCloseButtonClicked()
        {
            HidePanel();
        }

        private void HandlePauseResumeButtonClicked()
        {
            if (!IsAlchemyStation())
                return;

            if (sessionActionInFlight)
                return;

            var session = GetActiveAlchemySession();
            if (!session.HasValue)
                return;

            _ = TogglePauseResumeAsync(session.Value);
        }

        private void HandleCancelButtonClicked()
        {
            if (!IsAlchemyStation())
                return;

            if (sessionActionInFlight)
                return;

            var session = GetActiveAlchemySession();
            if (!session.HasValue || !session.Value.CanCancel)
                return;

            _ = CancelPracticeAsync(session.Value.PracticeSessionId);
        }

        private async Task StartCraftAsync(int recipeId, int requestedCraftCount)
        {
            craftActionInFlight = true;
            Refresh(force: true);
            try
            {
                var result = await ClientRuntime.AlchemyService.CraftPillAsync(
                    recipeId,
                    Mathf.Max(1, requestedCraftCount),
                    BuildSelectedPlayerItemIds(),
                    BuildSelectedOptionalInputs());
                if (!result.Success)
                    ClientLog.Warn($"WorldCraftingPanelController failed to start craft: {result.Message}");
            }
            catch (Exception ex)
            {
                ClientLog.Warn($"WorldCraftingPanelController craft exception: {ex.Message}");
            }
            finally
            {
                craftActionInFlight = false;
                liveSessionAnchorTime = 0f;
                liveSessionRemainingSeconds = 0L;
                Refresh(force: true);
            }
        }

        private async Task TogglePauseResumeAsync(PracticeSessionModel session)
        {
            sessionActionInFlight = true;
            Refresh(force: true);
            try
            {
                var result = session.IsPaused
                    ? await ClientRuntime.AlchemyService.ResumePracticeAsync(session.PracticeSessionId)
                    : await ClientRuntime.AlchemyService.PausePracticeAsync(session.PracticeSessionId);
                if (!result.Success)
                    ClientLog.Warn($"WorldCraftingPanelController practice toggle failed: {result.Message}");

                liveSessionAnchorTime = 0f;
                liveSessionRemainingSeconds = 0L;
            }
            catch (Exception ex)
            {
                ClientLog.Warn($"WorldCraftingPanelController practice toggle exception: {ex.Message}");
            }
            finally
            {
                sessionActionInFlight = false;
                Refresh(force: true);
            }
        }

        private async Task CancelPracticeAsync(long practiceSessionId)
        {
            sessionActionInFlight = true;
            Refresh(force: true);
            try
            {
                var result = await ClientRuntime.AlchemyService.CancelPracticeAsync(practiceSessionId);
                if (!result.Success)
                {
                    ClientLog.Warn($"WorldCraftingPanelController cancel practice failed: {result.Message}");
                }
                else
                {
                    ClearDraft();
                }

                liveSessionAnchorTime = 0f;
                liveSessionRemainingSeconds = 0L;
            }
            catch (Exception ex)
            {
                ClientLog.Warn($"WorldCraftingPanelController cancel practice exception: {ex.Message}");
            }
            finally
            {
                sessionActionInFlight = false;
                Refresh(force: true);
            }
        }

        private async Task ShowRecipeTooltipAsync(int recipeId)
        {
            if (recipeTooltipView == null || recipeId <= 0)
                return;

            var detail = await EnsureRecipeDetailLoadedAsync(recipeId, forceRefresh: false);
            if (!detail.HasValue)
                return;

            recipeTooltipView.Show(detail.Value, ResolveAssignedQuantityForTooltip, force: true);
        }

        private async Task<PillRecipeDetailModel?> EnsureRecipeDetailLoadedAsync(int recipeId, bool forceRefresh)
        {
            if (ClientRuntime.Alchemy.TryGetRecipeDetail(recipeId, out var cached) && !forceRefresh)
                return cached;

            try
            {
                var result = await ClientRuntime.AlchemyService.LoadRecipeDetailAsync(recipeId, forceRefresh);
                if (!result.Success || !result.Recipe.HasValue)
                    return null;

                return result.Recipe.Value;
            }
            catch (Exception ex)
            {
                ClientLog.Warn($"WorldCraftingPanelController load detail exception: {ex.Message}");
                return null;
            }
        }

        private async Task RefreshPreviewAsync()
        {
            if (!ClientRuntime.IsInitialized || !selectedRecipeId.HasValue || HasBlockingAlchemySession() || !TryGetSelectedRecipeDetail(out var detail))
                return;

            try
            {
                await ClientRuntime.AlchemyService.PreviewCraftAsync(
                    selectedRecipeId.Value,
                    ResolvePreviewRequestedCraftCount(detail),
                    BuildSelectedPlayerItemIds(),
                    BuildSelectedOptionalInputs());
            }
            catch (Exception ex)
            {
                ClientLog.Warn($"WorldCraftingPanelController preview exception: {ex.Message}");
            }
            finally
            {
                Refresh(force: true);
            }
        }

        private int ResolvePreviewRequestedCraftCount(PillRecipeDetailModel detail)
        {
            return draftState.ResolvePreviewRequestedCraftCount(detail, GetInventoryItems());
        }

        private void SetSelectedRecipe(int recipeId)
        {
            if (selectedRecipeId == recipeId)
                return;

            selectedRecipeId = recipeId;
            draftState.Clear();
            _ = EnsureRecipeDetailLoadedAsync(recipeId, forceRefresh: false);
            _ = RefreshPreviewAsync();
            Refresh(force: true);
        }

        private void AlignSelectionWithPracticeState()
        {
            var session = GetDisplayAlchemySession();
            if (!session.HasValue)
            {
                if (lastDisplayPracticeSessionId != 0L)
                {
                    lastDisplayPracticeSessionId = 0L;
                    ResetToIdleDraftState();
                }
                return;
            }

            lastDisplayPracticeSessionId = session.Value.PracticeSessionId;

            if (selectedRecipeId == session.Value.DefinitionId)
                return;

            selectedRecipeId = session.Value.DefinitionId;
            draftState.Clear();
            _ = EnsureRecipeDetailLoadedAsync(session.Value.DefinitionId, forceRefresh: false);
        }

        private bool TryGetSelectedRecipeDetail(out PillRecipeDetailModel detail)
        {
            if (selectedRecipeId.HasValue && ClientRuntime.Alchemy.TryGetRecipeDetail(selectedRecipeId.Value, out detail))
                return true;

            detail = default;
            return false;
        }

        private PracticeSessionModel? GetDisplayAlchemySession()
        {
            var session = ClientRuntime.Alchemy.CurrentPracticeSession;
            if (!session.HasValue)
                return null;

            if (session.Value.PracticeType != 2)
                return null;

            if (session.Value.PracticeState == 1 || session.Value.PracticeState == 2 || session.Value.PracticeState == 3)
                return session;

            return null;
        }

        private PracticeSessionModel? GetActiveAlchemySession()
        {
            var session = GetDisplayAlchemySession();
            if (!session.HasValue)
                return null;

            return session.Value.PracticeState == 1 || session.Value.PracticeState == 2
                ? session
                : null;
        }

        private bool HasBlockingAlchemySession()
        {
            return GetDisplayAlchemySession().HasValue;
        }

        private LearnedPillRecipeModel ResolveLearnedRecipe(int recipeId, PillRecipeDetailModel detail)
        {
            var recipes = ClientRuntime.Alchemy.Recipes;
            if (recipes != null)
            {
                for (var i = 0; i < recipes.Length; i++)
                {
                    if (recipes[i].PillRecipeTemplateId == recipeId)
                        return recipes[i];
                }
            }

            return new LearnedPillRecipeModel
            {
                PillRecipeTemplateId = detail.PillRecipeTemplateId,
                Code = detail.Code,
                Name = detail.Name,
                Description = detail.Description,
                ResultPill = detail.ResultPill,
                CraftDurationSeconds = detail.CraftDurationSeconds,
                BaseSuccessRate = detail.BaseSuccessRate,
                SuccessRateCap = detail.SuccessRateCap,
                MutationRate = detail.MutationRate,
                MutationRateCap = detail.MutationRateCap,
                TotalCraftCount = detail.TotalCraftCount,
                CurrentSuccessRateBonus = detail.CurrentSuccessRateBonus,
                LearnedUnixMs = detail.LearnedUnixMs
            };
        }

        private int ResolveAssignedQuantity(PillRecipeInputModel input, PracticeSessionModel? activeSession)
        {
            return draftState.ResolveAssignedQuantity(input, activeSession, GetConsumedItems(), GetInventoryItems());
        }

        private bool ResolveInputArmed(PillRecipeInputModel input, PracticeSessionModel? activeSession)
        {
            return draftState.ResolveInputArmed(input, activeSession, GetConsumedItems());
        }

        private int ResolveInventoryQuantity(int itemTemplateId)
        {
            var items = GetInventoryItems();
            var total = 0;
            for (var i = 0; i < items.Length; i++)
            {
                if (items[i].IsEquipped || items[i].ItemTemplateId != itemTemplateId)
                    continue;

                total += Math.Max(0, items[i].Quantity);
            }

            return total;
        }

        private InventoryItemModel[] GetInventoryItems()
        {
            return ClientRuntime.IsInitialized
                ? (ClientRuntime.Inventory.Items ?? Array.Empty<InventoryItemModel>())
                : Array.Empty<InventoryItemModel>();
        }

        private IReadOnlyList<AlchemyConsumedItemModel> GetConsumedItems()
        {
            var status = ClientRuntime.IsInitialized
                ? ClientRuntime.Alchemy.LastPracticeStatus
                : null;
            return status.HasValue && status.Value.ConsumedItems != null
                ? status.Value.ConsumedItems
                : Array.Empty<AlchemyConsumedItemModel>();
        }

        private static bool TryResolveInput(
            IReadOnlyList<PillRecipeInputModel> inputs,
            int inputId,
            out PillRecipeInputModel input)
        {
            if (inputs != null)
            {
                for (var i = 0; i < inputs.Count; i++)
                {
                    if (inputs[i].InputId != inputId)
                        continue;

                    input = inputs[i];
                    return true;
                }
            }

            input = default;
            return false;
        }

        private IReadOnlyList<CraftIngredientPanelView.SlotState> BuildRequiredSlotStates(
            IReadOnlyList<PillRecipeInputModel> requiredInputs,
            PracticeSessionModel? activeSession)
        {
            if (requiredInputs == null || requiredInputs.Count == 0)
                return Array.Empty<CraftIngredientPanelView.SlotState>();

            var slotCount = Math.Min(Math.Max(1, maxRequiredIngredientSlots), requiredInputs.Count);
            if (requiredInputs.Count > slotCount)
                ClientLog.Error($"WorldCraftingPanelController recipe {selectedRecipeId} requires {requiredInputs.Count} mandatory inputs but UI supports only {slotCount}.");

            var states = new List<CraftIngredientPanelView.SlotState>(slotCount);
            for (var i = 0; i < slotCount; i++)
            {
                var input = requiredInputs[i];
                var presentation = itemPresentationCatalog != null
                    ? itemPresentationCatalog.Resolve(input.RequiredItem)
                    : new InventoryItemPresentation(null, null, Color.white);
                var currentQuantity = ResolveAssignedQuantity(input, activeSession);
                var hasSelection = ResolveInputArmed(input, activeSession);
                states.Add(new CraftIngredientPanelView.SlotState(
                    input.InputId,
                    presentation,
                    currentQuantity,
                    Math.Max(1, input.RequiredQuantity),
                    hasSelection,
                    activeSession.HasValue,
                    showEmptyIcon: true));
            }

            return states;
        }

        private bool TryAssignInventoryItemToInput(IReadOnlyList<PillRecipeInputModel> inputs, int inputId, InventoryItemModel item)
        {
            var result = draftState.TryAssignInventoryItemToInput(inputs, inputId, item);
            if (!result.Success)
                return false;

            if (result.RequiresQuantityPrompt &&
                TryResolveInput(inputs, inputId, out var input) &&
                draftState.TryGetSelection(inputId, out var selection))
            {
                ShowOptionalInputQuantityPopup(input, selection);
            }

            return true;
        }

        private bool AreRequiredInputsReady(PillRecipeDetailModel detail, PracticeSessionModel? activeSession)
        {
            return draftState.AreRequiredInputsReady(detail, activeSession, GetConsumedItems(), GetInventoryItems());
        }

        private string BuildIngredientSummary(IReadOnlyList<PillRecipeInputModel> inputs, PracticeSessionModel? activeSession)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < inputs.Count; i++)
            {
                var input = inputs[i];
                if (builder.Length > 0)
                    builder.AppendLine();

                var currentQuantity = ResolveAssignedQuantity(input, activeSession);
                builder.Append("* ");
                builder.Append(string.IsNullOrWhiteSpace(input.RequiredItem.Name) ? "Nguyen lieu" : input.RequiredItem.Name.Trim());
                builder.Append(' ');
                builder.Append(currentQuantity.ToString(CultureInfo.InvariantCulture));
                builder.Append('/');
                builder.Append(Math.Max(1, input.RequiredQuantity).ToString(CultureInfo.InvariantCulture));
                if (input.IsOptional)
                    builder.Append(" (tuy chon)");
            }

            var preview = ClientRuntime.Alchemy.LastPreview;
            if (!activeSession.HasValue &&
                selectedRecipeId.HasValue &&
                preview.HasValue &&
                preview.Value.PillRecipeTemplateId == selectedRecipeId.Value &&
                preview.Value.MaxCraftableCount > 0)
            {
                builder.AppendLine();
                builder.Append("* Toi da ");
                builder.Append(preview.Value.MaxCraftableCount.ToString(CultureInfo.InvariantCulture));
                builder.Append(" vien");
            }

            return builder.ToString();
        }

        private long[] BuildSelectedPlayerItemIds()
        {
            return draftState.BuildSelectedPlayerItemIds();
        }

        private AlchemyOptionalInputSelectionModel[] BuildSelectedOptionalInputs()
        {
            if (!TryGetSelectedRecipeDetail(out var detail) || detail.Inputs == null)
                return Array.Empty<AlchemyOptionalInputSelectionModel>();

            return draftState.BuildSelectedOptionalInputs(detail);
        }

        private int ResolveAssignedQuantityForTooltip(PillRecipeInputModel input)
        {
            return ResolveAssignedQuantity(input, GetDisplayAlchemySession());
        }

        private int ResolveOptionalApplicationCount(PillRecipeInputModel input)
        {
            return draftState.ResolveOptionalApplicationCount(input);
        }

        private long ResolveLiveRemainingSeconds(PracticeSessionModel session)
        {
            if (session.IsPaused)
                return Math.Max(0L, session.RemainingDurationSeconds);

            if (liveSessionAnchorTime <= 0f)
            {
                liveSessionAnchorTime = Time.unscaledTime;
                liveSessionRemainingSeconds = Math.Max(0L, session.RemainingDurationSeconds);
            }

            var elapsed = Math.Max(0f, Time.unscaledTime - liveSessionAnchorTime);
            return Math.Max(0L, liveSessionRemainingSeconds - (long)Math.Floor(elapsed));
        }

        private float ResolveSessionProgress(PracticeSessionModel session, out long remainingSeconds)
        {
            remainingSeconds = ResolveLiveRemainingSeconds(session);
            var totalDuration = Math.Max(1L, session.TotalDurationSeconds);
            return Mathf.Clamp01((float)(totalDuration - remainingSeconds) / totalDuration);
        }

        private float ResolveCraftingResultHiddenFillAmount(PracticeSessionModel? displaySession)
        {
            if (!displaySession.HasValue)
                return 0f;

            if (displaySession.Value.PracticeState == 3)
                return 0f;

            return 1f - ResolveSessionProgress(displaySession.Value, out _);
        }

        private bool HasCraftingResultPreviewData(
            PracticeSessionModel? displaySession,
            AlchemyCraftPreviewModel? preview)
        {
            var pendingResult = ClientRuntime.Alchemy.PendingPracticeResult;
            if (pendingResult.HasValue && pendingResult.Value.PracticeType == 2)
                return true;

            if (displaySession.HasValue)
                return true;

            return preview.HasValue &&
                   selectedRecipeId.HasValue &&
                   preview.Value.PillRecipeTemplateId == selectedRecipeId.Value &&
                   preview.Value.MaxCraftableCount > 0;
        }

        private string ResolveCraftingResultProgressText(PracticeSessionModel? displaySession)
        {
            if (!displaySession.HasValue)
                return "0%";

            if (displaySession.Value.PracticeState == 3)
                return "100%";

            return string.Concat(
                Mathf.RoundToInt(ResolveSessionProgress(displaySession.Value, out _) * 100f)
                    .ToString(CultureInfo.InvariantCulture),
                "%");
        }

        private int ResolveCraftingResultQuantity(
            PracticeSessionModel? displaySession,
            AlchemyCraftPreviewModel? preview)
        {
            var pendingResult = ClientRuntime.Alchemy.PendingPracticeResult;
            if (pendingResult.HasValue &&
                pendingResult.Value.PracticeType == 2 &&
                (!displaySession.HasValue || pendingResult.Value.PracticeSessionId == displaySession.Value.PracticeSessionId))
            {
                if (pendingResult.Value.PrimaryReward.HasValue)
                    return Math.Max(0, pendingResult.Value.PrimaryReward.Value.Quantity);

                if (pendingResult.Value.SuccessCount > 0)
                    return Math.Max(0, pendingResult.Value.SuccessCount);

                return Math.Max(0, pendingResult.Value.RequestedCraftCount);
            }

            if (displaySession.HasValue)
                return Math.Max(0, displaySession.Value.RequestedCraftCount);

            if (preview.HasValue &&
                selectedRecipeId.HasValue &&
                preview.Value.PillRecipeTemplateId == selectedRecipeId.Value)
            {
                return Math.Max(0, preview.Value.MaxCraftableCount);
            }

            return 0;
        }

        private ItemTemplateSummaryModel ResolveCraftingResultItem(
            PillRecipeDetailModel detail,
            PracticeSessionModel? displaySession)
        {
            var pendingResult = ClientRuntime.Alchemy.PendingPracticeResult;
            if (pendingResult.HasValue &&
                pendingResult.Value.PracticeType == 2 &&
                (!displaySession.HasValue || pendingResult.Value.PracticeSessionId == displaySession.Value.PracticeSessionId))
            {
                if (pendingResult.Value.DisplayItem.HasValue)
                    return pendingResult.Value.DisplayItem.Value;

                if (pendingResult.Value.PrimaryReward.HasValue)
                    return pendingResult.Value.PrimaryReward.Value.Item;
            }

            return detail.ResultPill;
        }

        private static string ResolveSuccessRateText(PillRecipeDetailModel detail, AlchemyCraftPreviewModel? preview)
        {
            if (preview.HasValue)
                return FormatPercent(preview.Value.EffectiveSuccessRate);

            var rate = NormalizeRate(detail.BaseSuccessRate) + NormalizeRate(detail.CurrentSuccessRateBonus);
            if (detail.SuccessRateCap.HasValue)
                rate = Math.Min(rate, NormalizeRate(detail.SuccessRateCap.Value));
            return FormatPercent(rate);
        }

        private string ResolveDurationText(
            PillRecipeDetailModel detail,
            PracticeSessionModel? activeSession,
            AlchemyCraftPreviewModel? preview)
        {
            if (activeSession.HasValue)
                return FormatDuration(activeSession.Value.TotalDurationSeconds);

            return FormatDuration(detail.CraftDurationSeconds);
        }

        private IReadOnlyList<CraftIngredientPanelView.SlotState> BuildOptionalSlotStates(
            IReadOnlyList<PillRecipeInputModel> optionalInputs,
            PracticeSessionModel? activeSession)
        {
            if (optionalInputs == null || optionalInputs.Count == 0)
                return Array.Empty<CraftIngredientPanelView.SlotState>();

            var states = new List<CraftIngredientPanelView.SlotState>(optionalInputs.Count);
            for (var i = 0; i < optionalInputs.Count; i++)
            {
                var input = optionalInputs[i];
                var presentation = itemPresentationCatalog != null
                    ? itemPresentationCatalog.Resolve(input.RequiredItem)
                    : new InventoryItemPresentation(null, null, Color.white);
                var currentQuantity = ResolveAssignedQuantity(input, activeSession);
                var hasSelection = ResolveInputArmed(input, activeSession);
                states.Add(new CraftIngredientPanelView.SlotState(
                    input.InputId,
                    presentation,
                    currentQuantity,
                    Math.Max(1, input.RequiredQuantity),
                    hasSelection,
                    activeSession.HasValue,
                    showEmptyIcon: true));
            }

            return states;
        }

        private void ShowCraftCountPopup(PillRecipeDetailModel detail, AlchemyCraftPreviewModel preview, int maxCraftableCount)
        {
            if (quantityPopupView == null)
            {
                _ = StartCraftAsync(detail.PillRecipeTemplateId, maxCraftableCount);
                return;
            }

            quantityPopupMode = QuantityPopupMode.CraftCount;
            quantityPopupInputId = null;
            quantityPopupView.Show(
                Mathf.Max(1, maxCraftableCount),
                HandleQuantityPopupConfirmed,
                HandleQuantityPopupCancelled,
                string.IsNullOrWhiteSpace(detail.Name)
                    ? "Ban muon luyen che bao nhieu vien?"
                    : string.Concat("Ban muon luyen ", detail.Name.Trim(), " bao nhieu vien?"),
                initialQuantity: Mathf.Clamp(preview.RequestedCraftCount > 0 ? preview.RequestedCraftCount : 1, 1, Mathf.Max(1, maxCraftableCount)));
        }

        private void ShowOptionalInputQuantityPopup(PillRecipeInputModel input, AlchemyCraftDraftState.SelectionSnapshot selection)
        {
            if (quantityPopupView == null)
                return;

            quantityPopupMode = QuantityPopupMode.OptionalInputQuantity;
            quantityPopupInputId = input.InputId;
            var maxQuantity = Math.Max(
                Math.Max(0, selection.AssignedQuantity),
                Math.Max(0, selection.AssignedQuantity) + ResolveInventoryQuantity(input.RequiredItem.ItemTemplateId));
            quantityPopupView.Show(
                Mathf.Max(1, maxQuantity),
                HandleQuantityPopupConfirmed,
                HandleQuantityPopupCancelled,
                string.IsNullOrWhiteSpace(input.RequiredItem.Name)
                    ? "Ban muon gan bao nhieu catalyst?"
                    : string.Concat("Ban muon gan bao nhieu ", input.RequiredItem.Name.Trim(), "?"),
                initialQuantity: Mathf.Clamp(selection.AssignedQuantity > 0 ? selection.AssignedQuantity : Math.Max(1, input.RequiredQuantity), 1, Mathf.Max(1, maxQuantity)));
        }

        private void HandleQuantityPopupConfirmed(int quantity)
        {
            var mode = quantityPopupMode;
            var inputId = quantityPopupInputId;
            HideQuantityPopup(force: true);

            switch (mode)
            {
                case QuantityPopupMode.CraftCount:
                    if (selectedRecipeId.HasValue)
                        _ = StartCraftAsync(selectedRecipeId.Value, quantity);
                    break;
                case QuantityPopupMode.OptionalInputQuantity:
                    if (inputId.HasValue)
                    {
                        draftState.SetAssignedQuantity(inputId.Value, quantity);
                        _ = RefreshPreviewAsync();
                        Refresh(force: true);
                    }
                    break;
            }
        }

        private void HandleQuantityPopupCancelled()
        {
            HideQuantityPopup(force: true);
            Refresh(force: true);
        }

        private void HideQuantityPopup(bool force)
        {
            quantityPopupMode = QuantityPopupMode.None;
            quantityPopupInputId = null;
            if (quantityPopupView != null)
                quantityPopupView.Hide(force);
        }

        private void ClearIngredientViews()
        {
            if (ingredientPanelView != null)
                ingredientPanelView.Clear();
        }

        private void ClearDraft()
        {
            selectedRecipeId = null;
            draftState.Clear();
            liveSessionAnchorTime = 0f;
            liveSessionRemainingSeconds = 0L;
        }

        private void ResetToIdleDraftState()
        {
            ClearDraft();

            if (selectedRecipeSlotView != null)
                selectedRecipeSlotView.SetInteractionLocked(false);

            HideRecipeTooltip(force: true);
            HideInventoryTooltip(force: true);
            HideQuantityPopup(force: true);
        }

        private void HideRecipeTooltip(bool force)
        {
            if (recipeTooltipView != null)
                recipeTooltipView.Hide(force);
        }

        private void HideInventoryTooltip(bool force)
        {
            if (inventoryGridView != null)
                inventoryGridView.HideTooltip(force);
        }

        private static double NormalizeRate(double value)
        {
            if (value <= 0d)
                return 0d;

            return value > 1d ? value / 100d : value;
        }

        private static string FormatPercent(double value)
        {
            return string.Concat((NormalizeRate(value) * 100d).ToString("0.##", CultureInfo.InvariantCulture), "%");
        }

        private static string FormatDuration(long totalSeconds)
        {
            var clamped = Math.Max(0L, totalSeconds);
            if (clamped >= 3600L)
                return TimeSpan.FromSeconds(clamped).ToString(@"hh\:mm\:ss");

            return TimeSpan.FromSeconds(clamped).ToString(@"mm\:ss");
        }

        private static void ApplyText(TMP_Text text, string value, bool force)
        {
            if (text == null)
                return;

            if (!force && string.Equals(text.text, value, StringComparison.Ordinal))
                return;

            text.text = value ?? string.Empty;
        }

        private string BuildSnapshot()
        {
            var displaySession = GetDisplayAlchemySession();
            var builder = new StringBuilder();
            builder.Append(((int)currentStationType).ToString(CultureInfo.InvariantCulture));
            builder.Append('|');
            builder.Append(currentStationTitleOverride ?? string.Empty);
            builder.Append('|');
            builder.Append(ClientRuntime.Alchemy.HasLoadedRecipes ? "1" : "0");
            builder.Append('|');
            builder.Append(ClientRuntime.Inventory.HasLoadedInventory ? "1" : "0");
            builder.Append('|');
            builder.Append(selectedRecipeId.HasValue ? selectedRecipeId.Value.ToString(CultureInfo.InvariantCulture) : "0");
            builder.Append('|');
            builder.Append(craftActionInFlight ? "1" : "0");
            builder.Append('|');
            builder.Append(sessionActionInFlight ? "1" : "0");
            builder.Append('|');
            builder.Append(BuildSelectionSnapshot());
            builder.Append('|');
            builder.Append(BuildSessionSnapshot(displaySession));
            builder.Append('|');
            builder.Append(BuildPreviewSnapshot());
            builder.Append('|');
            builder.Append(ClientRuntime.Alchemy.LastStatusMessage ?? string.Empty);
            builder.Append('|');
            builder.Append(ClientRuntime.Inventory.LastStatusMessage ?? string.Empty);
            builder.Append('|');
            if (displaySession.HasValue && (displaySession.Value.PracticeState == 1 || displaySession.Value.PracticeState == 2))
            {
                builder.Append(ResolveLiveRemainingSeconds(displaySession.Value).ToString(CultureInfo.InvariantCulture));
                builder.Append(':');
                builder.Append(displaySession.Value.IsPaused ? "1" : "0");
            }
            return builder.ToString();
        }

        private string BuildSelectionSnapshot()
        {
            return draftState.BuildSnapshot();
        }

        private static string BuildSessionSnapshot(PracticeSessionModel? session)
        {
            if (!session.HasValue)
                return string.Empty;

            return string.Concat(
                session.Value.PracticeSessionId.ToString(CultureInfo.InvariantCulture),
                ":",
                session.Value.PracticeState.ToString(CultureInfo.InvariantCulture),
                ":",
                session.Value.DefinitionId.ToString(CultureInfo.InvariantCulture),
                ":",
                session.Value.RequestedCraftCount.ToString(CultureInfo.InvariantCulture),
                ":",
                session.Value.BoostedCraftCount.ToString(CultureInfo.InvariantCulture),
                ":",
                session.Value.RemainingDurationSeconds.ToString(CultureInfo.InvariantCulture),
                ":",
                session.Value.CanPause ? "1" : "0",
                ":",
                session.Value.CanCancel ? "1" : "0",
                ":",
                session.Value.IsPaused ? "1" : "0");
        }

        private string BuildPreviewSnapshot()
        {
            var preview = ClientRuntime.Alchemy.LastPreview;
            if (!preview.HasValue)
                return string.Empty;

            return string.Concat(
                preview.Value.PillRecipeTemplateId.ToString(CultureInfo.InvariantCulture),
                ":",
                preview.Value.CanCraft ? "1" : "0",
                ":",
                preview.Value.MaxCraftableCount.ToString(CultureInfo.InvariantCulture),
                ":",
                preview.Value.BoostedCraftCount.ToString(CultureInfo.InvariantCulture),
                ":",
                preview.Value.EffectiveSuccessRate.ToString("0.####", CultureInfo.InvariantCulture),
                ":",
                preview.Value.FailureReason ?? string.Empty);
        }

        private bool IsAlchemyStation()
        {
            return currentStationType == CraftingStationType.Alchemy;
        }

        private string BuildUnsupportedSnapshot()
        {
            return string.Concat(
                ((int)currentStationType).ToString(CultureInfo.InvariantCulture),
                "|",
                currentStationTitleOverride ?? string.Empty);
        }

        private string ResolvePanelTitle()
        {
            if (!string.IsNullOrWhiteSpace(currentStationTitleOverride))
                return currentStationTitleOverride;

            switch (currentStationType)
            {
                case CraftingStationType.Smithing:
                    return smithingPanelTitle;
                case CraftingStationType.Talisman:
                    return talismanPanelTitle;
                default:
                    return alchemyPanelTitle;
            }
        }

        private string ResolveUnsupportedPracticeStatusText()
        {
            switch (currentStationType)
            {
                case CraftingStationType.Smithing:
                    return smithingPlaceholderText;
                case CraftingStationType.Talisman:
                    return talismanPlaceholderText;
                default:
                    return "San sang luyen che";
            }
        }

        private void ValidateSerializedReferences()
        {
            ThrowIfMissing(recipeListView, nameof(recipeListView));
            ThrowIfMissing(selectedRecipeSlotView, nameof(selectedRecipeSlotView));
            ThrowIfMissing(recipeTooltipView, nameof(recipeTooltipView));
            ThrowIfMissing(inventoryGridView, nameof(inventoryGridView));
            ThrowIfMissing(itemPresentationCatalog, nameof(itemPresentationCatalog));
            ThrowIfMissing(ingredientPanelView, nameof(ingredientPanelView));
            ThrowIfMissing(quantityPopupView, nameof(quantityPopupView));
            ThrowIfMissing(practiceStatusText, nameof(practiceStatusText));
            ThrowIfMissing(closeButton, nameof(closeButton));
            ThrowIfMissing(craftButton, nameof(craftButton));
            ThrowIfMissing(craftButtonText, nameof(craftButtonText));
            ThrowIfMissing(pauseResumeButton, nameof(pauseResumeButton));
            ThrowIfMissing(pauseResumeButtonText, nameof(pauseResumeButtonText));
            ThrowIfMissing(cancelButton, nameof(cancelButton));
            ThrowIfMissing(cancelButtonText, nameof(cancelButtonText));
            ThrowIfMissing(craftingResultPreviewView, nameof(craftingResultPreviewView));
            ThrowIfMissing(countdownText, nameof(countdownText));
        }

        private void ThrowIfMissing(UnityEngine.Object value, string fieldName)
        {
            if (value == null)
                throw new InvalidOperationException($"{nameof(WorldCraftingPanelController)} on '{gameObject.name}' is missing required reference '{fieldName}'.");
        }

        private void DetachFromMainMenuRoot()
        {
            var currentTransform = transform;
            var parent = currentTransform.parent;
            if (parent == null || parent.parent == null)
                return;

            var parentName = (parent.name ?? string.Empty).Trim();
            if (!string.Equals(parentName, "WorldMenuPanel", StringComparison.Ordinal))
                return;

            currentTransform.SetParent(parent.parent, false);
            currentTransform.SetAsLastSibling();
        }
    }
}
