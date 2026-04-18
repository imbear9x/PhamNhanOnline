using System;
using System.Collections.Generic;
using System.Globalization;
using GameShared.Messages;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.UI.Inventory;
using PhamNhanOnline.Client.UI.MartialArts;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.World
{
    public sealed partial class WorldMartialArtPanelController
    {
        private const string UseOptionText = "Su dung";
        private const string UnequipOptionText = "Go ra";

        private void HandleMartialArtDropped(PlayerMartialArtModel martialArt)
        {
            _ = SetActiveMartialArtAsync(martialArt);
        }

        private void HandleMartialArtListItemClicked(PlayerMartialArtModel martialArt)
        {
            if (actionInFlight)
                return;

            var modalUIManager = WorldModalUIManager.Instance;
            if (modalUIManager != null &&
                modalUIManager.IsItemOptionsPopupVisible &&
                !popupTargetsActiveSlot &&
                popupMartialArtId.HasValue &&
                popupMartialArtId.Value == martialArt.MartialArtId)
            {
                HideMartialArtOptionsPopup();
                return;
            }

            ShowMartialArtOptions(martialArt, activeSlot: false);
        }

        private void HandleActiveMartialArtDroppedToList(PlayerMartialArtModel martialArt)
        {
            _ = ClearActiveMartialArtAsync(martialArt);
        }

        private void HandleActiveMartialArtSlotClicked(ActiveMartialArtSlotView slotView)
        {
            if (slotView == null || !slotView.HasItem || actionInFlight)
                return;

            var martialArt = slotView.Item;
            var modalUIManager = WorldModalUIManager.Instance;
            if (modalUIManager != null &&
                modalUIManager.IsItemOptionsPopupVisible &&
                popupTargetsActiveSlot &&
                popupMartialArtId.HasValue &&
                popupMartialArtId.Value == martialArt.MartialArtId)
            {
                HideMartialArtOptionsPopup();
                return;
            }

            ShowMartialArtOptions(martialArt, activeSlot: true);
        }

        private void ShowMartialArtOptions(PlayerMartialArtModel martialArt, bool activeSlot)
        {
            var modalUIManager = WorldModalUIManager.Instance;
            if (modalUIManager == null)
                return;

            var options = BuildMartialArtOptions(martialArt, activeSlot);
            if (options.Count == 0)
            {
                HideMartialArtOptionsPopup(force: true);
                return;
            }

            popupMartialArtId = martialArt.MartialArtId;
            popupTargetsActiveSlot = activeSlot;
            modalUIManager.HideItemTooltip(force: true);
            modalUIManager.ShowItemOptionsPopup(options, force: true);
            RefreshPanel(force: true);
        }

        private List<ItemOptionEntry> BuildMartialArtOptions(PlayerMartialArtModel martialArt, bool activeSlot)
        {
            if (activeSlot || martialArt.IsActive)
            {
                return new List<ItemOptionEntry>(1)
                {
                    new ItemOptionEntry(UnequipOptionText, () => _ = ClearActiveMartialArtAsync(martialArt))
                };
            }

            return new List<ItemOptionEntry>(1)
            {
                new ItemOptionEntry(UseOptionText, () => _ = SetActiveMartialArtAsync(martialArt))
            };
        }

        private void HideMartialArtOptionsPopup(bool force = false)
        {
            popupMartialArtId = null;
            popupTargetsActiveSlot = false;

            var modalUIManager = WorldModalUIManager.Instance;
            if (modalUIManager != null)
            {
                modalUIManager.HideItemOptionsPopup(force);
                modalUIManager.HideItemTooltip(force: true);
            }

            RefreshPanel(force: true);
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

        private void HandleStartCultivationButtonClicked() => _ = StartCultivationAsync();

        private void HandleStopCultivationButtonClicked() => _ = StopCultivationAsync();

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

            HideMartialArtOptionsPopup(force: true);
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
