using System;
using System.Globalization;
using GameShared.Messages;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Features.MartialArts.Application;
using PhamNhanOnline.Client.UI.Common;
using PhamNhanOnline.Client.UI.MartialArts;
using TMPro;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.World
{
    public sealed partial class WorldMartialArtPanelController
    {
        private const string MissingCharacterName = "Chua co nhan vat";
        private const string MissingRealmName = "Chua co canh gioi";
        private const string MissingCultivationText = "0/0";
        private const string MissingUnallocatedPotentialText = "0";

        private void ApplyLoadedState(
            ClientMartialArtState martialArtState,
            CharacterBaseStatsModel? baseStats,
            CharacterCurrentStateModel? currentState,
            bool force)
        {
            var selectedCharacter = ClientRuntime.Character.SelectedCharacter;
            var activeMartialArt = TryGetActiveMartialArt(martialArtState);
            var ownedMartialArts = martialArtState.OwnedMartialArts ?? Array.Empty<PlayerMartialArtModel>();
            var preview = martialArtState.CultivationPreview;
            var breakthroughAvailable = baseStats.HasValue && CanAttemptBreakthrough(baseStats.Value);
            var isCultivating = currentState.HasValue &&
                                (currentState.Value.CurrentState == CharacterStateCultivating ||
                                 currentState.Value.CurrentState == CharacterStatePracticing);
            var canChangeActive = CanChangeActiveMartialArt(currentState);

            ApplyCharacterSummary(selectedCharacter, currentState, baseStats, force: force);

            if (activeMartialArt.HasValue)
            {
                var presentation = presentationCatalog != null
                    ? presentationCatalog.Resolve(activeMartialArt.Value)
                    : new MartialArtPresentation(null);
                activeMartialArtSlotView?.SetItem(activeMartialArt.Value, presentation, force: true);
                activeMartialArtSlotView?.SetSelected(
                    popupTargetsActiveSlot &&
                    popupMartialArtId.HasValue &&
                    popupMartialArtId.Value == activeMartialArt.Value.MartialArtId,
                    force: true);
                activeMartialArtSlotView?.SetDragEnabled(canChangeActive);
            }
            else
            {
                activeMartialArtSlotView?.Clear(force: true);
            }

            if (martialArtListView != null)
            {
                martialArtListView.SetItems(
                    ownedMartialArts,
                    popupTargetsActiveSlot ? null : popupMartialArtId,
                    presentationCatalog,
                    force: true);
            }

            ApplyEstimate(activeMartialArt, preview, force);
            ApplyBreakthroughChance(baseStats, force);
            ApplyButtons(baseStats, currentState, activeMartialArt, preview, breakthroughAvailable, isCultivating, force);

            var status = ResolveStatusText(activeMartialArt, preview, baseStats, currentState, breakthroughAvailable, isCultivating);
            ApplyText(statusText, status, force: true);
        }

        private void ApplyMissingState(bool force)
        {
            var selectedCharacter = ClientRuntime.IsInitialized ? ClientRuntime.Character.SelectedCharacter : null;
            var currentState = ClientRuntime.IsInitialized ? ClientRuntime.Character.CurrentState : null;
            ApplyCharacterSummary(selectedCharacter, currentState, stats: null, force: force);
            ApplyText(statusText, lastStatusMessage, force);

            activeMartialArtSlotView?.Clear(force: true);
            martialArtListView?.Clear(force: true);

            if (estimateRoot != null)
                estimateRoot.SetActive(false);

            if (breakthroughRoot != null)
                breakthroughRoot.SetActive(false);

            ApplyText(estimateText, string.Empty, force);
            ApplyText(breakthroughChanceText, string.Empty, force);
            SetButtonVisible(startCultivationButton, false, force: force);
            SetButtonVisible(stopCultivationButton, false, force: force);
            SetButtonVisible(breakthroughButton, false, force: force);
        }

        private void ApplyEstimate(PlayerMartialArtModel? activeMartialArt, CultivationPreviewModel? preview, bool force)
        {
            var hasPreview = activeMartialArt.HasValue && preview.HasValue;
            if (estimateRoot != null)
                estimateRoot.SetActive(hasPreview);

            if (!hasPreview)
            {
                ApplyText(estimateText, string.Empty, force);
                return;
            }

            var hourlyExp = Math.Max(0d, preview.Value.EstimatedCultivationPerMinute) * 60d;
            ApplyText(
                estimateText,
                string.Format(CultureInfo.InvariantCulture, "+{0:0.##} exp/h", hourlyExp),
                force);
        }

        private void ApplyBreakthroughChance(CharacterBaseStatsModel? baseStats, bool force)
        {
            var text = baseStats.HasValue
                ? string.Format(CultureInfo.InvariantCulture, "{0:0.##}%", Math.Max(0d, baseStats.Value.BreakthroughChancePercent))
                : string.Empty;
            ApplyText(breakthroughChanceText, text, force);
        }

        private void ApplyButtons(
            CharacterBaseStatsModel? baseStats,
            CharacterCurrentStateModel? currentState,
            PlayerMartialArtModel? activeMartialArt,
            CultivationPreviewModel? preview,
            bool breakthroughAvailable,
            bool isCultivating,
            bool force)
        {
            var showStart = !actionInFlight && CanStartCultivation(activeMartialArt, preview, baseStats, currentState);
            var showStop = !actionInFlight &&
                           currentState.HasValue &&
                           currentState.Value.CurrentState == CharacterStateCultivating;
            var showBreakthrough = !actionInFlight && breakthroughAvailable;

            if (breakthroughRoot != null)
                breakthroughRoot.SetActive(showBreakthrough);

            SetButtonVisible(startCultivationButton, showStart, force: force);
            SetButtonVisible(stopCultivationButton, showStop, force: force);
            SetButtonVisible(breakthroughButton, showBreakthrough, force: force);
        }

        private string ResolveStatusText(
            PlayerMartialArtModel? activeMartialArt,
            CultivationPreviewModel? preview,
            CharacterBaseStatsModel? baseStats,
            CharacterCurrentStateModel? currentState,
            bool breakthroughAvailable,
            bool isCultivating)
        {
            if (actionInFlight && actionKind == PanelActionKind.Breakthrough)
                return statusBreakthroughInProgressText;

            if (actionInFlight && !string.IsNullOrWhiteSpace(lastStatusMessage))
                return lastStatusMessage;

            if (breakthroughAvailable)
                return statusBreakthroughRequiredText;

            if (isCultivating)
                return statusCultivatingText;

            if (!activeMartialArt.HasValue)
                return statusNoActiveMartialArtText;

            if (preview.HasValue && preview.Value.BlockedReason != MessageCode.None)
                return ResolveBlockedReasonText(preview.Value.BlockedReason, baseStats);

            if (!string.IsNullOrWhiteSpace(lastStatusMessage))
                return lastStatusMessage;

            return string.Empty;
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
                        ? "Da tu luyen toi dinh phong gap binh canh can dot pha"
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
                currentState.Value.CurrentState == CharacterStatePracticing ||
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

        private static bool TryFindOwnedMartialArtById(
            PlayerMartialArtModel[] martialArts,
            int martialArtId,
            out PlayerMartialArtModel martialArt)
        {
            if (martialArts != null)
            {
                for (var i = 0; i < martialArts.Length; i++)
                {
                    if (martialArts[i].MartialArtId != martialArtId)
                        continue;

                    martialArt = martialArts[i];
                    return true;
                }
            }

            martialArt = default;
            return false;
        }

        private void ApplyCharacterSummary(
            CharacterModel? selectedCharacter,
            CharacterCurrentStateModel? currentState,
            CharacterBaseStatsModel? stats,
            bool force)
        {
            if (characterSummaryView == null)
                return;

            var displayName = selectedCharacter.HasValue
                ? ResolveCharacterName(selectedCharacter.Value.Name)
                : MissingCharacterName;

            characterSummaryView.SetCharacterName(displayName, force);
            characterSummaryView.SetLifespanEndUnixMs(currentState.HasValue ? currentState.Value.LifespanEndUnixMs : null, force);

            if (!stats.HasValue)
            {
                characterSummaryView.SetStats("-", "-", "-", "-", "-", "-", force);
                characterSummaryView.SetRealmProgress(
                    MissingRealmName,
                    MissingCultivationText,
                    MissingUnallocatedPotentialText,
                    0f,
                    force);
                return;
            }

            var value = stats.Value;
            characterSummaryView.SetStats(
                value.FinalHp.ToString(CultureInfo.InvariantCulture),
                value.FinalMp.ToString(CultureInfo.InvariantCulture),
                value.FinalAttack.ToString(CultureInfo.InvariantCulture),
                value.FinalSpeed.ToString(CultureInfo.InvariantCulture),
                value.FinalLuck.ToString("0.##", CultureInfo.InvariantCulture),
                value.FinalSense.ToString(CultureInfo.InvariantCulture),
                force);
            characterSummaryView.SetRealmProgress(
                ResolveRealmDisplayName(value),
                BuildCultivationProgress(value),
                value.UnallocatedPotential.ToString(CultureInfo.InvariantCulture),
                ResolveCultivationFillAmount(value),
                force);
        }

        private static string ResolveCharacterName(string rawName)
        {
            return string.IsNullOrWhiteSpace(rawName) ? "-" : rawName.Trim();
        }

        private static string ResolveRealmDisplayName(CharacterBaseStatsModel stats)
        {
            if (!string.IsNullOrWhiteSpace(stats.RealmDisplayName))
                return stats.RealmDisplayName.Trim();

            return stats.RealmTemplateId > 0
                ? string.Format(CultureInfo.InvariantCulture, "Canh gioi {0}", stats.RealmTemplateId)
                : MissingRealmName;
        }

        private static string BuildCultivationProgress(CharacterBaseStatsModel stats)
        {
            var maxCultivation = Math.Max(0L, stats.RealmMaxCultivation);
            var currentCultivation = Math.Max(0L, stats.Cultivation);
            return string.Format(CultureInfo.InvariantCulture, "{0}/{1}", currentCultivation, maxCultivation);
        }

        private static float ResolveCultivationFillAmount(CharacterBaseStatsModel stats)
        {
            var maxCultivation = Math.Max(0L, stats.RealmMaxCultivation);
            if (maxCultivation <= 0L)
                return 0f;

            var currentCultivation = Math.Max(0L, stats.Cultivation);
            return Mathf.Clamp01((float)currentCultivation / maxCultivation);
        }

        private string BuildSnapshot(
            ClientMartialArtState martialArtState,
            CharacterBaseStatsModel? baseStats,
            CharacterCurrentStateModel? currentState)
        {
            var selectedCharacter = ClientRuntime.Character.SelectedCharacter;
            var preview = martialArtState.CultivationPreview;
            return string.Join(
                "|",
                selectedCharacter.HasValue ? selectedCharacter.Value.Name ?? string.Empty : string.Empty,
                currentState.HasValue && currentState.Value.LifespanEndUnixMs.HasValue
                    ? currentState.Value.LifespanEndUnixMs.Value.ToString(CultureInfo.InvariantCulture)
                    : string.Empty,
                martialArtState.HasLoadedMartialArts ? "1" : "0",
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
                ((int)value.BlockedReason).ToString(CultureInfo.InvariantCulture));
        }

        private static string BuildBaseStatsSnapshot(CharacterBaseStatsModel? baseStats)
        {
            if (!baseStats.HasValue)
                return string.Empty;

            var value = baseStats.Value;
            return string.Join(
                ":",
                value.FinalHp.ToString(CultureInfo.InvariantCulture),
                value.FinalMp.ToString(CultureInfo.InvariantCulture),
                value.FinalAttack.ToString(CultureInfo.InvariantCulture),
                value.FinalSpeed.ToString(CultureInfo.InvariantCulture),
                value.FinalSense.ToString(CultureInfo.InvariantCulture),
                value.FinalLuck.ToString("0.####", CultureInfo.InvariantCulture),
                value.Cultivation.ToString(CultureInfo.InvariantCulture),
                value.RealmMaxCultivation.ToString(CultureInfo.InvariantCulture),
                value.UnallocatedPotential.ToString(CultureInfo.InvariantCulture),
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

        private static void SetButtonVisible(UIButtonView button, bool visible, bool force)
        {
            if (button == null)
                return;

            if (button.gameObject.activeSelf != visible)
                button.gameObject.SetActive(visible);

            button.SetInteractable(visible, force: force || !visible);
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
