using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GameShared.Messages;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Network.Session;
using PhamNhanOnline.Client.UI.Potential;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PhamNhanOnline.Client.UI.World
{
    public sealed class WorldPotentialPanelController : MonoBehaviour
    {
        private static readonly PotentialAllocationTarget[] SupportedTargets =
        {
            PotentialAllocationTarget.BaseHp,
            PotentialAllocationTarget.BaseMp,
            PotentialAllocationTarget.BaseAttack,
            PotentialAllocationTarget.BaseSpeed,
            PotentialAllocationTarget.BaseFortune,
            PotentialAllocationTarget.BaseSpiritualSense
        };

        [Header("Header")]
        [SerializeField] private TMP_Text realmNameText;
        [SerializeField] private TMP_Text cultivationProgressText;
        [SerializeField] private TMP_Text unallocatedPotentialText;
        [SerializeField] private TMP_Text statusText;

        [Header("Potential Rows")]
        [SerializeField] private RectTransform statsPanelBounds;
        [SerializeField] private PotentialUpgradeRowListView rowListView;
        [SerializeField] private PotentialUpgradeOptionsPopupView optionsPopupView;
        [SerializeField] private PotentialStatPresentationCatalog presentationCatalog;

        [Header("Behavior")]
        [SerializeField] private int maxVisibleUpgradeOptions = 3;
        [SerializeField] private bool autoLoadMissingCharacterData = true;
        [SerializeField] private float reloadRetryCooldownSeconds = 2f;

        [Header("Display Text")]
        [SerializeField] private string missingRealmName = "Chua co canh gioi";
        [SerializeField] private string missingCultivationText = "0/0";
        [SerializeField] private string missingUnallocatedPotentialText = "Tiem nang: 0";

        private Guid? lastRequestedCharacterId;
        private float lastReloadAttemptTime = float.NegativeInfinity;
        private bool reloadInFlight;
        private bool actionInFlight;
        private string lastStatusMessage = string.Empty;
        private string lastSnapshot = string.Empty;
        private PotentialUpgradeRowView popupRow;

        private void Awake()
        {
            if (rowListView != null)
            {
                rowListView.RowClicked += HandleRowClicked;
                rowListView.RowHovered += HandleRowHovered;
                rowListView.RowHoverExited += HandleRowHoverExited;
            }

            if (statsPanelBounds == null)
                statsPanelBounds = transform as RectTransform;
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
            UpdatePopupVisibility();
        }

        private void OnDestroy()
        {
            if (rowListView != null)
            {
                rowListView.RowClicked -= HandleRowClicked;
                rowListView.RowHovered -= HandleRowHovered;
                rowListView.RowHoverExited -= HandleRowHoverExited;
            }
        }

        private void RefreshPanel(bool force)
        {
            if (!ClientRuntime.IsInitialized)
            {
                ApplyMissingState(force);
                return;
            }

            var baseStats = ClientRuntime.Character.BaseStats;
            if (!baseStats.HasValue)
            {
                ApplyMissingState(force);
                return;
            }

            var stats = baseStats.Value;
            var snapshot = BuildSnapshot(stats);
            if (!force && string.Equals(lastSnapshot, snapshot, StringComparison.Ordinal))
                return;

            lastSnapshot = snapshot;

            ApplyText(realmNameText, ResolveRealmDisplayName(stats), force: true);
            ApplyText(cultivationProgressText, BuildCultivationProgress(stats), force: true);
            ApplyText(
                unallocatedPotentialText,
                string.Format(CultureInfo.InvariantCulture, "Tiem nang chua dung: {0}", stats.UnallocatedPotential),
                force: true);
            ApplyText(statusText, lastStatusMessage, force: true);

            if (rowListView != null)
                rowListView.SetEntries(BuildRowEntries(stats), force: true);

            if (popupRow != null && optionsPopupView != null && optionsPopupView.IsVisible)
                ShowOptionsForRow(popupRow, stats);
        }

        private void ApplyMissingState(bool force)
        {
            ApplyText(realmNameText, reloadInFlight ? "Dang tai canh gioi..." : missingRealmName, force);
            ApplyText(cultivationProgressText, missingCultivationText, force);
            ApplyText(unallocatedPotentialText, missingUnallocatedPotentialText, force);
            ApplyText(statusText, lastStatusMessage, force);

            if (rowListView != null)
                rowListView.Clear(force: true);

            if (optionsPopupView != null)
                optionsPopupView.Hide(force: true);
        }

        private void TryReloadMissingData()
        {
            if (!autoLoadMissingCharacterData || reloadInFlight || !ClientRuntime.IsInitialized)
                return;

            if (ClientRuntime.Connection.State != ClientConnectionState.Connected)
                return;

            if (ClientRuntime.Character.BaseStats.HasValue)
                return;

            var selectedCharacterId = ClientRuntime.Character.SelectedCharacterId;
            if (!selectedCharacterId.HasValue)
                return;

            if (lastRequestedCharacterId == selectedCharacterId &&
                Time.unscaledTime - lastReloadAttemptTime < reloadRetryCooldownSeconds)
            {
                return;
            }

            _ = ReloadCharacterDataAsync(selectedCharacterId.Value);
        }

        private async System.Threading.Tasks.Task ReloadCharacterDataAsync(Guid characterId)
        {
            reloadInFlight = true;
            lastRequestedCharacterId = characterId;
            lastReloadAttemptTime = Time.unscaledTime;
            RefreshPanel(force: true);

            try
            {
                var result = await ClientRuntime.CharacterService.LoadCharacterDataAsync(characterId);
                if (!result.Success)
                {
                    lastStatusMessage = string.Format(
                        CultureInfo.InvariantCulture,
                        "Tai du lieu tiem nang that bai: {0}",
                        result.Code ?? MessageCode.UnknownError);
                    ClientLog.Warn($"WorldPotentialPanelController failed to load character data: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                lastStatusMessage = string.Format(CultureInfo.InvariantCulture, "Loi tai du lieu: {0}", ex.Message);
                ClientLog.Warn($"WorldPotentialPanelController reload exception: {ex.Message}");
            }
            finally
            {
                reloadInFlight = false;
                RefreshPanel(force: true);
            }
        }

        private void HandleRowHovered(PotentialUpgradeRowView row)
        {
        }

        private void HandleRowHoverExited(PotentialUpgradeRowView row)
        {
        }

        private void UpdatePopupVisibility()
        {
            if (optionsPopupView == null || !optionsPopupView.IsVisible)
                return;

            if (DidClickBlankSpaceInsideStatsPanel())
                HideOptionsPopup();
        }

        private void HandleRowClicked(PotentialUpgradeRowView row)
        {
            if (row == null || !ClientRuntime.IsInitialized)
                return;

            if (optionsPopupView != null && optionsPopupView.IsVisible && popupRow == row)
            {
                HideOptionsPopup();
                return;
            }

            var baseStats = ClientRuntime.Character.BaseStats;
            if (!baseStats.HasValue)
                return;

            popupRow = row;
            ShowOptionsForRow(row, baseStats.Value);
        }

        private void ShowOptionsForRow(PotentialUpgradeRowView row, CharacterBaseStatsModel stats)
        {
            if (optionsPopupView == null || row == null)
                return;

            var preview = GetPreview(stats, row.Target);
            var options = BuildUpgradeOptions(row.Target, preview, stats.UnallocatedPotential);
            if (options.Count == 0)
            {
                HideOptionsPopup();
                return;
            }

            optionsPopupView.Show(
                row.transform as RectTransform,
                ResolvePresentation(row.Target).DisplayName,
                options,
                force: true);
        }

        private List<PotentialUpgradeOptionsPopupView.OptionEntry> BuildUpgradeOptions(
            PotentialAllocationTarget target,
            PotentialUpgradePreviewModel? preview,
            int unallocatedPotential)
        {
            var result = new List<PotentialUpgradeOptionsPopupView.OptionEntry>(maxVisibleUpgradeOptions);
            if (!preview.HasValue || !preview.Value.IsAvailable || !preview.Value.CanUpgrade || maxVisibleUpgradeOptions <= 0)
                return result;

            var spendOptions = BuildSpendOptions(preview.Value, unallocatedPotential, maxVisibleUpgradeOptions);
            if (spendOptions.Count == 0)
                return result;

            var presentation = ResolvePresentation(target);
            for (var i = 0; i < spendOptions.Count; i++)
            {
                var requestedPotentialAmount = spendOptions[i];
                var appliedUpgradeCount = preview.Value.PotentialCost > 0
                    ? requestedPotentialAmount / preview.Value.PotentialCost
                    : 0;
                if (appliedUpgradeCount <= 0)
                    continue;

                var totalGain = appliedUpgradeCount * preview.Value.StatGain;
                var label = string.Format(
                    CultureInfo.InvariantCulture,
                    "Dung {0} tiem nang  ->  +{1} {2}",
                    requestedPotentialAmount,
                    totalGain.ToString(presentation.GainFormat, CultureInfo.InvariantCulture),
                    presentation.DisplayName);

                result.Add(new PotentialUpgradeOptionsPopupView.OptionEntry(
                    label,
                    () => _ = AllocatePotentialAsync(target, requestedPotentialAmount)));
            }

            return result;
        }

        private async System.Threading.Tasks.Task AllocatePotentialAsync(PotentialAllocationTarget target, int requestedPotentialAmount)
        {
            if (actionInFlight || !ClientRuntime.IsInitialized)
                return;

            actionInFlight = true;
            HideOptionsPopup(force: true);

            RefreshPanel(force: true);
            try
            {
                var result = await ClientRuntime.CharacterService.AllocatePotentialAsync(target, requestedPotentialAmount);
                if (!result.Success)
                {
                    lastStatusMessage = string.Format(
                        CultureInfo.InvariantCulture,
                        "Nang {0} that bai: {1}",
                        ResolvePresentation(target).DisplayName,
                        result.Code ?? MessageCode.UnknownError);
                    ClientLog.Warn($"WorldPotentialPanelController allocate potential failed: {result.Message}");
                    return;
                }

                var partialSuffix = result.SpentPotentialAmount < result.RequestedPotentialAmount
                    ? " do cham moc bac hien tai"
                    : string.Empty;
                lastStatusMessage = string.Format(
                    CultureInfo.InvariantCulture,
                    "Da nang {0}: +{1} lan, dung {2}/{3} tiem nang{4}.",
                    ResolvePresentation(target).DisplayName,
                    result.AppliedUpgradeCount,
                    result.SpentPotentialAmount,
                    result.RequestedPotentialAmount,
                    partialSuffix);
            }
            catch (Exception ex)
            {
                lastStatusMessage = string.Format(CultureInfo.InvariantCulture, "Loi nang chi so: {0}", ex.Message);
                ClientLog.Warn($"WorldPotentialPanelController allocate exception: {ex.Message}");
            }
            finally
            {
                actionInFlight = false;
                RefreshPanel(force: true);
            }
        }

        private PotentialUpgradeRowListView.Entry[] BuildRowEntries(CharacterBaseStatsModel stats)
        {
            var entries = new PotentialUpgradeRowListView.Entry[SupportedTargets.Length];
            for (var i = 0; i < SupportedTargets.Length; i++)
            {
                var target = SupportedTargets[i];
                var presentation = ResolvePresentation(target);
                entries[i] = new PotentialUpgradeRowListView.Entry(
                    target,
                    presentation,
                    ResolveCurrentValueText(stats, target, presentation));
            }

            return entries;
        }

        private PotentialStatPresentation ResolvePresentation(PotentialAllocationTarget target)
        {
            return presentationCatalog != null
                ? presentationCatalog.Resolve(target)
                : new PotentialStatPresentation(
                    target,
                    PotentialStatPresentationCatalog.GetFallbackDisplayName(target),
                    null,
                    target == PotentialAllocationTarget.BaseFortune ? "0.##" : "0",
                    "0.##");
        }

        private static string ResolveRealmDisplayName(CharacterBaseStatsModel stats)
        {
            if (!string.IsNullOrWhiteSpace(stats.RealmDisplayName))
                return stats.RealmDisplayName.Trim();

            return stats.RealmTemplateId > 0
                ? string.Format(CultureInfo.InvariantCulture, "Canh gioi {0}", stats.RealmTemplateId)
                : "Chua co canh gioi";
        }

        private static string BuildCultivationProgress(CharacterBaseStatsModel stats)
        {
            var maxCultivation = Math.Max(0L, stats.RealmMaxCultivation);
            var currentCultivation = Math.Max(0L, stats.Cultivation);
            return string.Format(CultureInfo.InvariantCulture, "{0}/{1}", currentCultivation, maxCultivation);
        }

        private static PotentialUpgradePreviewModel? GetPreview(CharacterBaseStatsModel stats, PotentialAllocationTarget target)
        {
            var previews = stats.PotentialUpgradePreviews;
            if (previews == null)
                return null;

            for (var i = 0; i < previews.Count; i++)
            {
                if (previews[i].TargetStat == (int)target)
                    return previews[i];
            }

            return null;
        }

        private static List<int> BuildSpendOptions(
            PotentialUpgradePreviewModel preview,
            int unallocatedPotential,
            int maxVisibleOptionCount)
        {
            var result = new List<int>(Math.Max(1, maxVisibleOptionCount));
            if (!preview.IsAvailable || !preview.CanUpgrade || preview.PotentialCost <= 0 || maxVisibleOptionCount <= 0)
                return result;

            var maxAffordableUpgradeCount = unallocatedPotential / preview.PotentialCost;
            if (maxAffordableUpgradeCount <= 0)
                return result;

            AddUnique(result, maxAffordableUpgradeCount * preview.PotentialCost);

            long power = 1;
            while (power * 10L <= maxAffordableUpgradeCount)
                power *= 10L;

            while (power > 0L && result.Count < maxVisibleOptionCount)
            {
                var spendAmount = power * preview.PotentialCost;
                if (spendAmount > 0L && spendAmount <= int.MaxValue)
                    AddUnique(result, (int)spendAmount);

                power /= 10L;
            }

            result.Sort((left, right) => right.CompareTo(left));
            if (result.Count > maxVisibleOptionCount)
                result.RemoveRange(maxVisibleOptionCount, result.Count - maxVisibleOptionCount);
            return result;
        }

        private static void AddUnique(List<int> values, int candidate)
        {
            if (candidate > 0 && !values.Contains(candidate))
                values.Add(candidate);
        }

        private static string ResolveCurrentValueText(
            CharacterBaseStatsModel stats,
            PotentialAllocationTarget target,
            PotentialStatPresentation presentation)
        {
            return target switch
            {
                PotentialAllocationTarget.BaseHp => (stats.BaseHp + stats.PotentialHpBonus).ToString(presentation.ValueFormat, CultureInfo.InvariantCulture),
                PotentialAllocationTarget.BaseMp => (stats.BaseMp + stats.PotentialMpBonus).ToString(presentation.ValueFormat, CultureInfo.InvariantCulture),
                PotentialAllocationTarget.BaseAttack => (stats.BaseAttack + stats.PotentialAttackBonus).ToString(presentation.ValueFormat, CultureInfo.InvariantCulture),
                PotentialAllocationTarget.BaseSpeed => (stats.BaseSpeed + stats.PotentialSpeedBonus).ToString(presentation.ValueFormat, CultureInfo.InvariantCulture),
                PotentialAllocationTarget.BaseSpiritualSense => (stats.BaseSpiritualSense + stats.PotentialSpiritualSenseBonus).ToString(presentation.ValueFormat, CultureInfo.InvariantCulture),
                PotentialAllocationTarget.BaseFortune => (stats.BaseFortune + stats.PotentialFortuneBonus).ToString(presentation.ValueFormat, CultureInfo.InvariantCulture),
                _ => "-"
            };
        }

        private static string BuildSnapshot(CharacterBaseStatsModel stats)
        {
            return string.Join(
                "|",
                ResolveRealmDisplayName(stats),
                stats.RealmMaxCultivation.ToString(CultureInfo.InvariantCulture),
                stats.Cultivation.ToString(CultureInfo.InvariantCulture),
                stats.UnallocatedPotential.ToString(CultureInfo.InvariantCulture),
                stats.PotentialHpBonus.ToString(CultureInfo.InvariantCulture),
                stats.PotentialMpBonus.ToString(CultureInfo.InvariantCulture),
                stats.PotentialAttackBonus.ToString(CultureInfo.InvariantCulture),
                stats.PotentialSpeedBonus.ToString(CultureInfo.InvariantCulture),
                stats.PotentialSpiritualSenseBonus.ToString(CultureInfo.InvariantCulture),
                stats.PotentialFortuneBonus.ToString("0.####", CultureInfo.InvariantCulture),
                BuildPreviewSnapshot(stats.PotentialUpgradePreviews));
        }

        private static string BuildPreviewSnapshot(IReadOnlyList<PotentialUpgradePreviewModel> previews)
        {
            if (previews == null || previews.Count == 0)
                return string.Empty;

            return string.Join(
                ";",
                previews.Select(
                    preview => string.Concat(
                        preview.TargetStat.ToString(CultureInfo.InvariantCulture),
                        ":",
                        preview.NextUpgradeCount.ToString(CultureInfo.InvariantCulture),
                        ":",
                        preview.TierIndex.ToString(CultureInfo.InvariantCulture),
                        ":",
                        preview.PotentialCost.ToString(CultureInfo.InvariantCulture),
                        ":",
                        preview.StatGain.ToString("0.####", CultureInfo.InvariantCulture),
                        ":",
                        preview.IsAvailable ? "1" : "0",
                        ":",
                        preview.CanUpgrade ? "1" : "0")));
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

        private void HideOptionsPopup(bool force = false)
        {
            popupRow = null;
            if (optionsPopupView != null)
                optionsPopupView.Hide(force);
        }

        private bool DidClickBlankSpaceInsideStatsPanel()
        {
            if (!Input.GetMouseButtonDown(0) || statsPanelBounds == null)
                return false;

            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;

            var canvas = statsPanelBounds.GetComponentInParent<Canvas>();
            var eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            if (!RectTransformUtility.RectangleContainsScreenPoint(statsPanelBounds, Input.mousePosition, eventCamera))
                return false;

            var pointerData = new PointerEventData(eventSystem)
            {
                position = Input.mousePosition
            };
            var results = new System.Collections.Generic.List<RaycastResult>(8);
            eventSystem.RaycastAll(pointerData, results);

            for (var i = 0; i < results.Count; i++)
            {
                var hitTransform = results[i].gameObject != null ? results[i].gameObject.transform : null;
                if (hitTransform == null)
                    continue;

                if (popupRow != null && hitTransform.IsChildOf(popupRow.transform))
                    return false;

                if (optionsPopupView != null && hitTransform.IsChildOf(optionsPopupView.transform))
                    return false;
            }

            return true;
        }
    }
}
