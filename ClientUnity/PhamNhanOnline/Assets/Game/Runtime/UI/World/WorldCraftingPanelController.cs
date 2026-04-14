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
        private sealed class IngredientSelection
        {
            public readonly List<long> SelectedPlayerItemIds = new List<long>(4);
            public bool Armed;
            public int AssignedQuantity;
        }

        private enum QuantityPopupMode
        {
            None = 0,
            CraftCount = 1,
            OptionalInputQuantity = 2
        }

        [Header("Recipe References")]
        [SerializeField] private TMP_Text recipeListStatusText;
        [SerializeField] private CraftRecipeListView recipeListView;
        [SerializeField] private CraftRecipeSlotView selectedRecipeSlotView;
        [SerializeField] private CraftRecipeTooltipView recipeTooltipView;

        [Header("Inventory References")]
        [SerializeField] private TMP_Text inventoryStatusText;
        [SerializeField] private InventoryItemGridView inventoryGridView;
        [SerializeField] private InventoryItemTooltipView inventoryItemTooltipView;
        [SerializeField] private InventoryItemPresentationCatalog itemPresentationCatalog;

        [Header("Ingredient References")]
        [SerializeField] private RectTransform requiredIngredientSlotsRoot;
        [SerializeField] private CraftMaterialSlotView requiredIngredientSlotTemplate;
        [SerializeField] private TMP_Text ingredientStatusText;
        [SerializeField] private RectTransform optionalIngredientSlotsRoot;
        [SerializeField] private CraftMaterialSlotView optionalIngredientSlotTemplate;
        [SerializeField] private InventoryDropQuantityPopupView quantityPopupView;

        [Header("Recipe Detail Text")]
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
        [SerializeField] private bool detachFromMainMenuOnAwake = true;
        [SerializeField] private bool hideOnAwake = true;
        [SerializeField] private KeyCode closeKey = KeyCode.Escape;

        [Header("Display Text")]
        [SerializeField] private string loadingRecipesText = "Dang tai dan phuong...";
        [SerializeField] private string missingRecipesText = "Chua tai danh sach dan phuong.";
        [SerializeField] private string emptyRecipesText = "Chua hoc dan phuong nao.";
        [SerializeField] private string emptyIngredientsText = "Keo nguyen lieu hop le vao tung o.";
        [SerializeField] private string craftIdleText = "Luyen che";
        [SerializeField] private string pauseIdleText = "Tam dung";
        [SerializeField] private string resumeIdleText = "Tiep tuc";
        [SerializeField] private string cancelIdleText = "Huy bo";
        [SerializeField] [Range(1, 6)] private int maxRequiredIngredientSlots = 6;

        private readonly Dictionary<int, IngredientSelection> selectionsByInputId = new Dictionary<int, IngredientSelection>();
        private readonly List<CraftMaterialSlotView> requiredIngredientSlotViews = new List<CraftMaterialSlotView>();
        private readonly Dictionary<CraftMaterialSlotView, int> requiredInputIdBySlotView = new Dictionary<CraftMaterialSlotView, int>();
        private readonly List<CraftMaterialSlotView> optionalIngredientSlotViews = new List<CraftMaterialSlotView>();
        private readonly Dictionary<CraftMaterialSlotView, int> optionalInputIdBySlotView = new Dictionary<CraftMaterialSlotView, int>();

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

        public bool IsPanelVisible => gameObject.activeSelf;

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
            if (autoLoadOnEnable)
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

            UnbindRequiredIngredientSlots();
            UnbindOptionalIngredientSlots();

            if (craftButton != null)
                craftButton.onClick.RemoveListener(HandleCraftButtonClicked);
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

            if (inventoryGridView != null)
            {
                inventoryGridView.ItemHovered += HandleInventoryItemHovered;
                inventoryGridView.ItemHoverExited += HandleInventoryItemHoverExited;
                inventoryGridView.ItemClicked += HandleInventoryItemClicked;
            }

            RebuildRequiredIngredientSlots(0);
            RebuildOptionalIngredientSlots(0);

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

            isInitialized = true;

            if (hideAfterInitialize)
                gameObject.SetActive(false);
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
                ClientLog.Warn($"WorldCraftingPanelController reload exception: {ex.Message}");
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
            ApplyText(ingredientStatusText, emptyIngredientsText, force);
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

                ApplyText(masteryText, "Thu tay: -", force);
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

            var requiredInputs = inputs.Where(static input => !input.IsOptional).ToArray();
            var optionalInputs = inputs.Where(static input => input.IsOptional).ToArray();

            ApplyRequiredIngredientSlots(requiredInputs, activeSession);
            ApplyOptionalIngredientSlots(optionalInputs, activeSession);
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

            ApplyText(
                practiceStatusText,
                displaySession.Value.IsPaused
                    ? string.Concat("Dang tam dung lo ", displaySession.Value.RequestedCraftCount.ToString(CultureInfo.InvariantCulture), " vien.")
                    : string.Concat("Dang luyen lo ", displaySession.Value.RequestedCraftCount.ToString(CultureInfo.InvariantCulture), " vien..."),
                force);
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

        private void HandleIngredientInventoryItemDropped(CraftMaterialSlotView slotView, InventoryItemModel item)
        {
            if (HasBlockingAlchemySession() || !TryGetSelectedRecipeDetail(out var detail) || detail.Inputs == null)
                return;

            if (requiredInputIdBySlotView.TryGetValue(slotView, out var requiredInputId))
            {
                if (!TryAssignInventoryItemToInput(detail.Inputs, requiredInputId, item))
                    return;

                _ = RefreshPreviewAsync();
                Refresh(force: true);
                return;
            }

            if (!optionalInputIdBySlotView.TryGetValue(slotView, out var optionalInputId))
                return;

            if (!TryAssignInventoryItemToInput(detail.Inputs, optionalInputId, item))
                return;

            _ = RefreshPreviewAsync();
            Refresh(force: true);
        }

        private void HandleIngredientSlotClicked(CraftMaterialSlotView slotView, PointerEventData.InputButton button)
        {
            if (button == PointerEventData.InputButton.Right)
            {
                if ((requiredInputIdBySlotView.TryGetValue(slotView, out var requiredInputId) ||
                     optionalInputIdBySlotView.TryGetValue(slotView, out requiredInputId)) &&
                    selectionsByInputId.Remove(requiredInputId))
                {
                    HideQuantityPopup(force: true);
                    _ = RefreshPreviewAsync();
                    Refresh(force: true);
                }

                return;
            }

            if (optionalInputIdBySlotView.TryGetValue(slotView, out var inputId) &&
                TryGetSelectedRecipeDetail(out var detail) &&
                detail.Inputs != null)
            {
                var optionalInput = detail.Inputs.FirstOrDefault(input => input.InputId == inputId);
                if (optionalInput.InputId > 0 &&
                    optionalInput.RequiredItem.IsStackable &&
                    selectionsByInputId.TryGetValue(inputId, out var selection) &&
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
            ShowCraftCountPopup(detail, preview.Value, maxCraftableCount);
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
            if (detail.Inputs == null || detail.Inputs.Count == 0)
                return 1;

            var maxCraftableCount = int.MaxValue;
            for (var i = 0; i < detail.Inputs.Count; i++)
            {
                var input = detail.Inputs[i];
                if (input.IsOptional)
                    continue;

                var availableQuantity = input.RequiredItem.IsStackable
                    ? ResolveInventoryQuantity(input.RequiredItem.ItemTemplateId)
                    : (selectionsByInputId.TryGetValue(input.InputId, out var selection)
                        ? selection.SelectedPlayerItemIds.Count
                        : 0);
                var craftableForInput = availableQuantity / Math.Max(1, input.RequiredQuantity);
                maxCraftableCount = Math.Min(maxCraftableCount, craftableForInput);
            }

            return Math.Max(1, maxCraftableCount == int.MaxValue ? 1 : maxCraftableCount);
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

            if (input.IsOptional)
                return Math.Max(0, selection.AssignedQuantity);

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

        private void ApplyRequiredIngredientSlots(IReadOnlyList<PillRecipeInputModel> requiredInputs, PracticeSessionModel? activeSession)
        {
            if (requiredInputs == null || requiredInputs.Count == 0)
            {
                ClearRequiredIngredientViews();
                return;
            }

            var slotCount = Math.Min(Math.Max(1, maxRequiredIngredientSlots), requiredInputs.Count);
            if (requiredInputs.Count > slotCount)
                ClientLog.Error($"WorldCraftingPanelController recipe {selectedRecipeId} requires {requiredInputs.Count} mandatory inputs but UI supports only {slotCount}.");

            RebuildRequiredIngredientSlots(slotCount);
            for (var i = 0; i < requiredIngredientSlotViews.Count; i++)
            {
                var slotView = requiredIngredientSlotViews[i];
                if (i >= requiredInputs.Count)
                {
                    slotView.gameObject.SetActive(false);
                    continue;
                }

                var input = requiredInputs[i];
                requiredInputIdBySlotView[slotView] = input.InputId;
                var presentation = itemPresentationCatalog != null
                    ? itemPresentationCatalog.Resolve(input.RequiredItem)
                    : new InventoryItemPresentation(null, null, Color.white);
                var currentQuantity = ResolveAssignedQuantity(input, activeSession);
                var hasSelection = ResolveInputArmed(input, activeSession);
                var stateLabel = activeSession.HasValue
                    ? (activeSession.Value.IsPaused ? "Dang tam dung" : "Dang khoa")
                    : (currentQuantity >= Math.Max(1, input.RequiredQuantity)
                        ? "Da du"
                        : hasSelection ? "Dang them" : "Keo vao day");
                slotView.gameObject.SetActive(true);
                slotView.SetState(
                    input.RequiredItem.Name,
                    presentation,
                    currentQuantity,
                    Math.Max(1, input.RequiredQuantity),
                    hasSelection,
                    activeSession.HasValue,
                    stateLabel,
                    showOptionalBadge: false);
            }
        }

        private bool TryAssignInventoryItemToInput(IReadOnlyList<PillRecipeInputModel> inputs, int inputId, InventoryItemModel item)
        {
            if (inputs == null)
                return false;

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

                    ShowOptionalInputQuantityPopup(input, selection);
                    return true;
                }

                if (selection.SelectedPlayerItemIds.Contains(item.PlayerItemId))
                    return false;

                selection.SelectedPlayerItemIds.Add(item.PlayerItemId);
                selection.AssignedQuantity = selection.SelectedPlayerItemIds.Count;
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
            return selectionsByInputId.Values
                .SelectMany(static selection => selection.SelectedPlayerItemIds)
                .Distinct()
                .OrderBy(static id => id)
                .ToArray();
        }

        private AlchemyOptionalInputSelectionModel[] BuildSelectedOptionalInputs()
        {
            if (!TryGetSelectedRecipeDetail(out var detail) || detail.Inputs == null)
                return Array.Empty<AlchemyOptionalInputSelectionModel>();

            return detail.Inputs
                .Where(static input => input.IsOptional)
                .Where(input => selectionsByInputId.TryGetValue(input.InputId, out var selection) && selection.Armed)
                .Select(input => new AlchemyOptionalInputSelectionModel
                {
                    InputId = input.InputId,
                    Quantity = ResolveOptionalApplicationCount(input)
                })
                .Where(static selection => selection.Quantity > 0)
                .OrderBy(static selection => selection.InputId)
                .ToArray();
        }

        private int ResolveAssignedQuantityForTooltip(PillRecipeInputModel input)
        {
            return ResolveAssignedQuantity(input, GetDisplayAlchemySession());
        }

        private int ResolveOptionalApplicationCount(PillRecipeInputModel input)
        {
            if (!input.IsOptional ||
                !selectionsByInputId.TryGetValue(input.InputId, out var selection) ||
                !selection.Armed)
            {
                return 0;
            }

            return Math.Max(0, ResolveAssignedQuantity(input, null) / Math.Max(1, input.RequiredQuantity));
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

        private string ResolveDurationText(
            PillRecipeDetailModel detail,
            PracticeSessionModel? activeSession,
            AlchemyCraftPreviewModel? preview)
        {
            if (activeSession.HasValue)
                return FormatDuration(activeSession.Value.TotalDurationSeconds);

            return FormatDuration(detail.CraftDurationSeconds);
        }

        private void ApplyOptionalIngredientSlots(IReadOnlyList<PillRecipeInputModel> optionalInputs, PracticeSessionModel? activeSession)
        {
            if (optionalInputs == null || optionalInputs.Count == 0)
            {
                ClearOptionalIngredientViews();
                return;
            }

            RebuildOptionalIngredientSlots(optionalInputs.Count);
            for (var i = 0; i < optionalIngredientSlotViews.Count; i++)
            {
                var slotView = optionalIngredientSlotViews[i];
                if (i >= optionalInputs.Count)
                {
                    slotView.gameObject.SetActive(false);
                    continue;
                }

                var input = optionalInputs[i];
                optionalInputIdBySlotView[slotView] = input.InputId;
                var presentation = itemPresentationCatalog != null
                    ? itemPresentationCatalog.Resolve(input.RequiredItem)
                    : new InventoryItemPresentation(null, null, Color.white);
                var currentQuantity = ResolveAssignedQuantity(input, activeSession);
                var hasSelection = ResolveInputArmed(input, activeSession);
                var stateLabel = activeSession.HasValue
                    ? (activeSession.Value.IsPaused ? "Dang tam dung" : "Dang khoa")
                    : (hasSelection ? "Da gan" : "Keo catalyst vao day");
                slotView.gameObject.SetActive(true);
                slotView.SetState(
                    input.RequiredItem.Name,
                    presentation,
                    currentQuantity,
                    Math.Max(1, input.RequiredQuantity),
                    hasSelection,
                    activeSession.HasValue,
                    stateLabel,
                    showOptionalBadge: true);
            }
        }

        private void RebuildRequiredIngredientSlots(int requiredCount)
        {
            if (requiredIngredientSlotsRoot == null || requiredIngredientSlotTemplate == null)
                return;

            requiredIngredientSlotTemplate.gameObject.SetActive(false);
            while (requiredIngredientSlotViews.Count < requiredCount)
            {
                var slotView = Instantiate(requiredIngredientSlotTemplate, requiredIngredientSlotsRoot);
                slotView.name = string.Concat(requiredIngredientSlotTemplate.name, "_", requiredIngredientSlotViews.Count.ToString(CultureInfo.InvariantCulture));
                slotView.gameObject.SetActive(true);
                slotView.InventoryItemDropped += HandleIngredientInventoryItemDropped;
                slotView.Clicked += HandleIngredientSlotClicked;
                requiredIngredientSlotViews.Add(slotView);
            }

            while (requiredIngredientSlotViews.Count > requiredCount)
            {
                var index = requiredIngredientSlotViews.Count - 1;
                var slotView = requiredIngredientSlotViews[index];
                requiredIngredientSlotViews.RemoveAt(index);
                requiredInputIdBySlotView.Remove(slotView);
                slotView.InventoryItemDropped -= HandleIngredientInventoryItemDropped;
                slotView.Clicked -= HandleIngredientSlotClicked;
                Destroy(slotView.gameObject);
            }

            requiredIngredientSlotsRoot.gameObject.SetActive(requiredCount > 0);
        }

        private void RebuildOptionalIngredientSlots(int requiredCount)
        {
            if (optionalIngredientSlotsRoot == null || optionalIngredientSlotTemplate == null)
                return;

            optionalIngredientSlotTemplate.gameObject.SetActive(false);
            while (optionalIngredientSlotViews.Count < requiredCount)
            {
                var slotView = Instantiate(optionalIngredientSlotTemplate, optionalIngredientSlotsRoot);
                slotView.name = string.Concat(optionalIngredientSlotTemplate.name, "_", optionalIngredientSlotViews.Count.ToString(CultureInfo.InvariantCulture));
                slotView.gameObject.SetActive(true);
                slotView.InventoryItemDropped += HandleIngredientInventoryItemDropped;
                slotView.Clicked += HandleIngredientSlotClicked;
                optionalIngredientSlotViews.Add(slotView);
            }

            while (optionalIngredientSlotViews.Count > requiredCount)
            {
                var index = optionalIngredientSlotViews.Count - 1;
                var slotView = optionalIngredientSlotViews[index];
                optionalIngredientSlotViews.RemoveAt(index);
                optionalInputIdBySlotView.Remove(slotView);
                slotView.InventoryItemDropped -= HandleIngredientInventoryItemDropped;
                slotView.Clicked -= HandleIngredientSlotClicked;
                Destroy(slotView.gameObject);
            }

            optionalIngredientSlotsRoot.gameObject.SetActive(requiredCount > 0);
        }

        private void UnbindRequiredIngredientSlots()
        {
            for (var i = 0; i < requiredIngredientSlotViews.Count; i++)
            {
                var slotView = requiredIngredientSlotViews[i];
                if (slotView == null)
                    continue;

                slotView.InventoryItemDropped -= HandleIngredientInventoryItemDropped;
                slotView.Clicked -= HandleIngredientSlotClicked;
            }
        }

        private void UnbindOptionalIngredientSlots()
        {
            for (var i = 0; i < optionalIngredientSlotViews.Count; i++)
            {
                var slotView = optionalIngredientSlotViews[i];
                if (slotView == null)
                    continue;

                slotView.InventoryItemDropped -= HandleIngredientInventoryItemDropped;
                slotView.Clicked -= HandleIngredientSlotClicked;
            }
        }

        private void ClearRequiredIngredientViews()
        {
            if (requiredIngredientSlotsRoot != null)
                requiredIngredientSlotsRoot.gameObject.SetActive(false);

            for (var i = 0; i < requiredIngredientSlotViews.Count; i++)
            {
                var slotView = requiredIngredientSlotViews[i];
                if (slotView == null)
                    continue;

                slotView.gameObject.SetActive(false);
                slotView.Clear();
            }
        }

        private void ClearOptionalIngredientViews()
        {
            if (optionalIngredientSlotsRoot != null)
                optionalIngredientSlotsRoot.gameObject.SetActive(false);

            for (var i = 0; i < optionalIngredientSlotViews.Count; i++)
            {
                var slotView = optionalIngredientSlotViews[i];
                if (slotView == null)
                    continue;

                slotView.gameObject.SetActive(false);
                slotView.Clear();
            }
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
                string.IsNullOrWhiteSpace(detail.Name) ? "Luyen che" : detail.Name,
                Mathf.Max(1, maxCraftableCount),
                HandleQuantityPopupConfirmed,
                HandleQuantityPopupCancelled,
                "Chon so vien luyen che",
                initialQuantity: Mathf.Clamp(preview.RequestedCraftCount > 0 ? preview.RequestedCraftCount : 1, 1, Mathf.Max(1, maxCraftableCount)),
                hintOverride: ResolveCraftCountHint(preview),
                confirmLabelOverride: "Luyen");
        }

        private string ResolveCraftCountHint(AlchemyCraftPreviewModel preview)
        {
            var boostedCraftCount = BuildSelectedOptionalInputs().Sum(static selection => Math.Max(0, selection.Quantity));
            if (boostedCraftCount > 0)
            {
                return string.Concat(
                    "Toi da ",
                    preview.MaxCraftableCount.ToString(CultureInfo.InvariantCulture),
                    " vien. Catalyst hien tai buff duoc ",
                    Math.Min(preview.MaxCraftableCount, boostedCraftCount).ToString(CultureInfo.InvariantCulture),
                    " vien.");
            }

            return string.Concat("Toi da ", preview.MaxCraftableCount.ToString(CultureInfo.InvariantCulture), " vien.");
        }

        private void ShowOptionalInputQuantityPopup(PillRecipeInputModel input, IngredientSelection selection)
        {
            if (quantityPopupView == null)
                return;

            quantityPopupMode = QuantityPopupMode.OptionalInputQuantity;
            quantityPopupInputId = input.InputId;
            var maxQuantity = Math.Max(
                Math.Max(0, selection.AssignedQuantity),
                Math.Max(0, selection.AssignedQuantity) + ResolveInventoryQuantity(input.RequiredItem.ItemTemplateId));
            quantityPopupView.Show(
                string.IsNullOrWhiteSpace(input.RequiredItem.Name) ? "Catalyst" : input.RequiredItem.Name,
                Mathf.Max(1, maxQuantity),
                HandleQuantityPopupConfirmed,
                HandleQuantityPopupCancelled,
                "Chon so luong catalyst",
                initialQuantity: Mathf.Clamp(selection.AssignedQuantity > 0 ? selection.AssignedQuantity : Math.Max(1, input.RequiredQuantity), 1, Mathf.Max(1, maxQuantity)),
                hintOverride: string.Concat("Moi ", Math.Max(1, input.RequiredQuantity).ToString(CultureInfo.InvariantCulture), " catalyst buff 1 vien."),
                confirmLabelOverride: "Gan");
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
                    if (inputId.HasValue &&
                        selectionsByInputId.TryGetValue(inputId.Value, out var selection))
                    {
                        selection.Armed = quantity > 0;
                        selection.AssignedQuantity = Math.Max(0, quantity);
                        if (selection.AssignedQuantity <= 0 && selection.SelectedPlayerItemIds.Count == 0)
                            selectionsByInputId.Remove(inputId.Value);

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
            ClearRequiredIngredientViews();
            ClearOptionalIngredientViews();
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
            HideQuantityPopup(force: true);
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
                        pair.Value.AssignedQuantity.ToString(CultureInfo.InvariantCulture),
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
            ThrowIfMissing(requiredIngredientSlotsRoot, nameof(requiredIngredientSlotsRoot));
            ThrowIfMissing(requiredIngredientSlotTemplate, nameof(requiredIngredientSlotTemplate));
            ThrowIfMissing(optionalIngredientSlotsRoot, nameof(optionalIngredientSlotsRoot));
            ThrowIfMissing(optionalIngredientSlotTemplate, nameof(optionalIngredientSlotTemplate));
            ThrowIfMissing(quantityPopupView, nameof(quantityPopupView));
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
