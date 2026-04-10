using System;
using System.Globalization;
using GameShared.Messages;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.World
{
    public sealed partial class WorldMartialArtPanelController
    {
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
            if (currentState.HasValue &&
                (currentState.Value.CurrentState == CharacterStateCultivating ||
                 currentState.Value.CurrentState == CharacterStatePracticing))
            {
                if (currentState.Value.CurrentState == CharacterStateCultivating)
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
    }
}
