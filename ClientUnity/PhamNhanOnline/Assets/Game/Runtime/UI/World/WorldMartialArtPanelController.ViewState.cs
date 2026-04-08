using System;
using System.Globalization;
using GameShared.Messages;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Features.MartialArts.Application;
using PhamNhanOnline.Client.UI.MartialArts;
using TMPro;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.World
{
    public sealed partial class WorldMartialArtPanelController
    {
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
                    martialArts[i].Category ?? string.Empty,
                    ":",
                    martialArts[i].Description ?? string.Empty);
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
