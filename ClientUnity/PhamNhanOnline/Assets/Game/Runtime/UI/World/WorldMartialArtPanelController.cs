using System;
using System.Globalization;
using GameShared.Messages;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Features.MartialArts.Application;
using PhamNhanOnline.Client.Network.Session;
using PhamNhanOnline.Client.UI.MartialArts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.World
{
    public sealed class WorldMartialArtPanelController : MonoBehaviour
    {
        private const int CharacterStateIdle = 0;
        private const int CharacterStateLifespanExpired = 2;
        private const int CharacterStateCultivating = 3;

        private enum PanelActionKind
        {
            None = 0,
            SetActive = 1,
            ClearActive = 2,
            StartCultivation = 3,
            StopCultivation = 4,
            Breakthrough = 5
        }

        [Header("References")]
        [SerializeField] private TMP_Text ownedCountText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private MartialArtPresentationCatalog presentationCatalog;
        [SerializeField] private ActiveMartialArtSlotView activeMartialArtSlotView;
        [SerializeField] private MartialArtListView martialArtListView;
        [SerializeField] private GameObject estimateRoot;
        [SerializeField] private Image cultivationProgressFillImage;
        [SerializeField] private TMP_Text cultivationProgressText;
        [SerializeField] private TMP_Text estimateText;
        [SerializeField] private TMP_Text estimateDetailText;
        [SerializeField] private Button startCultivationButton;
        [SerializeField] private TMP_Text startCultivationButtonText;
        [SerializeField] private GameObject breakthroughRoot;
        [SerializeField] private TMP_Text breakthroughChanceText;
        [SerializeField] private Button breakthroughButton;
        [SerializeField] private TMP_Text breakthroughButtonText;

        [Header("Behavior")]
        [SerializeField] private bool autoLoadMissingCharacterData = true;
        [SerializeField] private bool autoLoadMissingMartialArts = true;
        [SerializeField] private float reloadRetryCooldownSeconds = 2f;

        [Header("Display Text")]
        [SerializeField] private string missingOwnedCountText = "Cong phap: 0";
        [SerializeField] private string loadingOwnedCountText = "Dang tai cong phap...";
        [SerializeField] private string missingStatusText = "Chua tai danh sach cong phap.";
        [SerializeField] private string noActiveMartialArtText = "Keo 1 cong phap da hoc vao o chu tu de xem uoc tinh.";
        [SerializeField] private string startCultivationIdleText = "Tu luyen";
        [SerializeField] private string stopCultivationIdleText = "Dung tu luyen";
        [SerializeField] private string cultivationActionInFlightText = "Dang gui...";
        [SerializeField] private string breakthroughIdleText = "Dot pha";
        [SerializeField] private string breakthroughInFlightText = "Dang dot pha...";

        private Guid? lastRequestedCharacterId;
        private float lastCharacterReloadAttemptTime = float.NegativeInfinity;
        private float lastMartialArtReloadAttemptTime = float.NegativeInfinity;
        private bool characterReloadInFlight;
        private bool martialArtReloadInFlight;
        private bool actionInFlight;
        private PanelActionKind actionKind;
        private string lastStatusMessage = string.Empty;
        private string lastSnapshot = string.Empty;

        private void Awake()
        {
            if (activeMartialArtSlotView != null)
                activeMartialArtSlotView.MartialArtDropped += HandleMartialArtDropped;

            if (martialArtListView != null)
                martialArtListView.ActiveMartialArtDroppedToList += HandleActiveMartialArtDroppedToList;

            if (startCultivationButton != null)
            {
                startCultivationButton.onClick.RemoveListener(HandleCultivationButtonClicked);
                startCultivationButton.onClick.AddListener(HandleCultivationButtonClicked);
            }

            if (breakthroughButton != null)
            {
                breakthroughButton.onClick.RemoveListener(HandleBreakthroughButtonClicked);
                breakthroughButton.onClick.AddListener(HandleBreakthroughButtonClicked);
            }
        }

        private void OnEnable()
        {
            RefreshPanel(force: true);
            TryReloadMissingData();
        }

        private void Update()
        {
            if (!isActiveAndEnabled)
                return;

            RefreshPanel(force: false);
            TryReloadMissingData();
        }

        private void OnDestroy()
        {
            if (activeMartialArtSlotView != null)
                activeMartialArtSlotView.MartialArtDropped -= HandleMartialArtDropped;

            if (martialArtListView != null)
                martialArtListView.ActiveMartialArtDroppedToList -= HandleActiveMartialArtDroppedToList;

            if (startCultivationButton != null)
                startCultivationButton.onClick.RemoveListener(HandleCultivationButtonClicked);

            if (breakthroughButton != null)
                breakthroughButton.onClick.RemoveListener(HandleBreakthroughButtonClicked);
        }

        private void RefreshPanel(bool force)
        {
            if (!ClientRuntime.IsInitialized)
            {
                ApplyMissingState(force);
                return;
            }

            var martialArtState = ClientRuntime.MartialArts;
            var baseStats = ClientRuntime.Character.BaseStats;
            var currentState = ClientRuntime.Character.CurrentState;
            if (!martialArtState.HasLoadedMartialArts)
            {
                ApplyMissingState(force);
                return;
            }

            var snapshot = BuildSnapshot(martialArtState, baseStats, currentState);
            if (!force && string.Equals(lastSnapshot, snapshot, StringComparison.Ordinal))
                return;

            lastSnapshot = snapshot;
            ApplyLoadedState(martialArtState, baseStats, currentState, force: true);
        }

        private void ApplyLoadedState(
            ClientMartialArtState martialArtState,
            CharacterBaseStatsModel? baseStats,
            CharacterCurrentStateModel? currentState,
            bool force)
        {
            var totalOwnedCount = martialArtState.OwnedMartialArts != null ? martialArtState.OwnedMartialArts.Length : 0;
            ApplyText(
                ownedCountText,
                string.Format(CultureInfo.InvariantCulture, "Cong phap: {0}", totalOwnedCount),
                force);

            var activeMartialArt = TryGetActiveMartialArt(martialArtState);
            var visibleMartialArts = BuildVisibleMartialArtList(martialArtState.OwnedMartialArts, martialArtState.ActiveMartialArtId);
            var preview = martialArtState.CultivationPreview;
            var breakthroughAvailable = baseStats.HasValue && CanAttemptBreakthrough(baseStats.Value);
            var isCultivating = currentState.HasValue && currentState.Value.CurrentState == CharacterStateCultivating;
            var canChangeActive = CanChangeActiveMartialArt(currentState);

            if (activeMartialArt.HasValue)
            {
                var presentation = presentationCatalog != null
                    ? presentationCatalog.Resolve(activeMartialArt.Value)
                    : new MartialArtPresentation(null);
                activeMartialArtSlotView?.SetItem(activeMartialArt.Value, presentation, force: true);
                activeMartialArtSlotView?.SetDragEnabled(canChangeActive);
            }
            else
            {
                activeMartialArtSlotView?.Clear(force: true);
            }

            if (martialArtListView != null)
                martialArtListView.SetItems(visibleMartialArts, null, presentationCatalog, force: true);

            ApplyCultivationProgress(baseStats, force);
            ApplyEstimate(activeMartialArt, preview, force);
            ApplyActionArea(baseStats, currentState, activeMartialArt, preview, breakthroughAvailable, isCultivating, force);

            var status = ResolveStatusText(
                activeMartialArt,
                preview,
                baseStats,
                currentState,
                breakthroughAvailable,
                isCultivating);
            ApplyText(statusText, status, force: true);
        }

        private void ApplyMissingState(bool force)
        {
            var ownedText = loadingOwnedCountText;
            if (!ClientRuntime.IsInitialized || !martialArtReloadInFlight)
                ownedText = missingOwnedCountText;

            ApplyText(ownedCountText, ownedText, force);
            ApplyText(statusText, ResolveMissingStatusText(), force);

            activeMartialArtSlotView?.Clear(force: true);
            martialArtListView?.Clear(force: true);

            if (estimateRoot != null)
                estimateRoot.SetActive(false);

            ApplyCultivationProgress(null, force);
            ApplyCultivationButton(visible: false, interactable: false, force: force);
            ApplyBreakthroughArea(visible: false, interactable: false, chanceTextValue: string.Empty, force: force);
        }

        private void TryReloadMissingData()
        {
            if (!ClientRuntime.IsInitialized || ClientRuntime.Connection.State != ClientConnectionState.Connected)
                return;

            var selectedCharacterId = ClientRuntime.Character.SelectedCharacterId;
            if (!selectedCharacterId.HasValue)
                return;

            if (autoLoadMissingCharacterData &&
                !characterReloadInFlight &&
                (!ClientRuntime.Character.BaseStats.HasValue || !ClientRuntime.Character.CurrentState.HasValue) &&
                CanRetryReload(lastRequestedCharacterId, lastCharacterReloadAttemptTime, selectedCharacterId.Value))
            {
                _ = ReloadCharacterDataAsync(selectedCharacterId.Value);
            }

            if (autoLoadMissingMartialArts &&
                !martialArtReloadInFlight &&
                !ClientRuntime.MartialArts.HasLoadedMartialArts &&
                CanRetryReload(lastRequestedCharacterId, lastMartialArtReloadAttemptTime, selectedCharacterId.Value))
            {
                _ = ReloadMartialArtsAsync(selectedCharacterId.Value);
            }
        }

        private async System.Threading.Tasks.Task ReloadCharacterDataAsync(Guid characterId)
        {
            characterReloadInFlight = true;
            lastRequestedCharacterId = characterId;
            lastCharacterReloadAttemptTime = Time.unscaledTime;
            RefreshPanel(force: true);

            try
            {
                var result = await ClientRuntime.CharacterService.LoadCharacterDataAsync(characterId);
                if (!result.Success)
                {
                    lastStatusMessage = string.Format(
                        CultureInfo.InvariantCulture,
                        "Tai du lieu nhan vat that bai: {0}",
                        result.Code ?? MessageCode.UnknownError);
                    ClientLog.Warn($"WorldMartialArtPanelController failed to load character data: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                lastStatusMessage = string.Format(CultureInfo.InvariantCulture, "Loi tai du lieu nhan vat: {0}", ex.Message);
                ClientLog.Warn($"WorldMartialArtPanelController character reload exception: {ex.Message}");
            }
            finally
            {
                characterReloadInFlight = false;
                RefreshPanel(force: true);
            }
        }

        private async System.Threading.Tasks.Task ReloadMartialArtsAsync(Guid characterId)
        {
            martialArtReloadInFlight = true;
            lastRequestedCharacterId = characterId;
            lastMartialArtReloadAttemptTime = Time.unscaledTime;
            RefreshPanel(force: true);

            try
            {
                var result = await ClientRuntime.MartialArtService.LoadOwnedMartialArtsAsync(forceRefresh: true);
                if (!result.Success)
                {
                    lastStatusMessage = string.Format(
                        CultureInfo.InvariantCulture,
                        "Tai cong phap that bai: {0}",
                        result.Code ?? MessageCode.UnknownError);
                    ClientLog.Warn($"WorldMartialArtPanelController failed to load martial arts for {characterId}: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                lastStatusMessage = string.Format(CultureInfo.InvariantCulture, "Loi tai cong phap: {0}", ex.Message);
                ClientLog.Warn($"WorldMartialArtPanelController martial art reload exception: {ex.Message}");
            }
            finally
            {
                martialArtReloadInFlight = false;
                RefreshPanel(force: true);
            }
        }

        private void HandleMartialArtDropped(PlayerMartialArtModel martialArt)
        {
            _ = SetActiveMartialArtAsync(martialArt);
        }

        private void HandleActiveMartialArtDroppedToList(PlayerMartialArtModel martialArt)
        {
            _ = ClearActiveMartialArtAsync(martialArt);
        }

        private async System.Threading.Tasks.Task SetActiveMartialArtAsync(PlayerMartialArtModel martialArt)
        {
            if (!ClientRuntime.IsInitialized || !CanChangeActiveMartialArt(ClientRuntime.Character.CurrentState))
                return;

            if (ClientRuntime.MartialArts.ActiveMartialArtId.HasValue &&
                ClientRuntime.MartialArts.ActiveMartialArtId.Value == martialArt.MartialArtId)
            {
                return;
            }

            if (!BeginAction(PanelActionKind.SetActive, "Dang doi cong phap chu tu..."))
                return;

            try
            {
                var result = await ClientRuntime.MartialArtService.SetActiveMartialArtAsync(martialArt.MartialArtId);
                lastStatusMessage = result.Success
                    ? "Da cap nhat cong phap chu tu."
                    : string.Format(CultureInfo.InvariantCulture, "Dat cong phap that bai: {0}", result.Code ?? MessageCode.UnknownError);

                if (!result.Success)
                    ClientLog.Warn($"WorldMartialArtPanelController set active failed: {result.Message}");
            }
            catch (Exception ex)
            {
                lastStatusMessage = string.Format(CultureInfo.InvariantCulture, "Loi dat cong phap chu tu: {0}", ex.Message);
                ClientLog.Warn($"WorldMartialArtPanelController set active exception: {ex.Message}");
            }
            finally
            {
                EndAction();
            }
        }

        private async System.Threading.Tasks.Task ClearActiveMartialArtAsync(PlayerMartialArtModel martialArt)
        {
            if (!ClientRuntime.IsInitialized || !CanChangeActiveMartialArt(ClientRuntime.Character.CurrentState))
                return;

            if (!ClientRuntime.MartialArts.ActiveMartialArtId.HasValue ||
                ClientRuntime.MartialArts.ActiveMartialArtId.Value != martialArt.MartialArtId)
            {
                return;
            }

            if (!BeginAction(PanelActionKind.ClearActive, "Dang go cong phap chu tu..."))
                return;

            try
            {
                var result = await ClientRuntime.MartialArtService.SetActiveMartialArtAsync(0);
                lastStatusMessage = result.Success
                    ? "Da go cong phap chu tu."
                    : string.Format(CultureInfo.InvariantCulture, "Go cong phap that bai: {0}", result.Code ?? MessageCode.UnknownError);

                if (!result.Success)
                    ClientLog.Warn($"WorldMartialArtPanelController clear active failed: {result.Message}");
            }
            catch (Exception ex)
            {
                lastStatusMessage = string.Format(CultureInfo.InvariantCulture, "Loi go cong phap chu tu: {0}", ex.Message);
                ClientLog.Warn($"WorldMartialArtPanelController clear active exception: {ex.Message}");
            }
            finally
            {
                EndAction();
            }
        }

        private void HandleCultivationButtonClicked()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            var currentState = ClientRuntime.Character.CurrentState;
            if (currentState.HasValue && currentState.Value.CurrentState == CharacterStateCultivating)
            {
                _ = StopCultivationAsync();
                return;
            }

            _ = StartCultivationAsync();
        }

        private async System.Threading.Tasks.Task StartCultivationAsync()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            var baseStats = ClientRuntime.Character.BaseStats;
            var currentState = ClientRuntime.Character.CurrentState;
            var preview = ClientRuntime.MartialArts.CultivationPreview;
            var activeMartialArt = TryGetActiveMartialArt(ClientRuntime.MartialArts);
            if (!CanStartCultivation(activeMartialArt, preview, baseStats, currentState))
                return;

            if (!BeginAction(PanelActionKind.StartCultivation, "Dang bat dau tu luyen..."))
                return;

            try
            {
                var result = await ClientRuntime.CharacterService.StartCultivationAsync();
                lastStatusMessage = result.Success
                    ? "Nhan vat dang tu luyen."
                    : string.Format(CultureInfo.InvariantCulture, "Bat dau tu luyen that bai: {0}", result.Code ?? MessageCode.UnknownError);

                if (!result.Success)
                    ClientLog.Warn($"WorldMartialArtPanelController start cultivation failed: {result.Message}");
            }
            catch (Exception ex)
            {
                lastStatusMessage = string.Format(CultureInfo.InvariantCulture, "Loi bat dau tu luyen: {0}", ex.Message);
                ClientLog.Warn($"WorldMartialArtPanelController start cultivation exception: {ex.Message}");
            }
            finally
            {
                EndAction();
            }
        }

        private async System.Threading.Tasks.Task StopCultivationAsync()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            var currentState = ClientRuntime.Character.CurrentState;
            if (!currentState.HasValue || currentState.Value.CurrentState != CharacterStateCultivating)
                return;

            if (!BeginAction(PanelActionKind.StopCultivation, "Dang dung tu luyen..."))
                return;

            try
            {
                var result = await ClientRuntime.CharacterService.StopCultivationAsync();
                lastStatusMessage = result.Success
                    ? "Da dung tu luyen."
                    : string.Format(CultureInfo.InvariantCulture, "Dung tu luyen that bai: {0}", result.Code ?? MessageCode.UnknownError);

                if (!result.Success)
                    ClientLog.Warn($"WorldMartialArtPanelController stop cultivation failed: {result.Message}");
            }
            catch (Exception ex)
            {
                lastStatusMessage = string.Format(CultureInfo.InvariantCulture, "Loi dung tu luyen: {0}", ex.Message);
                ClientLog.Warn($"WorldMartialArtPanelController stop cultivation exception: {ex.Message}");
            }
            finally
            {
                EndAction();
            }
        }

        private void HandleBreakthroughButtonClicked()
        {
            _ = BreakthroughAsync();
        }

        private async System.Threading.Tasks.Task BreakthroughAsync()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            var baseStats = ClientRuntime.Character.BaseStats;
            if (!baseStats.HasValue || !CanAttemptBreakthrough(baseStats.Value))
                return;

            if (!BeginAction(PanelActionKind.Breakthrough, "Dang thu dot pha..."))
                return;

            try
            {
                var result = await ClientRuntime.CharacterService.BreakthroughAsync();
                lastStatusMessage = result.Success
                    ? "Dot pha thanh cong."
                    : string.Format(CultureInfo.InvariantCulture, "Dot pha that bai: {0}", result.Code ?? MessageCode.UnknownError);

                if (!result.Success)
                    ClientLog.Warn($"WorldMartialArtPanelController breakthrough failed: {result.Message}");
            }
            catch (Exception ex)
            {
                lastStatusMessage = string.Format(CultureInfo.InvariantCulture, "Loi dot pha: {0}", ex.Message);
                ClientLog.Warn($"WorldMartialArtPanelController breakthrough exception: {ex.Message}");
            }
            finally
            {
                EndAction();
            }
        }

        private void ApplyCultivationProgress(CharacterBaseStatsModel? baseStats, bool force)
        {
            var currentCultivation = baseStats.HasValue ? Math.Max(0L, baseStats.Value.Cultivation) : 0L;
            var maxCultivation = baseStats.HasValue ? Math.Max(0L, baseStats.Value.RealmMaxCultivation) : 0L;
            ApplyText(
                cultivationProgressText,
                string.Format(CultureInfo.InvariantCulture, "{0}/{1}", currentCultivation, maxCultivation),
                force);

            if (cultivationProgressFillImage != null)
            {
                var fillAmount = maxCultivation > 0L
                    ? Mathf.Clamp01((float)((double)currentCultivation / maxCultivation))
                    : 0f;

                if (force || !Mathf.Approximately(cultivationProgressFillImage.fillAmount, fillAmount))
                    cultivationProgressFillImage.fillAmount = fillAmount;
            }
        }

        private void ApplyEstimate(PlayerMartialArtModel? activeMartialArt, CultivationPreviewModel? preview, bool force)
        {
            var hasPreview = activeMartialArt.HasValue && preview.HasValue;
            if (estimateRoot != null)
                estimateRoot.SetActive(hasPreview);

            if (!hasPreview)
            {
                ApplyText(estimateText, string.Empty, force);
                ApplyText(estimateDetailText, string.Empty, force);
                return;
            }

            var previewValue = preview.Value;
            ApplyText(
                estimateText,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Tu vi: +{0:0.##}/phut",
                    Math.Max(0d, previewValue.EstimatedCultivationPerMinute)),
                force);
            ApplyText(
                estimateDetailText,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Tiem nang: +{0:0.##}/phut | Qi x{1:0.##} | Linh khi {2:0.##}/phut",
                    Math.Max(0d, previewValue.EstimatedPotentialPerMinute),
                    Math.Max(0d, previewValue.QiAbsorptionRate),
                    Math.Max(0d, previewValue.SpiritualEnergyPerMinute)),
                force);
        }

        private void ApplyActionArea(
            CharacterBaseStatsModel? baseStats,
            CharacterCurrentStateModel? currentState,
            PlayerMartialArtModel? activeMartialArt,
            CultivationPreviewModel? preview,
            bool breakthroughAvailable,
            bool isCultivating,
            bool force)
        {
            if (breakthroughAvailable)
            {
                ApplyCultivationButton(visible: false, interactable: false, force: force);
                ApplyBreakthroughArea(
                    visible: true,
                    interactable: !actionInFlight,
                    chanceTextValue: baseStats.HasValue
                        ? string.Format(
                            CultureInfo.InvariantCulture,
                            "Ti le dot pha: {0:0.##}%",
                            Math.Max(0d, baseStats.Value.BreakthroughChancePercent))
                        : string.Empty,
                    force: force);
                return;
            }

            ApplyBreakthroughArea(visible: false, interactable: false, chanceTextValue: string.Empty, force: force);

            var canInteract = isCultivating
                ? !actionInFlight
                : CanStartCultivation(activeMartialArt, preview, baseStats, currentState) && !actionInFlight;
            ApplyCultivationButton(visible: true, interactable: canInteract, force: force);
        }

        private void ApplyCultivationButton(bool visible, bool interactable, bool force)
        {
            if (startCultivationButton != null)
            {
                if (startCultivationButton.gameObject.activeSelf != visible)
                    startCultivationButton.gameObject.SetActive(visible);

                if (force || startCultivationButton.interactable != interactable)
                    startCultivationButton.interactable = interactable;
            }

            ApplyText(startCultivationButtonText, visible ? ResolveCultivationButtonText() : string.Empty, force: true);
        }

        private void ApplyBreakthroughArea(bool visible, bool interactable, string chanceTextValue, bool force)
        {
            if (breakthroughRoot != null)
                breakthroughRoot.SetActive(visible);

            ApplyText(breakthroughChanceText, visible ? chanceTextValue : string.Empty, force);
            if (breakthroughButton != null)
                breakthroughButton.interactable = visible && interactable;

            var label = actionInFlight && actionKind == PanelActionKind.Breakthrough
                ? breakthroughInFlightText
                : breakthroughIdleText;
            ApplyText(breakthroughButtonText, visible ? label : string.Empty, force);
        }

        private string ResolveCultivationButtonText()
        {
            if (actionInFlight &&
                (actionKind == PanelActionKind.StartCultivation ||
                 actionKind == PanelActionKind.StopCultivation ||
                 actionKind == PanelActionKind.SetActive ||
                 actionKind == PanelActionKind.ClearActive))
            {
                return cultivationActionInFlightText;
            }

            var currentState = ClientRuntime.IsInitialized ? ClientRuntime.Character.CurrentState : null;
            var isCultivating = currentState.HasValue && currentState.Value.CurrentState == CharacterStateCultivating;
            return isCultivating ? stopCultivationIdleText : startCultivationIdleText;
        }

        private string ResolveStatusText(
            PlayerMartialArtModel? activeMartialArt,
            CultivationPreviewModel? preview,
            CharacterBaseStatsModel? baseStats,
            CharacterCurrentStateModel? currentState,
            bool breakthroughAvailable,
            bool isCultivating)
        {
            if (actionInFlight && !string.IsNullOrWhiteSpace(lastStatusMessage))
                return lastStatusMessage;

            if (currentState.HasValue && currentState.Value.CurrentState == CharacterStateLifespanExpired)
                return "Nhan vat da het tho nguyen.";

            if (breakthroughAvailable)
                return "Nhan vat da dat nguong dot pha.";

            if (isCultivating)
                return "Nhan vat dang tu luyen.";

            if (!activeMartialArt.HasValue)
                return noActiveMartialArtText;

            if (preview.HasValue && preview.Value.BlockedReason != MessageCode.None)
                return ResolveBlockedReasonText(preview.Value.BlockedReason, baseStats);

            if (!string.IsNullOrWhiteSpace(lastStatusMessage))
                return lastStatusMessage;

            return "San sang tu luyen.";
        }

        private static string ResolveBlockedReasonText(MessageCode blockedReason, CharacterBaseStatsModel? baseStats)
        {
            switch (blockedReason)
            {
                case MessageCode.CultivationRequiresPrivateHome:
                    return "Chi co the tu luyen trong dong phu rieng.";
                case MessageCode.CultivationRequiresActiveMartialArt:
                    return "Can chon cong phap chu tu.";
                case MessageCode.CultivationRealmCapReached:
                    return baseStats.HasValue && CanAttemptBreakthrough(baseStats.Value)
                        ? "Nhan vat da dat nguong dot pha."
                        : "Da dat gioi han tu vi hien tai.";
                case MessageCode.CharacterLifespanExpired:
                    return "Nhan vat da het tho nguyen.";
                case MessageCode.CharacterActionsRestricted:
                    return "Khong the thao tac luc nay.";
                default:
                    return string.Format(CultureInfo.InvariantCulture, "Khong the tu luyen: {0}", blockedReason);
            }
        }

        private bool CanStartCultivation(
            PlayerMartialArtModel? activeMartialArt,
            CultivationPreviewModel? preview,
            CharacterBaseStatsModel? baseStats,
            CharacterCurrentStateModel? currentState)
        {
            if (!activeMartialArt.HasValue || !preview.HasValue || !currentState.HasValue)
                return false;

            if (currentState.Value.CurrentState == CharacterStateCultivating ||
                currentState.Value.CurrentState == CharacterStateLifespanExpired)
            {
                return false;
            }

            if (baseStats.HasValue && CanAttemptBreakthrough(baseStats.Value))
                return false;

            return preview.Value.BlockedReason == MessageCode.None;
        }

        private static bool CanAttemptBreakthrough(CharacterBaseStatsModel stats)
        {
            return stats.HasNextRealm &&
                   stats.RealmMaxCultivation > 0L &&
                   stats.Cultivation >= stats.RealmMaxCultivation;
        }

        private static bool CanChangeActiveMartialArt(CharacterCurrentStateModel? currentState)
        {
            return currentState.HasValue && currentState.Value.CurrentState == CharacterStateIdle;
        }

        private static PlayerMartialArtModel? TryGetActiveMartialArt(ClientMartialArtState martialArtState)
        {
            PlayerMartialArtModel activeMartialArt;
            return martialArtState.TryGetActiveMartialArt(out activeMartialArt)
                ? activeMartialArt
                : null;
        }

        private static PlayerMartialArtModel[] BuildVisibleMartialArtList(
            PlayerMartialArtModel[] martialArts,
            int? activeMartialArtId)
        {
            if (martialArts == null || martialArts.Length == 0)
                return Array.Empty<PlayerMartialArtModel>();

            var visible = new System.Collections.Generic.List<PlayerMartialArtModel>(martialArts.Length);
            for (var i = 0; i < martialArts.Length; i++)
            {
                var martialArt = martialArts[i];
                if (activeMartialArtId.HasValue && martialArt.MartialArtId == activeMartialArtId.Value)
                    continue;

                visible.Add(martialArt);
            }

            return visible.ToArray();
        }

        private bool BeginAction(PanelActionKind kind, string status)
        {
            if (actionInFlight)
                return false;

            actionInFlight = true;
            actionKind = kind;
            lastStatusMessage = status ?? string.Empty;
            RefreshPanel(force: true);
            return true;
        }

        private void EndAction()
        {
            actionInFlight = false;
            actionKind = PanelActionKind.None;
            RefreshPanel(force: true);
        }

        private bool CanRetryReload(Guid? lastRequestedId, float lastAttemptTime, Guid characterId)
        {
            return lastRequestedId != characterId ||
                   Time.unscaledTime - lastAttemptTime >= reloadRetryCooldownSeconds;
        }

        private string ResolveMissingStatusText()
        {
            if (!string.IsNullOrWhiteSpace(lastStatusMessage))
                return lastStatusMessage;

            if (characterReloadInFlight || martialArtReloadInFlight)
                return "Dang tai du lieu cong phap...";

            return missingStatusText;
        }

        private string BuildSnapshot(
            ClientMartialArtState martialArtState,
            CharacterBaseStatsModel? baseStats,
            CharacterCurrentStateModel? currentState)
        {
            var preview = martialArtState.CultivationPreview;
            return string.Join(
                "|",
                martialArtState.HasLoadedMartialArts ? "1" : "0",
                martialArtState.IsLoading ? "1" : "0",
                martialArtState.ActiveMartialArtId.HasValue ? martialArtState.ActiveMartialArtId.Value.ToString(CultureInfo.InvariantCulture) : "0",
                BuildOwnedSnapshot(martialArtState.OwnedMartialArts),
                BuildPreviewSnapshot(preview),
                BuildBaseStatsSnapshot(baseStats),
                BuildCurrentStateSnapshot(currentState),
                actionInFlight ? "1" : "0",
                ((int)actionKind).ToString(CultureInfo.InvariantCulture),
                lastStatusMessage ?? string.Empty);
        }

        private static string BuildOwnedSnapshot(PlayerMartialArtModel[] martialArts)
        {
            if (martialArts == null || martialArts.Length == 0)
                return string.Empty;

            var parts = new string[martialArts.Length];
            for (var i = 0; i < martialArts.Length; i++)
            {
                parts[i] = string.Concat(
                    martialArts[i].MartialArtId.ToString(CultureInfo.InvariantCulture),
                    ":",
                    martialArts[i].CurrentStage.ToString(CultureInfo.InvariantCulture),
                    ":",
                    martialArts[i].MaxStage.ToString(CultureInfo.InvariantCulture),
                    ":",
                    martialArts[i].QiAbsorptionRate.ToString("0.####", CultureInfo.InvariantCulture),
                    ":",
                    martialArts[i].IsActive ? "1" : "0",
                    ":",
                    martialArts[i].Icon ?? string.Empty,
                    ":",
                    martialArts[i].Name ?? string.Empty,
                    ":",
                    martialArts[i].Category ?? string.Empty);
            }

            return string.Join(";", parts);
        }

        private static string BuildPreviewSnapshot(CultivationPreviewModel? preview)
        {
            if (!preview.HasValue)
                return string.Empty;

            var value = preview.Value;
            return string.Join(
                ":",
                value.ActiveMartialArtId.ToString(CultureInfo.InvariantCulture),
                value.QiAbsorptionRate.ToString("0.####", CultureInfo.InvariantCulture),
                value.SpiritualEnergyPerMinute.ToString("0.####", CultureInfo.InvariantCulture),
                value.RealmAbsorptionMultiplier.ToString("0.####", CultureInfo.InvariantCulture),
                value.EstimatedCultivationPerMinute.ToString("0.####", CultureInfo.InvariantCulture),
                value.EstimatedPotentialPerMinute.ToString("0.####", CultureInfo.InvariantCulture),
                ((int)value.BlockedReason).ToString(CultureInfo.InvariantCulture));
        }

        private static string BuildBaseStatsSnapshot(CharacterBaseStatsModel? baseStats)
        {
            if (!baseStats.HasValue)
                return string.Empty;

            var value = baseStats.Value;
            return string.Join(
                ":",
                value.ActiveMartialArtId.ToString(CultureInfo.InvariantCulture),
                value.Cultivation.ToString(CultureInfo.InvariantCulture),
                value.RealmMaxCultivation.ToString(CultureInfo.InvariantCulture),
                value.BreakthroughChancePercent.ToString("0.####", CultureInfo.InvariantCulture),
                value.HasNextRealm ? "1" : "0",
                value.RealmDisplayName ?? string.Empty);
        }

        private static string BuildCurrentStateSnapshot(CharacterCurrentStateModel? currentState)
        {
            if (!currentState.HasValue)
                return string.Empty;

            var value = currentState.Value;
            return string.Join(
                ":",
                value.CurrentState.ToString(CultureInfo.InvariantCulture),
                value.CultivationStartedUnixMs.HasValue ? value.CultivationStartedUnixMs.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
                value.LastCultivationRewardedUnixMs.HasValue ? value.LastCultivationRewardedUnixMs.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
                value.LastSavedUnixMs.ToString(CultureInfo.InvariantCulture));
        }

        private static void ApplyText(TMP_Text textComponent, string value, bool force)
        {
            if (textComponent == null)
                return;

            var normalized = value ?? string.Empty;
            if (!force && string.Equals(textComponent.text, normalized, StringComparison.Ordinal))
                return;

            textComponent.text = normalized;
        }
    }
}
