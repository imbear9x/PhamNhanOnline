using System;
using System.Collections.Generic;
using System.Globalization;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Features.Character.Application;
using PhamNhanOnline.Client.Core.Logging;
using GameShared.Models;
using TMPro;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    public sealed class WorldTravelDebugController : MonoBehaviour
    {
        [SerializeField] private KeyCode travelToAdjacentMapKey = KeyCode.T;
        [SerializeField] private KeyCode cultivationToggleKey = KeyCode.U;
        [SerializeField] private KeyCode breakthroughKey = KeyCode.B;
        [SerializeField] private KeyCode joinZoneKey = KeyCode.I;
        [SerializeField] private KeyCode allocatePotentialKey = KeyCode.P;
        [SerializeField] private KeyCode cyclePotentialOptionKey = KeyCode.O;
        [SerializeField] private TMP_InputField zoneInputField;
        [SerializeField] private TMP_Dropdown allocateTargetDropdown;
        [SerializeField] private TMP_Text currentStateText;
        [SerializeField] private TMP_Text characterStatsText;
        [SerializeField] private TMP_Text potentialPreviewText;
        [SerializeField] private TMP_Text cultivationRewardText;

        private bool travelInFlight;
        private bool cultivationToggleInFlight;
        private bool breakthroughInFlight;
        private bool zoneJoinInFlight;
        private bool allocatePotentialInFlight;
        private int selectedPotentialSpendOptionIndex;
        private bool rewardTextPinnedByDebugMessage;
        private int lastStateCode = int.MinValue;
        private string lastStatsSnapshot = string.Empty;
        private bool hasAppliedRewardSnapshot;
        private long lastRewardCultivation = long.MinValue;
        private int lastRewardPotential = int.MinValue;
        private bool lastRewardOfflineSettlement;
        private bool lastRewardReachedCap;
        private long? lastRewardFromUnixMs;
        private long? lastRewardToUnixMs;
        private string lastCultivationDebugMessage = string.Empty;

        private void Start()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            InitializePotentialDropdown();
            RefreshCurrentStateText(force: true);
            RefreshCharacterStatsText(force: true);
            RefreshPotentialPreviewText(force: true);
            RefreshRewardTextFromCachedState();
        }

        private void OnDestroy()
        {
        }

        private void Update()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            RefreshCurrentStateText(force: false);
            RefreshCharacterStatsText(force: false);
            RefreshPotentialPreviewText(force: false);
            RefreshRewardTextFromCachedState();

            if (WasCultivationTogglePressed() && !cultivationToggleInFlight)
            {
                ShowCultivationDebugMessage(string.Format(
                    "Nhan phim {0}: dang gui yeu cau {1}.",
                    cultivationToggleKey,
                    IsCultivating() ? "dung tu luyen" : "bat dau tu luyen"));
                _ = ToggleCultivationAsync();
                return;
            }

            if (Input.GetKeyDown(breakthroughKey) && !breakthroughInFlight)
            {
                ShowCultivationDebugMessage(string.Format(
                    "Nhan phim {0}: dang gui yeu cau dot pha canh gioi.",
                    breakthroughKey));
                _ = BreakthroughAsync();
                return;
            }

            if (Input.GetKeyDown(joinZoneKey) && !zoneJoinInFlight && !IsCultivating())
            {
                _ = JoinZoneAsync();
                return;
            }

            if (Input.GetKeyDown(cyclePotentialOptionKey) && !allocatePotentialInFlight)
            {
                CyclePotentialSpendOption();
                return;
            }

            if (Input.GetKeyDown(allocatePotentialKey) && !allocatePotentialInFlight)
            {
                _ = AllocatePotentialAsync();
                return;
            }

            if (!Input.GetKeyDown(travelToAdjacentMapKey) || travelInFlight || IsCultivating())
                return;

            var adjacentMapIds = ClientRuntime.World.CurrentAdjacentMapIds;
            if (adjacentMapIds == null || adjacentMapIds.Count == 0)
            {
                ClientLog.Warn("WorldTravelDebugController found no adjacent map to travel to.");
                return;
            }

            var targetMapId = adjacentMapIds[0];
            _ = TravelAsync(targetMapId);
        }

        private async System.Threading.Tasks.Task TravelAsync(int targetMapId)
        {
            travelInFlight = true;
            try
            {
                var result = await ClientRuntime.WorldTravelService.TravelToMapAsync(targetMapId);
                if (!result.Success)
                    ClientLog.Warn($"WorldTravelDebugController travel failed: {result.Message}");
                else
                    ClientLog.Info($"WorldTravelDebugController travel succeeded: {result.Message}");
            }
            catch (Exception ex)
            {
                ClientLog.Warn($"WorldTravelDebugController travel exception: {ex.Message}");
            }
            finally
            {
                travelInFlight = false;
            }
        }

        private async System.Threading.Tasks.Task AllocatePotentialAsync()
        {
            PotentialAllocationTarget target;
            if (!TryGetPotentialAllocationTarget(out target))
                return;

            PotentialUpgradePreviewModel preview;
            int requestedPotentialAmount;
            if (!TryGetSelectedPotentialSpendOption(target, out preview, out requestedPotentialAmount))
                return;

            allocatePotentialInFlight = true;
            try
            {
                ShowCultivationDebugMessage(string.Format(
                    "Dang nang {0}: yeu cau dung {1} tiem nang, chi phi hien tai {2}/lan, +{3} moi lan.",
                    GetPotentialTargetLabel(target),
                    requestedPotentialAmount,
                    preview.PotentialCost,
                    FormatPreviewGain(preview)));
                var result = await ClientRuntime.CharacterService.AllocatePotentialAsync(target, requestedPotentialAmount);
                if (!result.Success)
                {
                    ClientLog.Warn($"WorldTravelDebugController allocate potential failed: {result.Message}");
                    ShowCultivationActionResult("Nang chi so that bai", result.Code, result.Message);
                    return;
                }

                RefreshCharacterStatsText(force: true);
                RefreshPotentialPreviewText(force: true);
                var partialSuffix = result.SpentPotentialAmount < result.RequestedPotentialAmount
                    ? " do cham moc bac hien tai"
                    : string.Empty;
                ShowCultivationDebugMessage(string.Format(
                    "Da nang {0}: +{1} lan, dung {2}/{3} tiem nang{4}.",
                    GetPotentialTargetLabel(target),
                    result.AppliedUpgradeCount,
                    result.SpentPotentialAmount,
                    result.RequestedPotentialAmount,
                    partialSuffix));
            }
            catch (Exception ex)
            {
                ClientLog.Warn($"WorldTravelDebugController allocate potential exception: {ex.Message}");
                ShowCultivationDebugMessage($"Loi nang chi so: {ex.Message}");
            }
            finally
            {
                allocatePotentialInFlight = false;
            }
        }

        private async System.Threading.Tasks.Task ToggleCultivationAsync()
        {
            cultivationToggleInFlight = true;
            try
            {
                if (IsCultivating())
                {
                    var stopResult = await ClientRuntime.CharacterService.StopCultivationAsync();
                    if (!stopResult.Success)
                    {
                        ClientLog.Warn($"WorldTravelDebugController stop cultivation failed: {stopResult.Message}");
                        ShowCultivationActionResult("Dung tu luyen that bai", stopResult.Code, stopResult.Message);
                    }
                    else
                    {
                        ClientLog.Info($"WorldTravelDebugController stop cultivation succeeded: {stopResult.Message}");
                        ShowCultivationActionResult("Da dung tu luyen", stopResult.Code, stopResult.Message);
                    }
                }
                else
                {
                    var startResult = await ClientRuntime.CharacterService.StartCultivationAsync();
                    if (!startResult.Success)
                    {
                        ClientLog.Warn($"WorldTravelDebugController start cultivation failed: {startResult.Message}");
                        ShowCultivationActionResult("Bat dau tu luyen that bai", startResult.Code, startResult.Message);
                    }
                    else
                    {
                        ClientLog.Info($"WorldTravelDebugController start cultivation succeeded: {startResult.Message}");
                        ShowCultivationActionResult("Da bat dau tu luyen", startResult.Code, startResult.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                ClientLog.Warn($"WorldTravelDebugController cultivation exception: {ex.Message}");
                ShowCultivationDebugMessage($"Loi cultivation: {ex.Message}");
            }
            finally
            {
                cultivationToggleInFlight = false;
                RefreshCurrentStateText(force: true);
            }
        }

        private async System.Threading.Tasks.Task BreakthroughAsync()
        {
            breakthroughInFlight = true;
            try
            {
                var result = await ClientRuntime.CharacterService.BreakthroughAsync();
                if (!result.Success)
                {
                    ClientLog.Warn($"WorldTravelDebugController breakthrough failed: {result.Message}");
                    ShowCultivationActionResult("Dot pha that bai", result.Code, result.Message);
                    RefreshCharacterStatsText(force: true);
                    RefreshPotentialPreviewText(force: true);
                    return;
                }

                ClientLog.Info($"WorldTravelDebugController breakthrough succeeded: {result.Message}");
                RefreshCharacterStatsText(force: true);
                RefreshPotentialPreviewText(force: true);
                ShowCultivationActionResult("Dot pha thanh cong", result.Code, result.Message);
            }
            catch (Exception ex)
            {
                ClientLog.Warn($"WorldTravelDebugController breakthrough exception: {ex.Message}");
                ShowCultivationDebugMessage($"Loi dot pha: {ex.Message}");
            }
            finally
            {
                breakthroughInFlight = false;
                RefreshCurrentStateText(force: true);
            }
        }

        private async System.Threading.Tasks.Task JoinZoneAsync()
        {
            var currentMapId = ClientRuntime.World.CurrentMapId;
            if (!currentMapId.HasValue)
            {
                ShowCultivationDebugMessage("Chua vao map nen khong the doi khu.");
                return;
            }

            int requestedZoneIndex;
            if (!TryGetRequestedZoneIndex(out requestedZoneIndex))
                return;

            zoneJoinInFlight = true;
            try
            {
                ShowCultivationDebugMessage(string.Format("Dang doi sang khu {0}...", requestedZoneIndex));
                var switchResult = await ClientRuntime.WorldTravelService.SwitchMapZoneAsync(currentMapId.Value, requestedZoneIndex);
                if (!switchResult.Success)
                {
                    ShowCultivationActionResult("Doi khu that bai", switchResult.Code, switchResult.Message);
                    return;
                }

                RefreshCurrentStateText(force: true);
                if (switchResult.Zone.HasValue)
                {
                    var zone = switchResult.Zone.Value;
                    ShowCultivationDebugMessage(string.Format(
                        "Da vao khu {0}. Linh khi: {1}/phut ({2}).",
                        zone.ZoneIndex,
                        FormatSpiritualEnergy(zone.SpiritualEnergyPerMinute),
                        string.IsNullOrWhiteSpace(zone.SpiritualEnergyCode) ? "unknown" : zone.SpiritualEnergyCode));
                }
                else
                {
                    ShowCultivationDebugMessage(string.Format("Da vao khu {0}.", requestedZoneIndex));
                }
            }
            catch (Exception ex)
            {
                ClientLog.Warn($"WorldTravelDebugController join zone exception: {ex.Message}");
                ShowCultivationDebugMessage($"Loi doi khu: {ex.Message}");
            }
            finally
            {
                zoneJoinInFlight = false;
            }
        }

        private bool IsCultivating()
        {
            var currentState = ClientRuntime.Character.CurrentState;
            return currentState.HasValue && currentState.Value.CurrentState == CharacterRuntimeStateCodes.Cultivating;
        }

        private void RefreshCurrentStateText(bool force)
        {
            var currentState = ClientRuntime.Character.CurrentState;
            var stateCode = currentState.HasValue ? currentState.Value.CurrentState : CharacterRuntimeStateCodes.Unknown;
            if (!force && stateCode == lastStateCode)
                return;

            lastStateCode = stateCode;
            if (currentStateText != null)
            {
                var mapName = string.IsNullOrWhiteSpace(ClientRuntime.World.CurrentMapName)
                    ? "chua vao map"
                    : ClientRuntime.World.CurrentMapName;
                var mapScope = ClientRuntime.World.CurrentMapIsPrivatePerPlayer ? "private" : "public";
                var zoneSuffix = ClientRuntime.World.CurrentZoneIndex.HasValue
                    ? $" | Khu: {ClientRuntime.World.CurrentZoneIndex.Value}"
                    : string.Empty;
                currentStateText.text = $"Trang thai: {GetStateLabel(stateCode)} | Map: {mapName} ({mapScope}){zoneSuffix}";
            }
        }

        private void RefreshCharacterStatsText(bool force)
        {
            if (characterStatsText == null)
                return;

            var snapshot = BuildCharacterStatsSnapshot();
            if (!force && string.Equals(snapshot, lastStatsSnapshot, StringComparison.Ordinal))
                return;

            lastStatsSnapshot = snapshot;
            characterStatsText.text = snapshot;
        }

        private void RefreshPotentialPreviewText(bool force)
        {
            if (potentialPreviewText == null)
                return;

            var previewLine = BuildPotentialPreviewLine();
            if (!force && string.Equals(previewLine, potentialPreviewText.text, StringComparison.Ordinal))
                return;

            potentialPreviewText.text = previewLine;
        }

        private void RefreshRewardTextFromCachedState()
        {
            var lastReward = ClientRuntime.Character.LastCultivationReward;
            if (!lastReward.HasValue)
            {
                if (!hasAppliedRewardSnapshot && !rewardTextPinnedByDebugMessage)
                    ApplyEmptyRewardStateText();
                return;
            }

            if (!HasRewardChanged(lastReward.Value))
                return;

            ApplyRewardText(lastReward.Value);
        }

        private void ApplyRewardText(CultivationRewardNotice notice)
        {
            if (cultivationRewardText == null)
                return;

            var sourceLabel = notice.IsOfflineSettlement ? "offline" : "online";
            var capSuffix = notice.ReachedRealmCap ? " | da vien man" : string.Empty;
            cultivationRewardText.text =
                $"Tu vi nhan them ({sourceLabel}): +{notice.CultivationGranted} tu vi, +{notice.UnallocatedPotentialGranted} tiem nang{capSuffix}";
            lastCultivationDebugMessage = cultivationRewardText.text;
            rewardTextPinnedByDebugMessage = false;
            hasAppliedRewardSnapshot = true;
            lastRewardCultivation = notice.CultivationGranted;
            lastRewardPotential = notice.UnallocatedPotentialGranted;
            lastRewardOfflineSettlement = notice.IsOfflineSettlement;
            lastRewardReachedCap = notice.ReachedRealmCap;
            lastRewardFromUnixMs = notice.RewardedFromUnixMs;
            lastRewardToUnixMs = notice.RewardedToUnixMs;
        }

        private bool HasRewardChanged(CultivationRewardNotice notice)
        {
            return !hasAppliedRewardSnapshot ||
                   lastRewardCultivation != notice.CultivationGranted ||
                   lastRewardPotential != notice.UnallocatedPotentialGranted ||
                   lastRewardOfflineSettlement != notice.IsOfflineSettlement ||
                   lastRewardReachedCap != notice.ReachedRealmCap ||
                   lastRewardFromUnixMs != notice.RewardedFromUnixMs ||
                   lastRewardToUnixMs != notice.RewardedToUnixMs;
        }

        private void ApplyEmptyRewardStateText()
        {
            if (cultivationRewardText == null)
                return;

            var currentState = ClientRuntime.Character.CurrentState;
            if (currentState.HasValue && currentState.Value.CurrentState == CharacterRuntimeStateCodes.Cultivating)
            {
                var lastRewardedLabel = FormatUnixMs(currentState.Value.LastCultivationRewardedUnixMs);
                cultivationRewardText.text = string.IsNullOrEmpty(lastRewardedLabel)
                    ? "Dang tu luyen, chua co tu vi moi de nhan."
                    : $"Dang tu luyen, chua co tu vi moi de nhan. Lan settlement gan nhat: {lastRewardedLabel}";
                return;
            }

            cultivationRewardText.text = "Cultivation debug: chua co su kien";
        }

        private static string FormatUnixMs(long? unixMs)
        {
            if (!unixMs.HasValue)
                return string.Empty;

            try
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(unixMs.Value).ToLocalTime().ToString("HH:mm:ss");
            }
            catch
            {
                return string.Empty;
            }
        }

        private bool WasCultivationTogglePressed()
        {
            if (Input.GetKeyDown(cultivationToggleKey))
                return true;

            var typed = Input.inputString;
            if (string.IsNullOrEmpty(typed))
                return false;

            for (var i = 0; i < typed.Length; i++)
            {
                var c = typed[i];
                if (c == 'u' || c == 'U')
                    return true;
            }

            return false;
        }

        private void ShowCultivationActionResult(string actionLabel, GameShared.Messages.MessageCode? code, string message)
        {
            var suffix = code.HasValue && code.Value != GameShared.Messages.MessageCode.None
                ? string.Format(" ({0})", code.Value)
                : string.Empty;
            ShowCultivationDebugMessage(string.Format("{0}{1}: {2}", actionLabel, suffix, message));
        }

        private void ShowCultivationDebugMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            lastCultivationDebugMessage = message;
            rewardTextPinnedByDebugMessage = true;
            if (cultivationRewardText != null)
                cultivationRewardText.text = message;
        }

        private void InitializePotentialDropdown()
        {
            if (allocateTargetDropdown == null)
                return;

            if (allocateTargetDropdown.options != null && allocateTargetDropdown.options.Count > 0)
                return;

            allocateTargetDropdown.ClearOptions();
            allocateTargetDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "Mau",
                "Linh luc",
                "Suc tan cong",
                "Toc do",
                "Co duyen",
                "Than thuc"
            });
        }

        private bool TryGetRequestedZoneIndex(out int zoneIndex)
        {
            zoneIndex = 0;
            if (zoneInputField == null)
            {
                ShowCultivationDebugMessage("Chua gan o nhap khu trong inspector.");
                return false;
            }

            var rawValue = zoneInputField.text == null ? string.Empty : zoneInputField.text.Trim();
            if (string.IsNullOrEmpty(rawValue) || !int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out zoneIndex) || zoneIndex <= 0)
            {
                ShowCultivationDebugMessage("Nhap so khu hop le roi nhan phim I de doi khu.");
                return false;
            }

            return true;
        }

        private bool TryGetPotentialAllocationTarget(out PotentialAllocationTarget target)
        {
            target = PotentialAllocationTarget.None;

            if (allocateTargetDropdown == null)
            {
                ShowCultivationDebugMessage("Chua gan dropdown chon chi so trong inspector.");
                return false;
            }

            target = MapDropdownIndexToPotentialTarget(allocateTargetDropdown.value);
            if (target == PotentialAllocationTarget.None)
            {
                ShowCultivationDebugMessage("Chi so duoc chon khong hop le.");
                return false;
            }

            return true;
        }

        private bool TryGetPotentialAllocationTargetSilently(out PotentialAllocationTarget target)
        {
            target = PotentialAllocationTarget.None;
            if (allocateTargetDropdown == null)
                return false;

            target = MapDropdownIndexToPotentialTarget(allocateTargetDropdown.value);
            return target != PotentialAllocationTarget.None;
        }

        private string BuildCharacterStatsSnapshot()
        {
            var baseStats = ClientRuntime.Character.BaseStats;
            var currentState = ClientRuntime.Character.CurrentState;
            if (!baseStats.HasValue)
                return "Chua co du lieu chi so nhan vat.";

            var stats = baseStats.Value;
            var currentHp = currentState.HasValue ? currentState.Value.CurrentHp : stats.BaseHp;
            var currentMp = currentState.HasValue ? currentState.Value.CurrentMp : stats.BaseMp;
            var currentStamina = currentState.HasValue ? currentState.Value.CurrentStamina : stats.BaseStamina;
            var hpPreview = GetPreview(stats, PotentialAllocationTarget.BaseHp);
            var mpPreview = GetPreview(stats, PotentialAllocationTarget.BaseMp);
            var attackPreview = GetPreview(stats, PotentialAllocationTarget.BaseAttack);
            var speedPreview = GetPreview(stats, PotentialAllocationTarget.BaseSpeed);
            var fortunePreview = GetPreview(stats, PotentialAllocationTarget.BaseFortune);
            var spiritualSensePreview = GetPreview(stats, PotentialAllocationTarget.BaseSpiritualSense);

            return string.Format(
                CultureInfo.InvariantCulture,
                "Tiem nang chua dung: {0}\nHP: {1}/{2} (bonus {3}, bac {4}) | next {5}\nMP: {6}/{7} (bonus {8}, bac {9}) | next {10}\nTan cong: {11} (bonus {12}, bac {13}) | next {14}\nToc do: {15} (bonus {16}, bac {17}) | next {18}\nThan thuc: {19} (bonus {20}, bac {21}) | next {22}\nCo duyen: {23:0.##} (bonus {24:0.##}, bac {25}) | next {26}\nThe luc hien tai/max: {27}/{28}",
                stats.UnallocatedPotential,
                currentHp,
                stats.BaseHp,
                stats.BonusHp,
                stats.HpUpgradeCount,
                FormatPreview(hpPreview),
                currentMp,
                stats.BaseMp,
                stats.BonusMp,
                stats.MpUpgradeCount,
                FormatPreview(mpPreview),
                stats.BaseAttack,
                stats.BonusAttack,
                stats.AttackUpgradeCount,
                FormatPreview(attackPreview),
                stats.BaseSpeed,
                stats.BonusSpeed,
                stats.SpeedUpgradeCount,
                FormatPreview(speedPreview),
                stats.BaseSpiritualSense,
                stats.BonusSpiritualSense,
                stats.SpiritualSenseUpgradeCount,
                FormatPreview(spiritualSensePreview),
                stats.BaseFortune,
                stats.BonusFortune,
                stats.FortuneUpgradeCount,
                FormatPreview(fortunePreview),
                currentStamina,
                stats.BaseStamina);
        }

        private string BuildPotentialPreviewLine()
        {
            var baseStats = ClientRuntime.Character.BaseStats;
            if (!baseStats.HasValue)
                return "Preview nang chi so: chua co chi so nhan vat.";

            PotentialAllocationTarget target;
            if (!TryGetPotentialAllocationTargetSilently(out target))
                return "Preview nang chi so: chua chon chi so.";

            var preview = GetPreview(baseStats.Value, target);
            if (!preview.HasValue || !preview.Value.IsAvailable)
                return $"Preview {GetPotentialTargetLabel(target)}: chua co tier cau hinh.";

            var state = preview.Value.CanUpgrade ? "co the nang" : "khong du tiem nang";
            var spendOptions = BuildPotentialSpendOptions(preview.Value, baseStats.Value.UnallocatedPotential);
            if (spendOptions.Count == 0)
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "Preview {0}: lan {1}, ton {2}/lan, +{3}, tier {4}, {5}",
                    GetPotentialTargetLabel(target),
                    preview.Value.NextUpgradeCount,
                    preview.Value.PotentialCost,
                    FormatPreviewGain(preview.Value),
                    preview.Value.TierIndex,
                    state);
            }

            var selectedIndex = NormalizeSelectedPotentialSpendOptionIndex(spendOptions.Count);
            return string.Format(
                CultureInfo.InvariantCulture,
                "Preview {0}: lan {1}, ton {2}/lan, +{3}, tier {4}, {5} | Lua chon: {6} | Nhan {7} de doi, {8} de nang",
                GetPotentialTargetLabel(target),
                preview.Value.NextUpgradeCount,
                preview.Value.PotentialCost,
                FormatPreviewGain(preview.Value),
                preview.Value.TierIndex,
                state,
                FormatPotentialSpendOptions(spendOptions, selectedIndex),
                cyclePotentialOptionKey,
                allocatePotentialKey);
        }

        private PotentialUpgradePreviewModel? GetPreview(PotentialAllocationTarget target)
        {
            var baseStats = ClientRuntime.Character.BaseStats;
            return baseStats.HasValue ? GetPreview(baseStats.Value, target) : null;
        }

        private void CyclePotentialSpendOption()
        {
            PotentialAllocationTarget target;
            if (!TryGetPotentialAllocationTarget(out target))
                return;

            var baseStats = ClientRuntime.Character.BaseStats;
            if (!baseStats.HasValue)
            {
                ShowCultivationDebugMessage("Chua co du lieu chi so nhan vat.");
                return;
            }

            var preview = GetPreview(baseStats.Value, target);
            if (!preview.HasValue || !preview.Value.IsAvailable || !preview.Value.CanUpgrade)
            {
                ShowCultivationDebugMessage($"Hien khong co lua chon nang hop le cho {GetPotentialTargetLabel(target)}.");
                return;
            }

            var spendOptions = BuildPotentialSpendOptions(preview.Value, baseStats.Value.UnallocatedPotential);
            if (spendOptions.Count == 0)
            {
                ShowCultivationDebugMessage($"Hien khong co lua chon nang hop le cho {GetPotentialTargetLabel(target)}.");
                return;
            }

            selectedPotentialSpendOptionIndex = (NormalizeSelectedPotentialSpendOptionIndex(spendOptions.Count) + 1) % spendOptions.Count;
            RefreshPotentialPreviewText(force: true);
            ShowCultivationDebugMessage(string.Format(
                CultureInfo.InvariantCulture,
                "Da chon nang {0} voi muc {1} tiem nang. Nhan {2} de xac nhan.",
                GetPotentialTargetLabel(target),
                spendOptions[selectedPotentialSpendOptionIndex],
                allocatePotentialKey));
        }

        private bool TryGetSelectedPotentialSpendOption(
            PotentialAllocationTarget target,
            out PotentialUpgradePreviewModel preview,
            out int requestedPotentialAmount)
        {
            preview = default;
            requestedPotentialAmount = 0;

            var baseStats = ClientRuntime.Character.BaseStats;
            if (!baseStats.HasValue)
            {
                ShowCultivationDebugMessage("Chua co du lieu chi so nhan vat.");
                return false;
            }

            var previewValue = GetPreview(baseStats.Value, target);
            if (!previewValue.HasValue || !previewValue.Value.IsAvailable)
            {
                ShowCultivationDebugMessage($"Khong co preview de nang {GetPotentialTargetLabel(target)}.");
                return false;
            }

            if (!previewValue.Value.CanUpgrade)
            {
                ShowCultivationDebugMessage($"Khong du tiem nang de nang {GetPotentialTargetLabel(target)}.");
                return false;
            }

            var spendOptions = BuildPotentialSpendOptions(previewValue.Value, baseStats.Value.UnallocatedPotential);
            if (spendOptions.Count == 0)
            {
                ShowCultivationDebugMessage($"Khong co lua chon nang hop le cho {GetPotentialTargetLabel(target)}.");
                return false;
            }

            preview = previewValue.Value;
            requestedPotentialAmount = spendOptions[NormalizeSelectedPotentialSpendOptionIndex(spendOptions.Count)];
            return true;
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

        private static string FormatPreview(PotentialUpgradePreviewModel? preview)
        {
            if (!preview.HasValue || !preview.Value.IsAvailable)
                return "khong co";

            var state = preview.Value.CanUpgrade ? "ok" : "thieu";
            return string.Format(
                CultureInfo.InvariantCulture,
                "ton {0}, +{1}, tier {2}, {3}",
                preview.Value.PotentialCost,
                FormatPreviewGain(preview.Value),
                preview.Value.TierIndex,
                state);
        }

        private static string FormatPreviewGain(PotentialUpgradePreviewModel preview)
        {
            return preview.StatGain.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private int NormalizeSelectedPotentialSpendOptionIndex(int optionCount)
        {
            if (optionCount <= 0)
            {
                selectedPotentialSpendOptionIndex = 0;
                return 0;
            }

            if (selectedPotentialSpendOptionIndex < 0 || selectedPotentialSpendOptionIndex >= optionCount)
                selectedPotentialSpendOptionIndex = 0;

            return selectedPotentialSpendOptionIndex;
        }

        private static List<int> BuildPotentialSpendOptions(PotentialUpgradePreviewModel preview, int unallocatedPotential)
        {
            var result = new List<int>(3);
            if (!preview.IsAvailable || !preview.CanUpgrade || preview.PotentialCost <= 0 || unallocatedPotential < preview.PotentialCost)
                return result;

            var multipliers = new[] { 1, 10, 100 };
            for (var i = 0; i < multipliers.Length; i++)
            {
                var spendAmount = (long)preview.PotentialCost * multipliers[i];
                if (spendAmount <= 0 || spendAmount > int.MaxValue || spendAmount > unallocatedPotential)
                    continue;

                result.Add((int)spendAmount);
            }

            return result;
        }

        private static string FormatPotentialSpendOptions(IReadOnlyList<int> spendOptions, int selectedIndex)
        {
            if (spendOptions.Count == 0)
                return "khong co";

            var parts = new string[spendOptions.Count];
            for (var i = 0; i < spendOptions.Count; i++)
            {
                var isSelected = i == selectedIndex;
                parts[i] = isSelected
                    ? $"[*]{spendOptions[i]}"
                    : spendOptions[i].ToString(CultureInfo.InvariantCulture);
            }

            return string.Join(" | ", parts);
        }

        private static PotentialAllocationTarget MapDropdownIndexToPotentialTarget(int dropdownIndex)
        {
            return dropdownIndex switch
            {
                0 => PotentialAllocationTarget.BaseHp,
                1 => PotentialAllocationTarget.BaseMp,
                2 => PotentialAllocationTarget.BaseAttack,
                3 => PotentialAllocationTarget.BaseSpeed,
                4 => PotentialAllocationTarget.BaseFortune,
                5 => PotentialAllocationTarget.BaseSpiritualSense,
                _ => PotentialAllocationTarget.None
            };
        }

        private static string GetPotentialTargetLabel(PotentialAllocationTarget target)
        {
            return target switch
            {
                PotentialAllocationTarget.BaseHp => "Mau",
                PotentialAllocationTarget.BaseMp => "Linh luc",
                PotentialAllocationTarget.BaseAttack => "Suc tan cong",
                PotentialAllocationTarget.BaseSpeed => "Toc do",
                PotentialAllocationTarget.BaseFortune => "Co duyen",
                PotentialAllocationTarget.BaseSpiritualSense => "Than thuc",
                _ => "Khong ro"
            };
        }

        private static string FormatSpiritualEnergy(decimal value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static string GetStateLabel(int stateCode)
        {
            return stateCode switch
            {
                CharacterRuntimeStateCodes.Idle => "Binh thuong",
                CharacterRuntimeStateCodes.Dead => "Trong thuong",
                CharacterRuntimeStateCodes.LifespanExpired => "Tho nguyen can",
                CharacterRuntimeStateCodes.Cultivating => "Dang tu luyen",
                _ => "Khong ro"
            };
        }

        private static class CharacterRuntimeStateCodes
        {
            public const int Unknown = -1;
            public const int Idle = 0;
            public const int Dead = 1;
            public const int LifespanExpired = 2;
            public const int Cultivating = 3;
        }
    }
}
