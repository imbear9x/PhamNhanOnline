using System;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Features.Character.Application;
using PhamNhanOnline.Client.UI.MartialArts;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.World
{
    [DisallowMultipleComponent]
    public sealed partial class WorldCultivationPanelController : MonoBehaviour
    {
        private const int CharacterStateIdle = 0;
        private const int CharacterStateLifespanExpired = 2;
        private const int CharacterStateCultivating = 3;
        private const int CharacterStatePracticing = 5;

        private enum PanelActionKind
        {
            None = 0,
            SetActive = 1,
            ClearActive = 2,
            StartCultivation = 3,
            StopCultivation = 4,
            Breakthrough = 5
        }

        [Header("Panel")]
        [SerializeField] private WorldCultivationPanelView panelView;

        [Header("Behavior")]
        [SerializeField] private bool hideOnAwake = true;
        [SerializeField] private KeyCode closeKey = KeyCode.Escape;

        [Header("Status Text")]
        [SerializeField] private string statusReadyText = "\u0053\u1eb5\u006e \u0073\u00e0\u006e\u0067 \u0074\u0075 \u006c\u0075\u0079\u1ec7\u006e";
        [SerializeField] private string statusCultivatingText = "\u0110\u0061\u006e\u0067 \u0074\u0075 \u006c\u0075\u0079\u1ec7\u006e";
        [SerializeField] private string statusBreakthroughSuccessText = "\u0110\u1ed9\u0074 \u0070\u0068\u00e1 \u0074\u0068\u00e0\u006e\u0068 \u0063\u00f4\u006e\u0067";
        [SerializeField] private string statusBreakthroughFailedText = "\u0110\u1ed9\u0074 \u0070\u0068\u00e1 \u0074\u0068\u1ea5\u0074 \u0062\u1ea1\u0069";
        [SerializeField] private string statusBreakthroughRequiredText = "\u0110\u00e3 \u0074\u1edb\u0069 \u0062\u00ec\u006e\u0068 \u0063\u1ea3\u006e\u0068\u002c \u0063\u1ea7\u006e \u0111\u1ed9\u0074 \u0070\u0068\u00e1 \u0111\u1ec3 \u0074\u0069\u1ebf\u0070 \u0074\u1ee5\u0063\u002e";

        private bool isInitialized;
        private bool actionInFlight;
        private int? popupMartialArtId;
        private bool popupTargetsActiveSlot;
        private bool rewardEventSubscribed;
        private PanelActionKind actionKind;
        private string lastStatusMessage = string.Empty;
        private string lastSnapshot = string.Empty;

        public bool IsPanelVisible => panelView != null ? panelView.IsVisible : gameObject.activeSelf;

        private void Awake()
        {
            EnsureInitialized(hideAfterInitialize: hideOnAwake);
        }

        private void OnEnable()
        {
            if (!IsPanelVisible)
                return;

            RefreshPanel(force: true);
        }

        private void Update()
        {
            if (!IsPanelVisible)
                return;

            if (Input.GetKeyDown(closeKey))
            {
                if (!IsCloseLockedBecauseCultivating())
                    HidePanel();
                return;
            }

            RefreshPanel(force: false);
        }

        private void OnDisable()
        {
            HideMartialArtOptionsPopup(force: true);
        }

        private void OnDestroy()
        {
            TryUnsubscribeRewardEvents();

            if (panelView == null)
                return;

            panelView.ActiveMartialArtDropped -= HandleMartialArtDropped;
            panelView.ActiveMartialArtClicked -= HandleActiveMartialArtSlotClicked;
            panelView.MartialArtListItemClicked -= HandleMartialArtListItemClicked;
            panelView.ActiveMartialArtDroppedToList -= HandleActiveMartialArtDroppedToList;
            panelView.StartCultivationClicked -= HandleStartCultivationButtonClicked;
            panelView.StopCultivationClicked -= HandleStopCultivationButtonClicked;
            panelView.BreakthroughClicked -= HandleBreakthroughButtonClicked;
            panelView.CloseClicked -= HandleCloseButtonClicked;
        }

        public void ShowPanel()
        {
            EnsureInitialized(hideAfterInitialize: false);
            if (!IsPanelVisible)
            {
                panelView?.Show();
                return;
            }

            RefreshPanel(force: true);
        }

        public void HidePanel()
        {
            EnsureInitialized(hideAfterInitialize: false);
            if (!IsPanelVisible)
                return;

            panelView?.Hide(force: true);
        }

        private void EnsureInitialized(bool hideAfterInitialize)
        {
            if (panelView == null)
                panelView = GetComponent<WorldCultivationPanelView>();

            if (isInitialized)
                return;

            if (panelView == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(WorldCultivationPanelController)} on '{gameObject.name}' requires {nameof(WorldCultivationPanelView)}.");
            }

            panelView.ValidateSerializedReferences();
            panelView.ActiveMartialArtDropped += HandleMartialArtDropped;
            panelView.ActiveMartialArtClicked += HandleActiveMartialArtSlotClicked;
            panelView.MartialArtListItemClicked += HandleMartialArtListItemClicked;
            panelView.ActiveMartialArtDroppedToList += HandleActiveMartialArtDroppedToList;
            panelView.StartCultivationClicked += HandleStartCultivationButtonClicked;
            panelView.StopCultivationClicked += HandleStopCultivationButtonClicked;
            panelView.BreakthroughClicked += HandleBreakthroughButtonClicked;
            panelView.CloseClicked += HandleCloseButtonClicked;
            TrySubscribeRewardEvents();

            isInitialized = true;

            if (hideAfterInitialize)
                panelView.Hide(force: true);
        }

        private void HandleCloseButtonClicked()
        {
            if (IsCloseLockedBecauseCultivating())
                return;

            HidePanel();
        }

        private void HandleCultivationRewardGranted(CultivationRewardNotice notice)
        {
            if (!IsPanelVisible || notice.IsOfflineSettlement || notice.CultivationGranted <= 0L)
                return;

            panelView?.ShowRewardExp(notice);
        }

        private void RefreshPanel(bool force)
        {
            TrySubscribeRewardEvents();

            if (!ClientRuntime.IsInitialized)
            {
                ApplyMissingState(force);
                return;
            }

            var martialArtState = ClientRuntime.MartialArts;
            var baseStats = ClientRuntime.Character.BaseStats;
            var currentState = ClientRuntime.Character.CurrentState;
            var modalUIManager = WorldModalUIManager.Instance;
            if (popupMartialArtId.HasValue &&
                (modalUIManager == null || !modalUIManager.IsItemOptionsPopupVisible))
            {
                popupMartialArtId = null;
                popupTargetsActiveSlot = false;
                force = true;
            }

            if (!martialArtState.HasLoadedMartialArts)
            {
                ApplyMissingState(force);
                return;
            }

            if (popupMartialArtId.HasValue)
            {
                if (!TryFindOwnedMartialArtById(martialArtState.OwnedMartialArts, popupMartialArtId.Value, out _) ||
                    (popupTargetsActiveSlot &&
                     (!martialArtState.ActiveMartialArtId.HasValue || martialArtState.ActiveMartialArtId.Value != popupMartialArtId.Value)))
                {
                    popupMartialArtId = null;
                    popupTargetsActiveSlot = false;
                    panelView?.HideItemOptionsPopup(force: true);
                    force = true;
                }
            }

            var snapshot = BuildSnapshot(martialArtState, baseStats, currentState);
            if (!force && string.Equals(lastSnapshot, snapshot, StringComparison.Ordinal))
                return;

            lastSnapshot = snapshot;
            ApplyLoadedState(martialArtState, baseStats, currentState, force: true);
        }

        private static bool IsCloseLockedBecauseCultivating()
        {
            if (!ClientRuntime.IsInitialized)
                return false;

            var currentState = ClientRuntime.Character.CurrentState;
            return currentState.HasValue && currentState.Value.CurrentState == CharacterStateCultivating;
        }

        private void TrySubscribeRewardEvents()
        {
            if (rewardEventSubscribed || !ClientRuntime.IsInitialized)
                return;

            ClientRuntime.Character.CultivationRewardGranted -= HandleCultivationRewardGranted;
            ClientRuntime.Character.CultivationRewardGranted += HandleCultivationRewardGranted;
            rewardEventSubscribed = true;
        }

        private void TryUnsubscribeRewardEvents()
        {
            if (!rewardEventSubscribed || !ClientRuntime.IsInitialized)
                return;

            ClientRuntime.Character.CultivationRewardGranted -= HandleCultivationRewardGranted;
            rewardEventSubscribed = false;
        }
    }
}
