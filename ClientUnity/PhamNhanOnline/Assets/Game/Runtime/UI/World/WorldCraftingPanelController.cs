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
    public sealed class WorldCraftingPanelController : MonoBehaviour
    {
        private const int CharacterStateCultivating = 3;

        private enum QuantityPopupMode
        {
            None = 0,
            InputQuantity = 1
        }

        [Header("Panel")]
        [SerializeField] private WorldCraftingPanelView panelView;

        [Header("Legacy View Wiring")]
        [SerializeField] [HideInInspector] private TMP_Text panelTitleText;
        [SerializeField] [HideInInspector] private CraftRecipeListView recipeListView;
        [SerializeField] [HideInInspector] private CraftRecipeSlotView selectedRecipeSlotView;
        [SerializeField] [HideInInspector] private InventoryItemGridView inventoryGridView;
        [SerializeField] [HideInInspector] private InventoryItemPresentationCatalog itemPresentationCatalog;
        [FormerlySerializedAs("ingredientPanelView")]
        [SerializeField] [HideInInspector] private CraftInputPanelView inputPanelView;
        [SerializeField] [HideInInspector] private CraftResultPreviewView craftingResultPreviewView;
        [SerializeField] [HideInInspector] private DropZoneView dropZoneView;
        [SerializeField] [HideInInspector] private UIButtonView closeButton;
        [SerializeField] [HideInInspector] private UIButtonView craftButton;
        [SerializeField] [HideInInspector] private TMP_Text craftButtonText;
        [SerializeField] [HideInInspector] private UIButtonView pauseResumeButton;
        [SerializeField] [HideInInspector] private TMP_Text pauseResumeButtonText;
        [SerializeField] [HideInInspector] private UIButtonView cancelButton;
        [SerializeField] [HideInInspector] private TMP_Text cancelButtonText;

        [Header("Behavior")]
        [SerializeField] private bool autoLoadOnEnable = true;
        [SerializeField] private bool clearDraftWhenClosedWithoutPractice = true;
        [SerializeField] private bool detachFromMainMenuOnAwake = true;
        [SerializeField] private bool hideOnAwake = true;
        [SerializeField] private KeyCode closeKey = KeyCode.Escape;

        [Header("Diagnostics")]
        [SerializeField] private bool logRecipeSelectionDiagnostics = true;

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
        [SerializeField] private string selectRecipeOptionText = "Chon";
        [SerializeField] private string removeSelectionOptionText = "Bo ra";
        [SerializeField] private string assignRequiredOptionText = "Gan vao nguyen lieu bat buoc";
        [SerializeField] private string assignOptionalOptionText = "Gan vao nguyen lieu phu tro";

        [Header("Notification Text")]
        [SerializeField] private string cancelWithoutRefundWarningTitle = "Canh bao";
        [SerializeField] [TextArea] private string cancelWithoutRefundWarningMessage = "Huy bo luyen che se khong hoan lai nguyen lieu.";

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
        private bool quantityPopupRestoreHadSelection;
        private int quantityPopupRestoreAssignedQuantity;
        private int? popupRecipeId;
        private long? popupInventoryPlayerItemId;
        private CraftingStationType currentStationType = CraftingStationType.Alchemy;
        private string currentStationTitleOverride;

        public bool IsPanelVisible => panelView != null ? panelView.IsVisible : gameObject.activeSelf;

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
            HideItemOptionsPopup(force: true);
            HideQuantityPopup(force: true);
            HideRecipeTooltip(force: true);
            HideInventoryTooltip(force: true);

            if (IsPanelVisible)
                Refresh(force: true);
        }

        private void Awake()
        {
            EnsureInitialized(hideAfterInitialize: hideOnAwake);
        }

        private void Start()
        {
            if (panelView == null)
                return;

            AdoptLegacyBindingsIfNeeded();
            panelView.ValidateSerializedReferences();
        }

        private void OnEnable()
        {
            if (!IsPanelVisible)
                return;

            Refresh(force: true);
            if (autoLoadOnEnable && IsAlchemyStation())
                _ = ReloadAllAsync(forceInventoryRefresh: false);
        }

        private void Update()
        {
            if (!IsPanelVisible)
                return;

            if (Input.GetKeyDown(closeKey))
            {
                if (!IsCloseLockedBecauseCultivating())
                    HidePanel();
                return;
            }

            Refresh(force: false);
        }

        private void OnDisable()
        {
            HideItemOptionsPopup(force: true);
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
            if (panelView == null)
                return;

            panelView.RecipeListClicked -= HandleRecipeListClicked;
            panelView.RecipeListHovered -= HandleRecipeListHovered;
            panelView.RecipeHoverExited -= HandleRecipeHoverExited;
            panelView.SelectedRecipeDropped -= HandleSelectedRecipeDropped;
            panelView.SelectedRecipeClicked -= HandleSelectedRecipeClicked;
            panelView.SelectedRecipeHovered -= HandleSelectedRecipeHovered;
            panelView.SelectedRecipeHoverExited -= HandleRecipeHoverExited;
            panelView.SelectedRecipeDroppedBackToList -= HandleRecipeDroppedBackToList;
            panelView.InputInventoryItemDropped -= HandleInputInventoryItemDropped;
            panelView.InputSlotClicked -= HandleInputSlotClicked;
            panelView.InputSlotHovered -= HandleInputSlotHovered;
            panelView.InputSlotHoverExited -= HandleInputSlotHoverExited;
            panelView.InventoryItemClicked -= HandleCraftingInventoryItemClicked;
            panelView.DropZonePayloadDropped -= HandleDropZonePayloadDropped;
            panelView.DropZoneClicked -= HandleDropZoneClicked;
            panelView.CraftClicked -= HandleCraftButtonClicked;
            panelView.CloseClicked -= HandleCloseButtonClicked;
            panelView.PauseResumeClicked -= HandlePauseResumeButtonClicked;
            panelView.CancelClicked -= HandleCancelButtonClicked;
        }

        public void ShowPanel()
        {
            EnsureInitialized(hideAfterInitialize: false);
            if (!IsPanelVisible)
            {
                if (panelView != null)
                    panelView.Show();
                else
                    gameObject.SetActive(true);
                return;
            }

            Refresh(force: true);
        }

        public void HidePanel()
        {
            EnsureInitialized(hideAfterInitialize: false);
            if (!IsPanelVisible)
                return;

            if (panelView != null)
                panelView.Hide(force: true);
            else
                gameObject.SetActive(false);
        }

        private void EnsureInitialized(bool hideAfterInitialize)
        {
            if (panelView == null)
                panelView = GetComponent<WorldCraftingPanelView>();

            if (isInitialized)
                return;

            if (panelView == null)
                throw new InvalidOperationException($"{nameof(WorldCraftingPanelController)} on '{gameObject.name}' requires {nameof(WorldCraftingPanelView)}.");

            if (detachFromMainMenuOnAwake)
                DetachFromMainMenuRoot();

            AdoptLegacyBindingsIfNeeded();
            panelView.ValidateSerializedReferences();
            panelView.RecipeListClicked += HandleRecipeListClicked;
            panelView.RecipeListHovered += HandleRecipeListHovered;
            panelView.RecipeHoverExited += HandleRecipeHoverExited;
            panelView.SelectedRecipeDropped += HandleSelectedRecipeDropped;
            panelView.SelectedRecipeClicked += HandleSelectedRecipeClicked;
            panelView.SelectedRecipeHovered += HandleSelectedRecipeHovered;
            panelView.SelectedRecipeHoverExited += HandleRecipeHoverExited;
            panelView.SelectedRecipeDroppedBackToList += HandleRecipeDroppedBackToList;
            panelView.InputInventoryItemDropped += HandleInputInventoryItemDropped;
            panelView.InputSlotClicked += HandleInputSlotClicked;
            panelView.InputSlotHovered += HandleInputSlotHovered;
            panelView.InputSlotHoverExited += HandleInputSlotHoverExited;
            panelView.InventoryItemClicked += HandleCraftingInventoryItemClicked;
            panelView.DropZonePayloadDropped += HandleDropZonePayloadDropped;
            panelView.DropZoneClicked += HandleDropZoneClicked;
            panelView.CraftClicked += HandleCraftButtonClicked;
            panelView.CloseClicked += HandleCloseButtonClicked;
            panelView.PauseResumeClicked += HandlePauseResumeButtonClicked;
            panelView.CancelClicked += HandleCancelButtonClicked;
            panelView.ClearInputs();

            isInitialized = true;

            if (hideAfterInitialize)
                panelView.Hide(force: true);
        }

        private void AdoptLegacyBindingsIfNeeded()
        {
            panelView?.AdoptLegacyBindings(
                panelTitleText,
                recipeListView,
                selectedRecipeSlotView,
                inventoryGridView,
                itemPresentationCatalog,
                inputPanelView,
                craftingResultPreviewView,
                dropZoneView,
                closeButton,
                craftButton,
                pauseResumeButton,
                pauseResumeButtonText,
                cancelButton);
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
            if (panelView == null)
                return;

            panelView.SetPanelTitle(ResolvePanelTitle(), force);
            panelView.SetRecipeListInteractionLocked(false);
            panelView.ClearCraftingResultPreview();
            panelView.ClearRecipeList(force: true);
            panelView.ClearSelectedRecipe();
            panelView.SetInventoryInteractionLocked(false);
            panelView.ClearInventory(force: true);
            panelView.ClearInputs();
            panelView.SetCloseButtonVisible(!IsCloseLockedBecauseCultivating());
            ApplyButtons(false, false, false, false, null);
        }

        private void ApplyUnsupportedStationState(bool force)
        {
            if (panelView == null)
                return;

            panelView.SetPanelTitle(ResolvePanelTitle(), force);
            panelView.SetRecipeListInteractionLocked(false);
            panelView.ClearCraftingResultPreview();
            panelView.ClearRecipeList(force: true);
            panelView.ClearSelectedRecipe();
            panelView.SetInventoryInteractionLocked(false);
            panelView.ClearInventory(force: true);
            panelView.ClearInputs();
            HideRecipeTooltip(force: true);
            HideInventoryTooltip(force: true);
            panelView.SetCloseButtonVisible(!IsCloseLockedBecauseCultivating());
            ApplyButtons(false, false, false, false, null);
            panelView.SetCraftButtonState(false, false, craftActionInFlight ? "Dang gui..." : craftIdleText);
            panelView.SetPauseResumeButtonState(false, false, sessionActionInFlight ? "Dang gui..." : pauseIdleText);
            panelView.SetCancelButtonState(false, false, sessionActionInFlight ? "Dang gui..." : cancelIdleText);
        }

        private void ApplyLoadedState(bool force)
        {
            ApplyPanelTitle(force);
            var recipes = ClientRuntime.Alchemy.Recipes ?? Array.Empty<LearnedPillRecipeModel>();
            var selectedDetail = TryGetSelectedRecipeDetail(out var detail) ? detail : (PillRecipeDetailModel?)null;
            var selectedRecipe = selectedRecipeId.HasValue && TryGetLearnedRecipe(selectedRecipeId.Value, out var learnedRecipe)
                ? learnedRecipe
                : (LearnedPillRecipeModel?)null;
            var displaySession = GetDisplayAlchemySession();
            var preview = selectedRecipeId.HasValue && ClientRuntime.Alchemy.LastPreview.HasValue &&
                          ClientRuntime.Alchemy.LastPreview.Value.PillRecipeTemplateId == selectedRecipeId.Value
                ? ClientRuntime.Alchemy.LastPreview
                : null;
            var interactionLocked = displaySession.HasValue;

            panelView?.SetRecipeList(recipes, selectedRecipeId, force: true);
            panelView?.SetRecipeListInteractionLocked(interactionLocked);
            panelView?.SetInventoryInteractionLocked(interactionLocked);
            panelView?.SetCloseButtonVisible(!IsCloseLockedBecauseCultivating());

            ApplyInventory(force);
            ApplySelectedRecipe(selectedDetail, selectedRecipe, displaySession, preview, force);
            ApplyInputs(selectedDetail, displaySession, force);
            ApplyCraftingResultPreview(selectedDetail, displaySession, preview);
            ApplyButtonsFromState(selectedDetail, displaySession, preview);
        }

        private void ApplyPanelTitle(bool force)
        {
            panelView?.SetPanelTitle(ResolvePanelTitle(), force);
        }

        private void ApplyInventory(bool force)
        {
            if (panelView == null)
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

            panelView.SetInventoryItems(ordered, force: true);
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
                    var reservedQuantity = Math.Max(
                        0,
                        selection.AssignedQuantity > 0
                            ? selection.AssignedQuantity
                            : input.RequiredQuantity);
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
            LearnedPillRecipeModel? selectedRecipe,
            PracticeSessionModel? activeSession,
            AlchemyCraftPreviewModel? preview,
            bool force)
        {
            if (panelView == null)
                return;

            if (!detail.HasValue)
            {
                if (!selectedRecipe.HasValue)
                {
                    panelView.ClearSelectedRecipe();
                    return;
                }

                panelView.SetSelectedRecipe(
                    selectedRecipe.Value,
                    selectedRecipe.Value.ResultPill,
                    activeSession.HasValue);
                return;
            }

            var learnedRecipe = ResolveLearnedRecipe(detail.Value.PillRecipeTemplateId, detail.Value);
            panelView.SetSelectedRecipe(
                learnedRecipe,
                learnedRecipe.ResultPill,
                activeSession.HasValue);
        }

        private void ApplyInputs(PillRecipeDetailModel? detail, PracticeSessionModel? activeSession, bool force)
        {
            var inputs = detail.HasValue && detail.Value.Inputs != null
                ? detail.Value.Inputs
                : null;
            if (inputs == null || inputs.Count == 0)
            {
                panelView?.ClearInputs();
                return;
            }

            var requiredInputs = inputs.Where(static input => !input.IsOptional).ToArray();
            var optionalInputs = inputs.Where(static input => input.IsOptional).ToArray();

            panelView?.SetInputSlots(
                BuildRequiredInputSlotStates(requiredInputs, activeSession),
                BuildOptionalInputSlotStates(optionalInputs, activeSession));
        }

        private void ApplyCraftingResultPreview(
            PillRecipeDetailModel? detail,
            PracticeSessionModel? displaySession,
            AlchemyCraftPreviewModel? preview)
        {
            if (panelView == null)
                return;

            if (!detail.HasValue)
            {
                panelView.ClearCraftingResultPreview();
                return;
            }

            var resultItem = ResolveCraftingResultItem(detail.Value, displaySession);
            panelView.SetCraftingResultPreview(
                resultItem,
                string.IsNullOrWhiteSpace(resultItem.Name) ? "Ket qua luyen che" : resultItem.Name.Trim(),
                ResolveSuccessRateText(displaySession, detail.Value, preview),
                ResolveDurationText(detail.Value, displaySession, preview),
                ResolveCraftingResultQuantity(displaySession, preview),
                ResolveCraftingResultProgressFillAmount(displaySession),
                ResolveCraftingResultProgressLabel(displaySession),
                ResolvePracticeStatusText(displaySession));
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
            if (panelView == null)
                return;

            panelView.SetCraftButtonState(
                !showPracticeButtons && (craftInteractable || craftActionInFlight),
                craftInteractable,
                craftActionInFlight ? "Dang gui..." : craftIdleText);
            panelView.SetPauseResumeButtonState(
                showPracticeButtons,
                pauseResumeInteractable,
                sessionActionInFlight ? "Dang gui..." : (pauseButtonLabel ?? pauseIdleText));
            panelView.SetCancelButtonState(
                showPracticeButtons,
                cancelInteractable,
                sessionActionInFlight ? "Dang gui..." : cancelIdleText);
        }

        private void HandleRecipeListClicked(LearnedPillRecipeModel recipe)
        {
            if (HasBlockingAlchemySession())
            {
                LogRecipeSelection($"recipe-list-click ignored because practice session is blocking recipe={DescribeRecipe(recipe)}.");
                return;
            }

            LogRecipeSelection($"recipe-list-click received recipe={DescribeRecipe(recipe)}.");
            ShowRecipeOptions(recipe);
        }

        private void HandleRecipeListHovered(LearnedPillRecipeModel recipe)
        {
            _ = ShowRecipeTooltipAsync(recipe.PillRecipeTemplateId);
        }

        private void HandleSelectedRecipeDropped(LearnedPillRecipeModel recipe)
        {
            if (HasBlockingAlchemySession())
            {
                LogRecipeSelection($"selected-slot-drop ignored because practice session is blocking recipe={DescribeRecipe(recipe)}.");
                return;
            }

            LogRecipeSelection($"selected-slot-drop received recipe={DescribeRecipe(recipe)}.");
            _ = SetSelectedRecipeAsync(recipe.PillRecipeTemplateId);
        }

        private void HandleSelectedRecipeClicked()
        {
            if (HasBlockingAlchemySession())
                return;

            if (selectedRecipeId.HasValue)
                ShowSelectedRecipeOptions(selectedRecipeId.Value);
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

        private void HandleInputInventoryItemDropped(int inputId, InventoryItemModel item)
        {
            if (HasBlockingAlchemySession() || !TryGetSelectedRecipeDetail(out var detail) || detail.Inputs == null)
                return;

            if (!TryAssignInventoryItemToInput(detail.Inputs, inputId, item))
                return;

            _ = RefreshPreviewAsync();
            Refresh(force: true);
        }

        private void HandleInputSlotClicked(int inputId, bool isOptional, PointerEventData.InputButton button)
        {
            if (button != PointerEventData.InputButton.Left && button != PointerEventData.InputButton.Right)
                return;

            if (HasBlockingAlchemySession() || !TryGetSelectedRecipeDetail(out var detail) || detail.Inputs == null)
                return;

            if (!TryResolveInput(detail.Inputs, inputId, out var input) ||
                !input.RequiredItem.IsStackable ||
                !draftState.TryGetSelection(inputId, out var selection) ||
                !selection.Armed)
            {
                HideItemOptionsPopup(force: true);
                return;
            }

            ShowInputQuantityPopup(
                input,
                selection,
                restoreHadSelection: true,
                restoreAssignedQuantity: Math.Max(0, selection.AssignedQuantity));
        }

        private void HandleInputSlotHovered(int inputId, bool isOptional)
        {
            if (selectedRecipeId.HasValue)
                _ = ShowRecipeTooltipAsync(selectedRecipeId.Value);
        }

        private void HandleInputSlotHoverExited()
        {
            HideRecipeTooltip(force: true);
        }

        private void HandleCraftingInventoryItemClicked(InventoryItemModel item)
        {
            if (HasBlockingAlchemySession())
                return;

            ShowInventoryItemOptions(item);
        }

        private void HandleDropZonePayloadDropped(UIDragPayload payload)
        {
            if (payload.Kind == UIDragPayloadKind.CraftInputMaterial &&
                payload.SourceKind == UIDragSourceKind.CraftInputMaterialSlot &&
                payload.HasSourceIndex)
            {
                RemoveInputSelection(payload.SourceIndex);
            }
        }

        private void HandleDropZoneClicked(PointerEventData.InputButton button)
        {
            if (button == PointerEventData.InputButton.Left)
                HideItemOptionsPopup(force: true);
        }

        private void ShowRecipeOptions(LearnedPillRecipeModel recipe)
        {
            if (recipe.PillRecipeTemplateId <= 0)
            {
                LogRecipeSelection($"show-recipe-options ignored invalid recipe={DescribeRecipe(recipe)}.");
                return;
            }

            LogRecipeSelection($"show-recipe-options recipe={DescribeRecipe(recipe)}.");
            var options = new List<ItemOptionEntry>(1)
            {
                new ItemOptionEntry(selectRecipeOptionText, () =>
                {
                    LogRecipeSelection($"recipe-option-select clicked recipe={DescribeRecipe(recipe)}.");
                    HideItemOptionsPopup(force: true);
                    _ = SetSelectedRecipeAsync(recipe.PillRecipeTemplateId);
                })
            };

            popupRecipeId = recipe.PillRecipeTemplateId;
            popupInventoryPlayerItemId = null;
            HideRecipeTooltip(force: true);
            panelView?.ShowItemOptionsPopup(options, force: true);
        }

        private void ShowSelectedRecipeOptions(int recipeId)
        {
            if (recipeId <= 0)
                return;

            var options = new List<ItemOptionEntry>(1)
            {
                new ItemOptionEntry(removeSelectionOptionText, () =>
                {
                    HideItemOptionsPopup(force: true);
                    ClearDraft();
                    Refresh(force: true);
                })
            };

            popupRecipeId = recipeId;
            popupInventoryPlayerItemId = null;
            HideRecipeTooltip(force: true);
            panelView?.ShowItemOptionsPopup(options, force: true);
        }

        private void ShowInventoryItemOptions(InventoryItemModel item)
        {
            var options = BuildInventoryAssignmentOptions(item);
            if (options.Count == 0)
            {
                HideItemOptionsPopup(force: true);
                return;
            }

            popupRecipeId = null;
            popupInventoryPlayerItemId = item.PlayerItemId;
            HideRecipeTooltip(force: true);
            HideInventoryTooltip(force: true);
            panelView?.ShowItemOptionsPopup(options, force: true);
        }

        private List<ItemOptionEntry> BuildInventoryAssignmentOptions(InventoryItemModel item)
        {
            var options = new List<ItemOptionEntry>();
            if (HasBlockingAlchemySession() || !TryGetSelectedRecipeDetail(out var detail) || detail.Inputs == null)
                return options;

            var requiredOrdinal = 0;
            var optionalOrdinal = 0;
            for (var i = 0; i < detail.Inputs.Count; i++)
            {
                var input = detail.Inputs[i];
                if (input.RequiredItem.ItemTemplateId != item.ItemTemplateId)
                    continue;

                string label;
                if (input.IsOptional)
                {
                    optionalOrdinal++;
                    label = optionalOrdinal > 1
                        ? string.Concat(assignOptionalOptionText, " ", optionalOrdinal.ToString(CultureInfo.InvariantCulture))
                        : assignOptionalOptionText;
                }
                else
                {
                    requiredOrdinal++;
                    label = requiredOrdinal > 1
                        ? string.Concat(assignRequiredOptionText, " ", requiredOrdinal.ToString(CultureInfo.InvariantCulture))
                        : assignRequiredOptionText;
                }

                var capturedInputId = input.InputId;
                options.Add(new ItemOptionEntry(label, () =>
                {
                    HideItemOptionsPopup(force: true);
                    AssignInventoryItemToInput(capturedInputId, item);
                }));
            }

            return options;
        }

        private void AssignInventoryItemToInput(int inputId, InventoryItemModel item)
        {
            if (HasBlockingAlchemySession() || !TryGetSelectedRecipeDetail(out var detail) || detail.Inputs == null)
                return;

            if (!TryAssignInventoryItemToInput(detail.Inputs, inputId, item))
                return;

            _ = RefreshPreviewAsync();
            Refresh(force: true);
        }

        private void RemoveInputSelection(int inputId)
        {
            if (!draftState.ClearInput(inputId))
                return;

            HideItemOptionsPopup(force: true);
            HideQuantityPopup(force: true);
            _ = RefreshPreviewAsync();
            Refresh(force: true);
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

            var requestedCraftCount = Mathf.Max(1, preview.Value.RequestedCraftCount);
            _ = StartCraftAsync(detail.PillRecipeTemplateId, requestedCraftCount);
        }

        private void HandleCloseButtonClicked()
        {
            if (IsCloseLockedBecauseCultivating())
                return;

            HidePanel();
        }

        private static bool IsCloseLockedBecauseCultivating()
        {
            if (!ClientRuntime.IsInitialized)
                return false;

            var currentState = ClientRuntime.Character.CurrentState;
            return currentState.HasValue && currentState.Value.CurrentState == CharacterStateCultivating;
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

            if (ShouldConfirmCancelWithoutRefund(session.Value))
            {
                ShowCancelWithoutRefundConfirmation(session.Value.PracticeSessionId);
                return;
            }

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

        private bool ShouldConfirmCancelWithoutRefund(PracticeSessionModel session)
        {
            return session.Progress >= Math.Clamp(session.CancelRefundProgressThreshold, 0d, 1d);
        }

        private void ShowCancelWithoutRefundConfirmation(long practiceSessionId)
        {
            HideItemOptionsPopup(force: true);
            HideQuantityPopup(force: true);
            HideRecipeTooltip(force: true);
            HideInventoryTooltip(force: true);

            WorldModalUIManager.Instance?.ShowNotificationPopup(
                cancelWithoutRefundWarningTitle,
                cancelWithoutRefundWarningMessage,
                Array.Empty<NotificationPopupItemData>(),
                onConfirm: () => _ = CancelPracticeAsync(practiceSessionId),
                showCancelButton: true,
                onCancel: null);
        }

        private async Task ShowRecipeTooltipAsync(int recipeId)
        {
            if (recipeId <= 0)
                return;

            var detail = await EnsureRecipeDetailLoadedAsync(recipeId, forceRefresh: false);
            if (!detail.HasValue)
                return;

            panelView?.ShowRecipeTooltip(detail.Value, ResolveAssignedQuantityForTooltip, force: true);
        }

        private async Task<PillRecipeDetailModel?> EnsureRecipeDetailLoadedAsync(int recipeId, bool forceRefresh)
        {
            if (ClientRuntime.Alchemy.TryGetRecipeDetail(recipeId, out var cached) && !forceRefresh)
            {
                LogRecipeSelection($"recipe-detail cache-hit recipeId={recipeId}.");
                return cached;
            }

            try
            {
                LogRecipeSelection($"recipe-detail load-start recipeId={recipeId} forceRefresh={forceRefresh}.");
                var result = await ClientRuntime.AlchemyService.LoadRecipeDetailAsync(recipeId, forceRefresh);
                if (!result.Success || !result.Recipe.HasValue)
                {
                    ClientLog.Warn(
                        $"WorldCraftingPanelController failed to load recipe detail recipeId={recipeId} " +
                        $"code={result.Code?.ToString() ?? "None"} reason='{result.FailureReason}'.");
                    return null;
                }

                LogRecipeSelection(
                    $"recipe-detail load-success recipeId={recipeId} received={result.Recipe.Value.PillRecipeTemplateId} " +
                    $"inputCount={(result.Recipe.Value.Inputs != null ? result.Recipe.Value.Inputs.Count : 0)}.");

                if (result.Recipe.Value.PillRecipeTemplateId != recipeId)
                {
                    ClientLog.Warn(
                        $"WorldCraftingPanelController received mismatched recipe detail. " +
                        $"requested={recipeId} received={result.Recipe.Value.PillRecipeTemplateId}. Retrying once.");

                    result = await ClientRuntime.AlchemyService.LoadRecipeDetailAsync(recipeId, forceRefresh: true);
                    if (!result.Success ||
                        !result.Recipe.HasValue ||
                        result.Recipe.Value.PillRecipeTemplateId != recipeId)
                    {
                        ClientLog.Warn(
                            $"WorldCraftingPanelController failed to load matching recipe detail after retry. " +
                            $"requested={recipeId} received={(result.Recipe.HasValue ? result.Recipe.Value.PillRecipeTemplateId.ToString(CultureInfo.InvariantCulture) : "None")} " +
                            $"code={result.Code?.ToString() ?? "None"} reason='{result.FailureReason}'.");
                        return null;
                    }
                }

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

        private async Task SetSelectedRecipeAsync(int recipeId)
        {
            if (recipeId <= 0 || !ClientRuntime.IsInitialized)
            {
                LogRecipeSelection($"set-selected ignored recipeId={recipeId} runtimeInitialized={ClientRuntime.IsInitialized}.");
                return;
            }

            LogRecipeSelection($"set-selected start recipeId={recipeId} previous={selectedRecipeId?.ToString(CultureInfo.InvariantCulture) ?? "None"}.");
            var selectionChanged = selectedRecipeId != recipeId;
            selectedRecipeId = recipeId;
            if (selectionChanged)
            {
                draftState.Clear();
                LogRecipeSelection($"set-selected cleared draft for recipeId={recipeId}.");
            }

            Refresh(force: true);
            LogRecipeSelection($"set-selected refreshed immediate state recipeId={recipeId}.");

            var detail = await EnsureRecipeDetailLoadedAsync(recipeId, forceRefresh: false);
            if (!detail.HasValue)
            {
                LogRecipeSelection($"set-selected missing detail recipeId={recipeId} stillSelected={selectedRecipeId == recipeId}.");
                if (selectedRecipeId == recipeId)
                    Refresh(force: true);
                return;
            }

            if (selectedRecipeId != recipeId)
            {
                LogRecipeSelection($"set-selected aborted because selection changed while loading requested={recipeId} current={selectedRecipeId?.ToString(CultureInfo.InvariantCulture) ?? "None"}.");
                return;
            }

            LogRecipeSelection(
                $"set-selected detail-ready recipeId={recipeId} name='{detail.Value.Name}' " +
                $"inputCount={(detail.Value.Inputs != null ? detail.Value.Inputs.Count : 0)}.");
            await RefreshPreviewAsync();
            if (selectedRecipeId == recipeId)
            {
                Refresh(force: true);
                LogRecipeSelection($"set-selected complete recipeId={recipeId}.");
            }
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
            if (TryGetLearnedRecipe(recipeId, out var learnedRecipe))
                return learnedRecipe;

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

        private static bool TryGetLearnedRecipe(int recipeId, out LearnedPillRecipeModel recipe)
        {
            var recipes = ClientRuntime.Alchemy.Recipes;
            if (recipes != null)
            {
                for (var i = 0; i < recipes.Length; i++)
                {
                    if (recipes[i].PillRecipeTemplateId == recipeId)
                    {
                        recipe = recipes[i];
                        return true;
                    }
                }
            }

            recipe = default;
            return false;
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

        private IReadOnlyList<CraftInputPanelView.SlotState> BuildRequiredInputSlotStates(
            IReadOnlyList<PillRecipeInputModel> requiredInputs,
            PracticeSessionModel? activeSession)
        {
            if (requiredInputs == null || requiredInputs.Count == 0)
                return Array.Empty<CraftInputPanelView.SlotState>();

            var states = new List<CraftInputPanelView.SlotState>(requiredInputs.Count);
            for (var i = 0; i < requiredInputs.Count; i++)
            {
                var input = requiredInputs[i];
                var presentation = panelView != null
                    ? panelView.ResolvePresentation(input.RequiredItem)
                    : new InventoryItemPresentation(null, null, Color.white);
                var currentQuantity = ResolveAssignedQuantity(input, activeSession);
                var hasSelection = ResolveInputArmed(input, activeSession);
                states.Add(new CraftInputPanelView.SlotState(
                    input.InputId,
                    input.RequiredItem.ItemTemplateId,
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
            var hadPreviousSelection = draftState.TryGetSelection(inputId, out var previousSelection) && previousSelection.Armed;
            var previousAssignedQuantity = hadPreviousSelection
                ? Math.Max(0, previousSelection.AssignedQuantity)
                : 0;
            var result = draftState.TryAssignInventoryItemToInput(inputs, inputId, item);
            if (!result.Success)
                return false;

            if (result.RequiresQuantityPrompt &&
                TryResolveInput(inputs, inputId, out var input) &&
                draftState.TryGetSelection(inputId, out var selection))
            {
                ShowInputQuantityPopup(input, selection, hadPreviousSelection, previousAssignedQuantity);
            }

            return true;
        }

        private bool AreRequiredInputsReady(PillRecipeDetailModel detail, PracticeSessionModel? activeSession)
        {
            return draftState.AreRequiredInputsReady(detail, activeSession, GetConsumedItems(), GetInventoryItems());
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

        private float ResolveCraftingResultProgressFillAmount(PracticeSessionModel? displaySession)
        {
            if (!displaySession.HasValue)
                return 0f;

            if (displaySession.Value.PracticeState == 3)
                return 1f;

            return ResolveSessionProgress(displaySession.Value, out _);
        }

        private string ResolveCraftingResultProgressLabel(PracticeSessionModel? displaySession)
        {
            if (!displaySession.HasValue)
                return string.Empty;

            if (displaySession.Value.PracticeState == 3)
                return "100%  00:00";

            var progress = string.Concat(
                Mathf.RoundToInt(ResolveSessionProgress(displaySession.Value, out var remainingSeconds) * 100f)
                    .ToString(CultureInfo.InvariantCulture),
                "%");

            return string.Concat(progress, "  ", FormatDuration(remainingSeconds));
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
                return Math.Max(0, preview.Value.RequestedCraftCount);
            }

            return 0;
        }

        private string ResolvePracticeStatusText(PracticeSessionModel? displaySession)
        {
            if (!displaySession.HasValue)
                return "San sang luyen che";

            if (displaySession.Value.PracticeState == 3)
                return "Dang doi ket qua luyen che";

            return displaySession.Value.IsPaused
                ? "Dang tam dung"
                : "Dang luyen che";
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

        private static string ResolveSuccessRateText(
            PracticeSessionModel? displaySession,
            PillRecipeDetailModel detail,
            AlchemyCraftPreviewModel? preview)
        {
            var segments = displaySession.HasValue
                ? displaySession.Value.SuccessRateSegments
                : preview.HasValue
                    ? preview.Value.SuccessRateSegments
                    : null;
            if (segments != null && segments.Count > 0)
                return FormatSuccessRateSegments(segments);

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

            var craftCount = preview.HasValue
                ? Math.Max(1, preview.Value.RequestedCraftCount)
                : ResolvePreviewRequestedCraftCount(detail);
            return FormatDuration(detail.CraftDurationSeconds * craftCount);
        }

        private IReadOnlyList<CraftInputPanelView.SlotState> BuildOptionalInputSlotStates(
            IReadOnlyList<PillRecipeInputModel> optionalInputs,
            PracticeSessionModel? activeSession)
        {
            if (optionalInputs == null || optionalInputs.Count == 0)
                return Array.Empty<CraftInputPanelView.SlotState>();

            var states = new List<CraftInputPanelView.SlotState>(optionalInputs.Count);
            for (var i = 0; i < optionalInputs.Count; i++)
            {
                var input = optionalInputs[i];
                var presentation = panelView != null
                    ? panelView.ResolvePresentation(input.RequiredItem)
                    : new InventoryItemPresentation(null, null, Color.white);
                var currentQuantity = ResolveAssignedQuantity(input, activeSession);
                var hasSelection = ResolveInputArmed(input, activeSession);
                states.Add(new CraftInputPanelView.SlotState(
                    input.InputId,
                    input.RequiredItem.ItemTemplateId,
                    presentation,
                    currentQuantity,
                    Math.Max(1, input.RequiredQuantity),
                    hasSelection,
                    activeSession.HasValue,
                    showEmptyIcon: true));
            }

            return states;
        }

        private void ShowInputQuantityPopup(
            PillRecipeInputModel input,
            AlchemyCraftDraftState.SelectionSnapshot selection,
            bool restoreHadSelection,
            int restoreAssignedQuantity)
        {
            if (panelView == null || WorldModalUIManager.Instance == null)
                return;

            quantityPopupMode = QuantityPopupMode.InputQuantity;
            quantityPopupInputId = input.InputId;
            quantityPopupRestoreHadSelection = restoreHadSelection;
            quantityPopupRestoreAssignedQuantity = Math.Max(0, restoreAssignedQuantity);
            var maxQuantity = Math.Max(
                Math.Max(0, selection.AssignedQuantity),
                Math.Max(0, selection.AssignedQuantity) + ResolveInventoryQuantity(input.RequiredItem.ItemTemplateId));
            HideItemOptionsPopup(force: true);
            HideRecipeTooltip(force: true);
            panelView.ShowQuantityPopup(
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
                case QuantityPopupMode.InputQuantity:
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
            var mode = quantityPopupMode;
            var inputId = quantityPopupInputId;
            var restoreHadSelection = quantityPopupRestoreHadSelection;
            var restoreAssignedQuantity = quantityPopupRestoreAssignedQuantity;
            HideQuantityPopup(force: true);

            if (mode == QuantityPopupMode.InputQuantity && inputId.HasValue)
            {
                if (restoreHadSelection)
                    draftState.SetAssignedQuantity(inputId.Value, restoreAssignedQuantity);
                else
                    draftState.ClearInput(inputId.Value);

                _ = RefreshPreviewAsync();
            }

            Refresh(force: true);
        }

        private void HideQuantityPopup(bool force)
        {
            quantityPopupMode = QuantityPopupMode.None;
            quantityPopupInputId = null;
            quantityPopupRestoreHadSelection = false;
            quantityPopupRestoreAssignedQuantity = 0;
            panelView?.HideQuantityPopup(force);
        }

        private void HideItemOptionsPopup(bool force)
        {
            popupRecipeId = null;
            popupInventoryPlayerItemId = null;
            panelView?.HideItemOptionsPopup(force);
        }

        private void ClearInputViews()
        {
            panelView?.ClearInputs();
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

            panelView?.SetSelectedRecipeInteractionLocked(false);

            HideRecipeTooltip(force: true);
            HideInventoryTooltip(force: true);
            HideQuantityPopup(force: true);
        }

        private void HideRecipeTooltip(bool force)
        {
            panelView?.HideRecipeTooltip(force);
        }

        private void HideInventoryTooltip(bool force)
        {
            panelView?.HideInventoryTooltip(force);
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
                BuildSuccessRateSegmentSnapshot(session.Value.SuccessRateSegments),
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
                preview.Value.RequestedCraftCount.ToString(CultureInfo.InvariantCulture),
                ":",
                preview.Value.MaxCraftableCount.ToString(CultureInfo.InvariantCulture),
                ":",
                preview.Value.BoostedCraftCount.ToString(CultureInfo.InvariantCulture),
                ":",
                BuildSuccessRateSegmentSnapshot(preview.Value.SuccessRateSegments),
                ":",
                preview.Value.EffectiveSuccessRate.ToString("0.####", CultureInfo.InvariantCulture),
                ":",
                preview.Value.FailureReason ?? string.Empty);
        }

        private static string FormatSuccessRateSegments(IReadOnlyList<AlchemyCraftRateSegmentModel> segments)
        {
            if (segments == null || segments.Count == 0)
                return string.Empty;

            var normalizedSegments = segments
                .Where(static segment => segment.Count > 0)
                .ToArray();
            if (normalizedSegments.Length == 0)
                return string.Empty;

            if (normalizedSegments.Length == 1)
                return FormatPercent(normalizedSegments[0].SuccessRate);

            return string.Join(
                ", ",
                normalizedSegments.Select(segment => string.Concat(
                    FormatPercent(segment.SuccessRate),
                    " x",
                    segment.Count.ToString(CultureInfo.InvariantCulture))));
        }

        private static string BuildSuccessRateSegmentSnapshot(IReadOnlyList<AlchemyCraftRateSegmentModel> segments)
        {
            if (segments == null || segments.Count == 0)
                return string.Empty;

            return string.Join(
                ",",
                segments
                    .Where(static segment => segment.Count > 0)
                    .Select(segment => string.Concat(
                        NormalizeRate(segment.SuccessRate).ToString("0.######", CultureInfo.InvariantCulture),
                        "x",
                        segment.Count.ToString(CultureInfo.InvariantCulture))));
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

        private void LogRecipeSelection(string message)
        {
            if (!logRecipeSelectionDiagnostics)
                return;

            ClientLog.Info($"[CraftRecipeSelect] {message}");
        }

        private static string DescribeRecipe(LearnedPillRecipeModel recipe)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "id={0} code='{1}' name='{2}' resultItem={3}",
                recipe.PillRecipeTemplateId,
                recipe.Code ?? string.Empty,
                recipe.Name ?? string.Empty,
                recipe.ResultPill.ItemTemplateId);
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
