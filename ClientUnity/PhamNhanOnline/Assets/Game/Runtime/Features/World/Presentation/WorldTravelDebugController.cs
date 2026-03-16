using System;
using System.Globalization;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Features.Character.Application;
using PhamNhanOnline.Client.Core.Logging;
using TMPro;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    public sealed class WorldTravelDebugController : MonoBehaviour
    {
        [SerializeField] private KeyCode travelToAdjacentMapKey = KeyCode.T;
        [SerializeField] private KeyCode cultivationToggleKey = KeyCode.U;
        [SerializeField] private KeyCode joinZoneKey = KeyCode.I;
        [SerializeField] private TMP_InputField zoneInputField;
        [SerializeField] private TMP_Text currentStateText;
        [SerializeField] private TMP_Text cultivationRewardText;

        private bool travelInFlight;
        private bool cultivationToggleInFlight;
        private bool zoneJoinInFlight;
        private bool rewardTextPinnedByDebugMessage;
        private int lastStateCode = int.MinValue;
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

            RefreshCurrentStateText(force: true);
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

            if (Input.GetKeyDown(joinZoneKey) && !zoneJoinInFlight && !IsCultivating())
            {
                _ = JoinZoneAsync();
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
