using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Features.Character.Application;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.World
{
    public sealed class WorldCombatDeathController : MonoBehaviour
    {
        [Header("View")]
        [SerializeField] private CombatDeadPanelView panelView;

        [Header("Status Text")]
        [SerializeField] private string actionInProgressText = "Dang tro ve dong phu...";

        private bool actionInFlight;
        private bool loggedMissingPanelView;

        private void Awake()
        {
            if (panelView != null)
                panelView.ReturnHomeRequested += HandleReturnHomeRequested;

            ApplyViewState(false);
        }

        private void Start()
        {
            LogMissingCriticalDependenciesIfNeeded();
        }

        private void OnEnable()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            ClientRuntime.Character.CurrentStateChanged += HandleCharacterCurrentStateChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (ClientRuntime.IsInitialized)
                ClientRuntime.Character.CurrentStateChanged -= HandleCharacterCurrentStateChanged;

            ApplyViewState(false);
        }

        private void OnDestroy()
        {
            if (panelView != null)
                panelView.ReturnHomeRequested -= HandleReturnHomeRequested;
        }

        private void HandleCharacterCurrentStateChanged(CharacterCurrentStateChangeNotice notice)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (!ClientRuntime.IsInitialized)
            {
                ApplyViewState(false);
                return;
            }

            var isCombatDead = IsCombatDead(ClientRuntime.Character.CurrentState);
            if (!isCombatDead)
                actionInFlight = false;

            ApplyViewState(isCombatDead);
        }

        private async void HandleReturnHomeRequested()
        {
            if (actionInFlight || !ClientRuntime.IsInitialized || ClientRuntime.CombatDeathRecoveryService == null)
                return;

            actionInFlight = true;
            if (panelView != null)
            {
                panelView.SetStatus(actionInProgressText);
                panelView.SetBusy(true);
            }

            CombatDeathReturnHomeResult result;
            try
            {
                result = await ClientRuntime.CombatDeathRecoveryService.ReturnHomeAsync();
            }
            finally
            {
                actionInFlight = false;
            }

            if (panelView != null)
            {
                panelView.SetStatus(result.Success ? string.Empty : result.Message);
                panelView.SetBusy(false);
            }

            Refresh();
        }

        private void ApplyViewState(bool isCombatDead)
        {
            if (panelView == null)
                return;

            if (isCombatDead)
            {
                if (WorldUIController.Instance != null)
                    WorldUIController.Instance.HideMenuIfVisible();

                panelView.Show();
                panelView.SetBusy(actionInFlight);
            }
            else
            {
                panelView.Hide();
                panelView.SetBusy(false);
            }
        }

        private static bool IsCombatDead(GameShared.Models.CharacterCurrentStateModel? currentState)
        {
            return currentState.HasValue &&
                   ClientCharacterRuntimeStateCodes.IsCombatDead(currentState.Value.CurrentState);
        }

        private void LogMissingCriticalDependenciesIfNeeded()
        {
            if (panelView == null && !loggedMissingPanelView)
            {
                ClientLog.Error("WorldCombatDeathController is missing CombatDeadPanelView.");
                loggedMissingPanelView = true;
            }
        }
    }
}


