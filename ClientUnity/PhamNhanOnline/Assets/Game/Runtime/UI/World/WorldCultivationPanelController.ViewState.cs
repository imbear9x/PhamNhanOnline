using System;
using System.Globalization;
using GameShared.Messages;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Features.MartialArts.Application;
using PhamNhanOnline.Client.UI.MartialArts;

namespace PhamNhanOnline.Client.UI.World
{
    public sealed partial class WorldCultivationPanelController
    {
        private void ApplyLoadedState(
            ClientMartialArtState martialArtState,
            CharacterBaseStatsModel? baseStats,
            CharacterCurrentStateModel? currentState,
            bool force)
        {
            var activeMartialArt = TryGetActiveMartialArt(martialArtState);
            var ownedMartialArts = martialArtState.OwnedMartialArts ?? Array.Empty<PlayerMartialArtModel>();
            var preview = martialArtState.CultivationPreview;
            var breakthroughAvailable = baseStats.HasValue && CanAttemptBreakthrough(baseStats.Value);
            var isCultivating = currentState.HasValue &&
                                (currentState.Value.CurrentState == CharacterStateCultivating ||
                                 currentState.Value.CurrentState == CharacterStatePracticing);
            var canChangeActive = CanChangeActiveMartialArt(currentState);

            if (activeMartialArt.HasValue)
            {
                panelView?.SetActiveMartialArt(
                    activeMartialArt.Value,
                    popupTargetsActiveSlot &&
                    popupMartialArtId.HasValue &&
                    popupMartialArtId.Value == activeMartialArt.Value.MartialArtId,
                    canChangeActive,
                    force: true);
            }
            else
            {
                panelView?.ClearActiveMartialArt(force: true);
            }

            panelView?.SetMartialArtList(
                ownedMartialArts,
                popupTargetsActiveSlot ? null : popupMartialArtId,
                force: true);

            ApplyCultivationPreview(activeMartialArt, preview, baseStats, breakthroughAvailable, isCultivating, force);
            ApplyButtons(baseStats, currentState, activeMartialArt, preview, breakthroughAvailable, isCultivating, force);
        }

        private void ApplyMissingState(bool force)
        {
            panelView?.ClearActiveMartialArt(force: true);
            panelView?.ClearMartialArtList(force: true);
            panelView?.ClearCultivationPreview(force);
            panelView?.SetBreakthroughRootVisible(false);
            panelView?.SetStartCultivationButtonState(false, false, force: force);
            panelView?.SetStopCultivationButtonState(false, false, force: force);
            panelView?.SetBreakthroughButtonState(false, false, force: force);
        }

        private void ApplyCultivationPreview(
            PlayerMartialArtModel? activeMartialArt,
            CultivationPreviewModel? preview,
            CharacterBaseStatsModel? baseStats,
            bool breakthroughAvailable,
            bool isCultivating,
            bool force)
        {
            panelView?.SetCultivationPreview(
                activeMartialArt,
                preview,
                BuildEstimateText(activeMartialArt, preview),
                BuildBreakthroughChanceText(baseStats),
                ResolvePreviewStatusText(breakthroughAvailable, isCultivating),
                activeMartialArt.HasValue,
                force);
        }

        private static string BuildEstimateText(PlayerMartialArtModel? activeMartialArt, CultivationPreviewModel? preview)
        {
            if (!activeMartialArt.HasValue || !preview.HasValue)
                return string.Empty;

            var hourlyExp = Math.Max(0d, preview.Value.EstimatedCultivationPerMinute) * 60d;
            return string.Format(CultureInfo.InvariantCulture, "+{0:0.##} tu vi / h", hourlyExp);
        }

        private static string BuildBreakthroughChanceText(CharacterBaseStatsModel? baseStats)
        {
            return baseStats.HasValue
                ? string.Format(CultureInfo.InvariantCulture, "{0:0.##}%", Math.Max(0d, baseStats.Value.BreakthroughChancePercent))
                : string.Empty;
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
            _ = isCultivating;

            var showStart = !actionInFlight && CanStartCultivation(activeMartialArt, preview, baseStats, currentState);
            var showStop = !actionInFlight &&
                           currentState.HasValue &&
                           currentState.Value.CurrentState == CharacterStateCultivating;
            var showBreakthrough = !actionInFlight && breakthroughAvailable;

            panelView?.SetBreakthroughRootVisible(showBreakthrough);
            panelView?.SetStartCultivationButtonState(showStart, showStart, force: force);
            panelView?.SetStopCultivationButtonState(showStop, showStop, force: force);
            panelView?.SetBreakthroughButtonState(showBreakthrough, showBreakthrough, force: force);
        }

        private string ResolvePreviewStatusText(bool breakthroughAvailable, bool isCultivating)
        {
            if (!string.IsNullOrWhiteSpace(lastStatusMessage))
            {
                if (lastStatusMessage.StartsWith("Dot pha thanh cong", StringComparison.OrdinalIgnoreCase))
                    return statusBreakthroughSuccessText;

                if (lastStatusMessage.StartsWith("Dot pha that bai", StringComparison.OrdinalIgnoreCase) ||
                    lastStatusMessage.StartsWith("Loi dot pha", StringComparison.OrdinalIgnoreCase))
                {
                    return statusBreakthroughFailedText;
                }
            }

            if (breakthroughAvailable)
                return statusBreakthroughRequiredText;

            if (isCultivating)
                return statusCultivatingText;

            return statusReadyText;
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

        private string BuildSnapshot(
            ClientMartialArtState martialArtState,
            CharacterBaseStatsModel? baseStats,
            CharacterCurrentStateModel? currentState)
        {
            var preview = martialArtState.CultivationPreview;
            return string.Join(
                "|",
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
                    martialArts[i].CurrentExp.ToString(CultureInfo.InvariantCulture),
                    ":",
                    martialArts[i].ExpRequired.ToString(CultureInfo.InvariantCulture),
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
    }
}
