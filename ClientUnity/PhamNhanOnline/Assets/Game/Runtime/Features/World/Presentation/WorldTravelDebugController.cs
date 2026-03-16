using System;
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
        [SerializeField] private TMP_Text currentStateText;
        [SerializeField] private TMP_Text cultivationRewardText;

        private bool travelInFlight;
        private bool cultivationToggleInFlight;
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
                currentStateText.text = $"Trang thai: {GetStateLabel(stateCode)} | Map: {mapName} ({mapScope})";
            }
        }

        private void RefreshRewardTextFromCachedState()
        {
            var lastReward = ClientRuntime.Character.LastCultivationReward;
            if (!lastReward.HasValue)
            {
                if (!hasAppliedRewardSnapshot)
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
            if (cultivationRewardText != null)
                cultivationRewardText.text = message;
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
