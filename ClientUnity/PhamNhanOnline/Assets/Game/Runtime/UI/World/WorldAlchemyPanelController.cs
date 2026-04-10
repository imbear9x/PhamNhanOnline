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
using PhamNhanOnline.Client.UI.Alchemy;
using PhamNhanOnline.Client.UI.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.World
{
    public sealed class WorldAlchemyPanelController : MonoBehaviour
    {
        private sealed class IngredientSelection
        {
            public readonly List<long> SelectedPlayerItemIds = new List<long>(4);
            public bool Armed;
        }

        [Header("Recipe References")]
        [SerializeField] private TMP_Text recipeListStatusText;
        [SerializeField] private AlchemyRecipeListView recipeListView;
        [SerializeField] private AlchemyRecipeSlotView selectedRecipeSlotView;
        [SerializeField] private AlchemyRecipeTooltipView recipeTooltipView;

        [Header("Inventory References")]
        [SerializeField] private TMP_Text inventoryStatusText;
        [SerializeField] private InventoryItemGridView inventoryGridView;
        [SerializeField] private InventoryItemTooltipView inventoryItemTooltipView;
        [SerializeField] private InventoryItemPresentationCatalog itemPresentationCatalog;

        [Header("Ingredient References")]
        [SerializeField] private AlchemyIngredientSlotView ingredientSlotView;
        [SerializeField] private TMP_Text ingredientStatusText;

        [Header("Recipe Detail Text")]
        [SerializeField] private TMP_Text selectedRecipeNameText;
        [SerializeField] private TMP_Text selectedRecipeDescriptionText;
        [SerializeField] private TMP_Text durationText;
        [SerializeField] private TMP_Text successRateText;
        [SerializeField] private TMP_Text masteryText;
        [SerializeField] private TMP_Text practiceStatusText;

        [Header("Practice Controls")]
        [SerializeField] private Button craftButton;
        [SerializeField] private TMP_Text craftButtonText;
        [SerializeField] private Button pauseResumeButton;
        [SerializeField] private TMP_Text pauseResumeButtonText;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TMP_Text cancelButtonText;
        [SerializeField] private Image progressFillImage;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text countdownText;

        [Header("Behavior")]
        [SerializeField] private bool autoLoadOnEnable = true;
        [SerializeField] private bool clearDraftWhenClosedWithoutPractice = true;

        [Header("Display Text")]
        [SerializeField] private string loadingRecipesText = "Dang tai dan phuong...";
        [SerializeField] private string missingRecipesText = "Chua tai danh sach dan phuong.";
        [SerializeField] private string emptyRecipesText = "Chua hoc dan phuong nao.";
        [SerializeField] private string emptyRecipeName = "Chua dat dan phuong";
        [SerializeField] private string emptyRecipeDescription = "Keo dan phuong da hoc vao o ben phai de bat dau chuan bi luyen che.";
        [SerializeField] private string emptyIngredientsText = "Keo nguyen lieu hop le vao tung o.";
        [SerializeField] private string craftIdleText = "Luyen che";
        [SerializeField] private string pauseIdleText = "Tam dung";
        [SerializeField] private string resumeIdleText = "Tiep tuc";
        [SerializeField] private string cancelIdleText = "Huy bo";

        private readonly Dictionary<int, IngredientSelection> selectionsByInputId = new Dictionary<int, IngredientSelection>();

        private int? selectedRecipeId;
        private float liveSessionAnchorTime;
        private long liveSessionRemainingSeconds;
        private string lastSnapshot = string.Empty;
        private bool craftActionInFlight;
        private bool sessionActionInFlight;
        private long lastDisplayPracticeSessionId;

        private void Awake()
        {
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

            if (inventoryGridView != null)
            {
                inventoryGridView.ItemHovered += HandleInventoryItemHovered;
                inventoryGridView.ItemHoverExited += HandleInventoryItemHoverExited;
                inventoryGridView.ItemClicked += HandleInventoryItemClicked;
            }

            if (ingredientSlotView != null)
            {
                ingredientSlotView.InventoryItemDropped += HandleIngredientInventoryItemDropped;
                ingredientSlotView.Clicked += HandleIngredientSlotClicked;
            }

            if (craftButton != null)
            {
                craftButton.onClick.RemoveListener(HandleCraftButtonClicked);
                craftButton.onClick.AddListener(HandleCraftButtonClicked);
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
        }

        private void Start()
        {
            ValidateSerializedReferences();
        }

        private void OnEnable()
        {
            Refresh(force: true);
            if (autoLoadOnEnable)
                _ = ReloadAllAsync(forceInventoryRefresh: false);
        }

        private void Update()
        {
            Refresh(force: false);
        }

        private void OnDisable()
        {
            if (clearDraftWhenClosedWithoutPractice && !HasBlockingAlchemySession())
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

            if (inventoryGridView != null)
            {
                inventoryGridView.ItemHovered -= HandleInventoryItemHovered;
                inventoryGridView.ItemHoverExited -= HandleInventoryItemHoverExited;
                inventoryGridView.ItemClicked -= HandleInventoryItemClicked;
            }

            if (ingredientSlotView != null)
            {
                ingredientSlotView.InventoryItemDropped -= HandleIngredientInventoryItemDropped;
                ingredientSlotView.Clicked -= HandleIngredientSlotClicked;
            }

            if (craftButton != null)
                craftButton.onClick.RemoveListener(HandleCraftButtonClicked);
            if (pauseResumeButton != null)
                pauseResumeButton.onClick.RemoveListener(HandlePauseResumeButtonClicked);
            if (cancelButton != null)
                cancelButton.onClick.RemoveListener(HandleCancelButtonClicked);
        }

        private void Refresh(bool force)
        {
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
                ClientLog.Warn($"WorldAlchemyPanelController reload exception: {ex.Message}");
            }
            finally
            {
                Refresh(force: true);
            }
        }

        private void ApplyMissingState(bool force)
        {
            ApplyText(recipeListStatusText, missingRecipesText, force);
            ApplyText(inventoryStatusText, "Kho do chua san sang.", force);
            ApplyText(selectedRecipeNameText, emptyRecipeName, force);
            ApplyText(selectedRecipeDescriptionText, emptyRecipeDescription, force);
            ApplyText(ingredientStatusText, emptyIngredientsText, force);
            ApplyText(durationText, "Thoi gian: -", force);
            ApplyText(successRateText, "Ti le thanh cong: -", force);
            ApplyText(masteryText, "Thu tay: -", force);
            ApplyText(practiceStatusText, "Trang thai: -", force);
            ApplyText(progressText, "0%", force);
            ApplyText(countdownText, "--:--", force);

            if (progressFillImage != null)
                progressFillImage.fillAmount = 0f;

            if (recipeListView != null)
                recipeListView.Clear(force: true);
            if (selectedRecipeSlotView != null)
                selectedRecipeSlotView.Clear();
            if (inventoryGridView != null)
                inventoryGridView.Clear(force: true);
            ClearIngredientViews();
            ApplyButtons(false, false, false, false, null);
        }

        private void ApplyLoadedState(bool force)
        {
            var recipes = ClientRuntime.Alchemy.Recipes ?? Array.Empty<LearnedPillRecipeModel>();
            var selectedDetail = TryGetSelectedRecipeDetail(out var detail) ? detail : (PillRecipeDetailModel?)null;
            var displaySession = GetDisplayAlchemySession();
            var preview = selectedRecipeId.HasValue && ClientRuntime.Alchemy.LastPreview.HasValue &&
                          ClientRuntime.Alchemy.LastPreview.Value.PillRecipeTemplateId == selectedRecipeId.Value
                ? ClientRuntime.Alchemy.LastPreview
                : null;

            if (recipeListView != null)
                recipeListView.SetItems(recipes, selectedRecipeId, itemPresentationCatalog, force: true);

            ApplyText(recipeListStatusText, ResolveRecipeListStatus(recipes), force);
            ApplyInventory(force);
            ApplySelectedRecipe(selectedDetail, displaySession, preview, force);
            ApplyIngredients(selectedDetail, displaySession, force);
            ApplyPracticeStatus(displaySession, preview, force);
            ApplyButtonsFromState(selectedDetail, displaySession, preview);
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
            ApplyText(
                inventoryStatusText,
                inventoryState.HasLoadedInventory
                    ? (ordered.Length > 0 ? string.Concat("Kho nguyen lieu: ", ordered.Length.ToString(CultureInfo.InvariantCulture), " vat pham") : "Kho nguyen lieu dang trong.")
                    : "Kho nguyen lieu chua tai.",
                force);
        }

        private IReadOnlyList<InventoryItemModel> BuildProjectedInventoryItems(IReadOnlyList<InventoryItemModel> items)
        {
            if (items == null || items.Count == 0)
                return items ?? Array.Empty<InventoryItemModel>();

            // Once a practice session is active/paused/pending, inventory has already been
            // authoritatively consumed on the server. Do not subtract the local draft again.
            if (HasBlockingAlchemySession())
                return items;

            if (selectionsByInputId.Count == 0 || !TryGetSelectedRecipeDetail(out var detail) || detail.Inputs == null)
                return items ?? Array.Empty<InventoryItemModel>();

            var reservedStackQuantitiesByTemplateId = new Dictionary<int, int>();
            var reservedNonStackableIds = new HashSet<long>();
            for (var i = 0; i < detail.Inputs.Count; i++)
            {
                var input = detail.Inputs[i];
                if (!selectionsByInputId.TryGetValue(input.InputId, out var selection) || !selection.Armed)
                    continue;

                if (input.RequiredItem.IsStackable)
                {
                    if (input.RequiredQuantity <= 0)
                        continue;

                    reservedStackQuantitiesByTemplateId[input.RequiredItem.ItemTemplateId] =
                        reservedStackQuantitiesByTemplateId.TryGetValue(input.RequiredItem.ItemTemplateId, out var existing)
                            ? existing + Math.Max(0, input.RequiredQuantity)
                            : Math.Max(0, input.RequiredQuantity);
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

                ApplyText(selectedRecipeNameText, emptyRecipeName, force);
                ApplyText(selectedRecipeDescriptionText, emptyRecipeDescription, force);
                ApplyText(durationText, "Thoi gian: -", force);
                ApplyText(successRateText, "Ti le thanh cong: -", force);
                ApplyText(masteryText, "Thu tay: -", force);
                return;
            }

            var learnedRecipe = ResolveLearnedRecipe(detail.Value.PillRecipeTemplateId, detail.Value);
            if (selectedRecipeSlotView != null)
            {
                var presentation = itemPresentationCatalog != null
                    ? itemPresentationCatalog.Resolve(learnedRecipe.ResultPill)
                    : new InventoryItemPresentation(null, null, Color.white);
                selectedRecipeSlotView.SetRecipe(learnedRecipe, presentation);
                selectedRecipeSlotView.SetInteractionLocked(activeSession.HasValue);
            }

            ApplyText(selectedRecipeNameText, learnedRecipe.Name ?? emptyRecipeName, force);
            ApplyText(selectedRecipeDescriptionText, string.IsNullOrWhiteSpace(detail.Value.Description) ? emptyRecipeDescription : detail.Value.Description.Trim(), force);
            ApplyText(durationText, string.Concat("Thoi gian: ", FormatDuration(detail.Value.CraftDurationSeconds)), force);
            ApplyText(successRateText, string.Concat("Ti le thanh cong: ", ResolveSuccessRateText(detail.Value, preview)), force);
            ApplyText(
                masteryText,
                string.Concat(
                    "Thu tay: ",
                    detail.Value.TotalCraftCount.ToString(CultureInfo.InvariantCulture),
                    " lan | Cong them ",
                    FormatPercent(detail.Value.CurrentSuccessRateBonus)),
                force);
        }

        private void ApplyIngredients(PillRecipeDetailModel? detail, PracticeSessionModel? activeSession, bool force)
        {
            var inputs = detail.HasValue && detail.Value.Inputs != null
                ? detail.Value.Inputs
                : null;
            if (inputs == null || inputs.Count == 0)
            {
                ClearIngredientViews();
                ApplyText(ingredientStatusText, emptyIngredientsText, force);
                return;
            }

            if (ingredientSlotView != null)
            {
                var aggregate = ResolveAggregateIngredientProgress(inputs, activeSession);
                var primaryItem = ResolvePrimaryIngredientItem(inputs);
                var presentation = itemPresentationCatalog != null && primaryItem.HasValue
                    ? itemPresentationCatalog.Resolve(primaryItem.Value)
                    : new InventoryItemPresentation(null, null, Color.white);

                ingredientSlotView.gameObject.SetActive(true);
                ingredientSlotView.SetState(
                    "Tong hop nguyen lieu",
                    presentation,
                    aggregate.current,
                    aggregate.required,
                    aggregate.hasSelection,
                    activeSession.HasValue,
                    aggregate.stateLabel);
            }

            ApplyText(ingredientStatusText, BuildIngredientSummary(inputs, activeSession), force);
        }

        private void ApplyPracticeStatus(PracticeSessionModel? displaySession, AlchemyCraftPreviewModel? preview, bool force)
        {
            if (!displaySession.HasValue)
            {
                liveSessionAnchorTime = 0f;
                liveSessionRemainingSeconds = 0L;
                ApplyText(practiceStatusText, preview.HasValue && !preview.Value.CanCraft
                    ? string.IsNullOrWhiteSpace(preview.Value.FailureReason)
                        ? "Nguyen lieu chua san sang."
                        : preview.Value.FailureReason.Trim()
                    : "Chua bat dau luyen che.",
                    force);
                ApplyText(progressText, "0%", force);
                ApplyText(countdownText, "--:--", force);
                if (progressFillImage != null)
                    progressFillImage.fillAmount = 0f;
                return;
            }

            if (displaySession.Value.PracticeState == 3)
            {
                liveSessionAnchorTime = 0f;
                liveSessionRemainingSeconds = 0L;
                if (progressFillImage != null)
                    progressFillImage.fillAmount = 1f;

                ApplyText(practiceStatusText, "Dang cho xem ket qua luyen che.", force);
                ApplyText(progressText, "100%", force);
                ApplyText(countdownText, FormatDuration(0L), force);
                return;
            }

            if (displaySession.Value.PracticeSessionId != 0 && (liveSessionRemainingSeconds <= 0L || liveSessionAnchorTime <= 0f))
            {
                liveSessionRemainingSeconds = Math.Max(0L, displaySession.Value.RemainingDurationSeconds);
                liveSessionAnchorTime = Time.unscaledTime;
            }

            var remainingSeconds = ResolveLiveRemainingSeconds(displaySession.Value);
            var totalDuration = Math.Max(1L, displaySession.Value.TotalDurationSeconds);
            var progress = Mathf.Clamp01((float)(totalDuration - remainingSeconds) / totalDuration);
            if (progressFillImage != null)
                progressFillImage.fillAmount = progress;

            ApplyText(practiceStatusText, displaySession.Value.IsPaused ? "Dang tam dung luyen che." : "Dang luyen che...", force);
            ApplyText(progressText, string.Concat(Mathf.RoundToInt(progress * 100f).ToString(CultureInfo.InvariantCulture), "%"), force);
            ApplyText(countdownText, FormatDuration(remainingSeconds), force);
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
                    pauseResumeInteractable: showPracticeButtons && !sessionActionInFlight,
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

        private string ResolveRecipeListStatus(IReadOnlyList<LearnedPillRecipeModel> recipes)
        {
            if (!ClientRuntime.Alchemy.HasLoadedRecipes)
                return ClientRuntime.Alchemy.IsLoadingRecipes ? loadingRecipesText : missingRecipesText;

            if (recipes == null || recipes.Count == 0)
                return emptyRecipesText;

            return string.Concat("Da hoc ", recipes.Count.ToString(CultureInfo.InvariantCulture), " dan phuong.");
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

        private void HandleInventoryItemHovered(InventoryItemModel item)
        {
            if (inventoryItemTooltipView == null)
                return;

            var presentation = itemPresentationCatalog != null
                ? itemPresentationCatalog.Resolve(item)
                : new InventoryItemPresentation(null, null, Color.white);
            inventoryItemTooltipView.Show(item, presentation, force: true);
        }

        private void HandleInventoryItemHoverExited()
        {
            HideInventoryTooltip(force: true);
        }

        private void HandleInventoryItemClicked(InventoryItemModel item)
        {
            HandleInventoryItemHovered(item);
        }

        private void HandleIngredientInventoryItemDropped(InventoryItemModel item)
        {
            if (HasBlockingAlchemySession() || !TryGetSelectedRecipeDetail(out var detail) || detail.Inputs == null)
                return;

            if (!TryAssignInventoryItemToRecipe(detail.Inputs, item))
                return;

            _ = RefreshPreviewAsync();
            Refresh(force: true);
        }

        private void HandleIngredientSlotClicked(PointerEventData.InputButton button)
        {
            if (selectedRecipeId.HasValue)
                _ = ShowRecipeTooltipAsync(selectedRecipeId.Value);
        }

        private void HandleCraftButtonClicked()
        {
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

            _ = StartCraftAsync(selectedRecipeId.Value);
        }

        private void HandlePauseResumeButtonClicked()
        {
            if (sessionActionInFlight)
                return;

            var session = GetActiveAlchemySession();
            if (!session.HasValue)
                return;

            _ = TogglePauseResumeAsync(session.Value);
        }

        private void HandleCancelButtonClicked()
        {
            if (sessionActionInFlight)
                return;

            var session = GetActiveAlchemySession();
            if (!session.HasValue || !session.Value.CanCancel)
                return;

            _ = CancelPracticeAsync(session.Value.PracticeSessionId);
        }

        private async Task StartCraftAsync(int recipeId)
        {
            craftActionInFlight = true;
            Refresh(force: true);
            try
            {
                var result = await ClientRuntime.AlchemyService.CraftPillAsync(
                    recipeId,
                    BuildSelectedPlayerItemIds(),
                    BuildSelectedOptionalInputIds());
                if (!result.Success)
                    ClientLog.Warn($"WorldAlchemyPanelController failed to start craft: {result.Message}");
            }
            catch (Exception ex)
            {
                ClientLog.Warn($"WorldAlchemyPanelController craft exception: {ex.Message}");
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
                    ClientLog.Warn($"WorldAlchemyPanelController practice toggle failed: {result.Message}");

                liveSessionAnchorTime = 0f;
                liveSessionRemainingSeconds = 0L;
            }
            catch (Exception ex)
            {
                ClientLog.Warn($"WorldAlchemyPanelController practice toggle exception: {ex.Message}");
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
                    ClientLog.Warn($"WorldAlchemyPanelController cancel practice failed: {result.Message}");
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
                ClientLog.Warn($"WorldAlchemyPanelController cancel practice exception: {ex.Message}");
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
                ClientLog.Warn($"WorldAlchemyPanelController load detail exception: {ex.Message}");
                return null;
            }
        }

        private async Task RefreshPreviewAsync()
        {
            if (!ClientRuntime.IsInitialized || !selectedRecipeId.HasValue || HasBlockingAlchemySession())
                return;

            try
            {
                await ClientRuntime.AlchemyService.PreviewCraftAsync(
                    selectedRecipeId.Value,
                    BuildSelectedPlayerItemIds(),
                    BuildSelectedOptionalInputIds());
            }
            catch (Exception ex)
            {
                ClientLog.Warn($"WorldAlchemyPanelController preview exception: {ex.Message}");
            }
            finally
            {
                Refresh(force: true);
            }
        }

        private void SetSelectedRecipe(int recipeId)
        {
            if (selectedRecipeId == recipeId)
                return;

            selectedRecipeId = recipeId;
            selectionsByInputId.Clear();
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
            selectionsByInputId.Clear();
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
            if (activeSession.HasValue)
                return ResolveConsumedQuantity(input);

            if (!selectionsByInputId.TryGetValue(input.InputId, out var selection) || !selection.Armed)
                return 0;

            if (!input.RequiredItem.IsStackable)
                return selection.SelectedPlayerItemIds.Count;

            return ResolveInventoryQuantity(input.RequiredItem.ItemTemplateId);
        }

        private bool ResolveInputArmed(PillRecipeInputModel input, PracticeSessionModel? activeSession)
        {
            if (activeSession.HasValue)
                return ResolveConsumedQuantity(input) > 0;

            return selectionsByInputId.TryGetValue(input.InputId, out var selection) && selection.Armed;
        }

        private int ResolveConsumedQuantity(PillRecipeInputModel input)
        {
            var status = ClientRuntime.Alchemy.LastPracticeStatus;
            if (!status.HasValue || status.Value.ConsumedItems == null)
                return 0;

            var total = 0;
            for (var i = 0; i < status.Value.ConsumedItems.Count; i++)
            {
                var entry = status.Value.ConsumedItems[i];
                if (entry.Item.ItemTemplateId != input.RequiredItem.ItemTemplateId)
                    continue;

                total += Math.Max(0, entry.Quantity);
            }

            return total;
        }

        private int ResolveInventoryQuantity(int itemTemplateId)
        {
            var items = ClientRuntime.Inventory.Items;
            var total = 0;
            for (var i = 0; i < items.Length; i++)
            {
                if (items[i].IsEquipped || items[i].ItemTemplateId != itemTemplateId)
                    continue;

                total += Math.Max(0, items[i].Quantity);
            }

            return total;
        }

        private (int current, int required, bool hasSelection, string stateLabel) ResolveAggregateIngredientProgress(
            IReadOnlyList<PillRecipeInputModel> inputs,
            PracticeSessionModel? activeSession)
        {
            var current = 0;
            var required = 0;
            var hasSelection = false;

            for (var i = 0; i < inputs.Count; i++)
            {
                var input = inputs[i];
                var resolvedRequired = Math.Max(1, input.RequiredQuantity);
                var resolvedCurrent = Math.Min(resolvedRequired, ResolveAssignedQuantity(input, activeSession));
                required += resolvedRequired;
                current += resolvedCurrent;
                hasSelection |= resolvedCurrent > 0 || ResolveInputArmed(input, activeSession);
            }

            string stateLabel;
            if (activeSession.HasValue)
                stateLabel = activeSession.Value.IsPaused ? "Dang tam dung" : "Dang khoa";
            else if (current >= required)
                stateLabel = "Da du";
            else if (hasSelection)
                stateLabel = "Dang them";
            else
                stateLabel = "Keo nguyen lieu vao day";

            return (current, required, hasSelection, stateLabel);
        }

        private ItemTemplateSummaryModel? ResolvePrimaryIngredientItem(IReadOnlyList<PillRecipeInputModel> inputs)
        {
            if (inputs == null || inputs.Count == 0)
                return null;

            return inputs[0].RequiredItem;
        }

        private bool TryAssignInventoryItemToRecipe(IReadOnlyList<PillRecipeInputModel> inputs, InventoryItemModel item)
        {
            if (inputs == null || item.ItemTemplateId <= 0)
                return false;

            for (var i = 0; i < inputs.Count; i++)
            {
                var input = inputs[i];
                if (input.RequiredItem.ItemTemplateId != item.ItemTemplateId)
                    continue;

                if (!selectionsByInputId.TryGetValue(input.InputId, out var selection))
                {
                    selection = new IngredientSelection();
                    selectionsByInputId[input.InputId] = selection;
                }

                if (input.RequiredItem.IsStackable)
                {
                    selection.Armed = true;
                    return true;
                }

                if (selection.SelectedPlayerItemIds.Contains(item.PlayerItemId))
                    return false;

                if (selection.SelectedPlayerItemIds.Count >= Math.Max(1, input.RequiredQuantity))
                    continue;

                selection.Armed = true;
                selection.SelectedPlayerItemIds.Add(item.PlayerItemId);
                return true;
            }

            return false;
        }

        private bool AreRequiredInputsReady(PillRecipeDetailModel detail, PracticeSessionModel? activeSession)
        {
            if (detail.Inputs == null || detail.Inputs.Count == 0)
                return true;

            for (var i = 0; i < detail.Inputs.Count; i++)
            {
                var input = detail.Inputs[i];
                if (input.IsOptional)
                    continue;

                var requiredQuantity = Math.Max(1, input.RequiredQuantity);
                if (ResolveAssignedQuantity(input, activeSession) < requiredQuantity)
                    return false;
            }

            return true;
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
                builder.Append(Math.Min(Math.Max(1, input.RequiredQuantity), currentQuantity).ToString(CultureInfo.InvariantCulture));
                builder.Append('/');
                builder.Append(Math.Max(1, input.RequiredQuantity).ToString(CultureInfo.InvariantCulture));
                if (input.IsOptional)
                    builder.Append(" (tuy chon)");
            }

            return builder.ToString();
        }

        private long[] BuildSelectedPlayerItemIds()
        {
            return selectionsByInputId.Values
                .SelectMany(static selection => selection.SelectedPlayerItemIds)
                .Distinct()
                .OrderBy(static id => id)
                .ToArray();
        }

        private int[] BuildSelectedOptionalInputIds()
        {
            if (!TryGetSelectedRecipeDetail(out var detail) || detail.Inputs == null)
                return Array.Empty<int>();

            return detail.Inputs
                .Where(static input => input.IsOptional)
                .Where(input => selectionsByInputId.TryGetValue(input.InputId, out var selection) && selection.Armed)
                .Select(static input => input.InputId)
                .OrderBy(static id => id)
                .ToArray();
        }

        private int ResolveAssignedQuantityForTooltip(PillRecipeInputModel input)
        {
            return ResolveAssignedQuantity(input, GetDisplayAlchemySession());
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

        private static string ResolveSuccessRateText(PillRecipeDetailModel detail, AlchemyCraftPreviewModel? preview)
        {
            if (preview.HasValue)
                return FormatPercent(preview.Value.EffectiveSuccessRate);

            var rate = NormalizeRate(detail.BaseSuccessRate) + NormalizeRate(detail.CurrentSuccessRateBonus);
            if (detail.SuccessRateCap.HasValue)
                rate = Math.Min(rate, NormalizeRate(detail.SuccessRateCap.Value));
            return FormatPercent(rate);
        }

        private void ClearIngredientViews()
        {
            if (ingredientSlotView == null)
                return;

            ingredientSlotView.gameObject.SetActive(false);
            ingredientSlotView.Clear();
        }

        private void ClearDraft()
        {
            selectedRecipeId = null;
            selectionsByInputId.Clear();
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
        }

        private void HideRecipeTooltip(bool force)
        {
            if (recipeTooltipView != null)
                recipeTooltipView.Hide(force);
        }

        private void HideInventoryTooltip(bool force)
        {
            if (inventoryItemTooltipView != null)
                inventoryItemTooltipView.Hide(force);
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
                        string.Join(",", pair.Value.SelectedPlayerItemIds.OrderBy(static id => id).Select(static id => id.ToString(CultureInfo.InvariantCulture))))));
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
                session.Value.RemainingDurationSeconds.ToString(CultureInfo.InvariantCulture),
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
                preview.Value.EffectiveSuccessRate.ToString("0.####", CultureInfo.InvariantCulture),
                ":",
                preview.Value.FailureReason ?? string.Empty);
        }

        private void ValidateSerializedReferences()
        {
            ThrowIfMissing(recipeListStatusText, nameof(recipeListStatusText));
            ThrowIfMissing(recipeListView, nameof(recipeListView));
            ThrowIfMissing(selectedRecipeSlotView, nameof(selectedRecipeSlotView));
            ThrowIfMissing(recipeTooltipView, nameof(recipeTooltipView));
            ThrowIfMissing(inventoryStatusText, nameof(inventoryStatusText));
            ThrowIfMissing(inventoryGridView, nameof(inventoryGridView));
            ThrowIfMissing(inventoryItemTooltipView, nameof(inventoryItemTooltipView));
            ThrowIfMissing(itemPresentationCatalog, nameof(itemPresentationCatalog));
            ThrowIfMissing(ingredientSlotView, nameof(ingredientSlotView));
            ThrowIfMissing(selectedRecipeNameText, nameof(selectedRecipeNameText));
            ThrowIfMissing(durationText, nameof(durationText));
            ThrowIfMissing(successRateText, nameof(successRateText));
            ThrowIfMissing(practiceStatusText, nameof(practiceStatusText));
            ThrowIfMissing(craftButton, nameof(craftButton));
            ThrowIfMissing(craftButtonText, nameof(craftButtonText));
            ThrowIfMissing(pauseResumeButton, nameof(pauseResumeButton));
            ThrowIfMissing(pauseResumeButtonText, nameof(pauseResumeButtonText));
            ThrowIfMissing(cancelButton, nameof(cancelButton));
            ThrowIfMissing(cancelButtonText, nameof(cancelButtonText));
            ThrowIfMissing(progressFillImage, nameof(progressFillImage));
            ThrowIfMissing(progressText, nameof(progressText));
            ThrowIfMissing(countdownText, nameof(countdownText));
        }

        private void ThrowIfMissing(UnityEngine.Object value, string fieldName)
        {
            if (value == null)
                throw new InvalidOperationException($"{nameof(WorldAlchemyPanelController)} on '{gameObject.name}' is missing required reference '{fieldName}'.");
        }
    }
}
