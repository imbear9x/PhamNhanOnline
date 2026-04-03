using System;
using System.Globalization;
using GameShared.Messages;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Features.MartialArts.Application;
using PhamNhanOnline.Client.Network.Session;
using PhamNhanOnline.Client.UI.MartialArts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.World
{
    public sealed partial class WorldMartialArtPanelController : MonoBehaviour
    {
        private const int CharacterStateIdle = 0;
        private const int CharacterStateLifespanExpired = 2;
        private const int CharacterStateCultivating = 3;

        private enum PanelActionKind
        {
            None = 0,
            SetActive = 1,
            ClearActive = 2,
            StartCultivation = 3,
            StopCultivation = 4,
            Breakthrough = 5
        }

        [Header("References")]
        [SerializeField] private TMP_Text ownedCountText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private MartialArtPresentationCatalog presentationCatalog;
        [SerializeField] private ActiveMartialArtSlotView activeMartialArtSlotView;
        [SerializeField] private MartialArtListView martialArtListView;
        [SerializeField] private GameObject estimateRoot;
        [SerializeField] private Image cultivationProgressFillImage;
        [SerializeField] private TMP_Text cultivationProgressText;
        [SerializeField] private TMP_Text estimateText;
        [SerializeField] private TMP_Text estimateDetailText;
        [SerializeField] private Button startCultivationButton;
        [SerializeField] private TMP_Text startCultivationButtonText;
        [SerializeField] private GameObject breakthroughRoot;
        [SerializeField] private TMP_Text breakthroughChanceText;
        [SerializeField] private Button breakthroughButton;
        [SerializeField] private TMP_Text breakthroughButtonText;

        [Header("Behavior")]
        [SerializeField] private bool autoLoadMissingCharacterData = true;
        [SerializeField] private bool autoLoadMissingMartialArts = true;
        [SerializeField] private float reloadRetryCooldownSeconds = 2f;

        [Header("Display Text")]
        [SerializeField] private string missingOwnedCountText = "Cong phap: 0";
        [SerializeField] private string loadingOwnedCountText = "Dang tai cong phap...";
        [SerializeField] private string missingStatusText = "Chua tai danh sach cong phap.";
        [SerializeField] private string noActiveMartialArtText = "Keo 1 cong phap da hoc vao o chu tu de xem uoc tinh.";
        [SerializeField] private string startCultivationIdleText = "Tu luyen";
        [SerializeField] private string stopCultivationIdleText = "Dung tu luyen";
        [SerializeField] private string cultivationActionInFlightText = "Dang gui...";
        [SerializeField] private string breakthroughIdleText = "Dot pha";
        [SerializeField] private string breakthroughInFlightText = "Dang dot pha...";

        private Guid? lastRequestedCharacterId;
        private float lastCharacterReloadAttemptTime = float.NegativeInfinity;
        private float lastMartialArtReloadAttemptTime = float.NegativeInfinity;
        private bool characterReloadInFlight;
        private bool martialArtReloadInFlight;
        private bool actionInFlight;
        private PanelActionKind actionKind;
        private string lastStatusMessage = string.Empty;
        private string lastSnapshot = string.Empty;

        private void Awake()
        {
            if (activeMartialArtSlotView != null)
                activeMartialArtSlotView.MartialArtDropped += HandleMartialArtDropped;

            if (martialArtListView != null)
                martialArtListView.ActiveMartialArtDroppedToList += HandleActiveMartialArtDroppedToList;

            if (startCultivationButton != null)
            {
                startCultivationButton.onClick.RemoveListener(HandleCultivationButtonClicked);
                startCultivationButton.onClick.AddListener(HandleCultivationButtonClicked);
            }

            if (breakthroughButton != null)
            {
                breakthroughButton.onClick.RemoveListener(HandleBreakthroughButtonClicked);
                breakthroughButton.onClick.AddListener(HandleBreakthroughButtonClicked);
            }
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
        }

        private void OnDestroy()
        {
            if (activeMartialArtSlotView != null)
                activeMartialArtSlotView.MartialArtDropped -= HandleMartialArtDropped;

            if (martialArtListView != null)
                martialArtListView.ActiveMartialArtDroppedToList -= HandleActiveMartialArtDroppedToList;

            if (startCultivationButton != null)
                startCultivationButton.onClick.RemoveListener(HandleCultivationButtonClicked);

            if (breakthroughButton != null)
                breakthroughButton.onClick.RemoveListener(HandleBreakthroughButtonClicked);
        }

        private void RefreshPanel(bool force)
        {
            if (!ClientRuntime.IsInitialized)
            {
                ApplyMissingState(force);
                return;
            }

            var martialArtState = ClientRuntime.MartialArts;
            var baseStats = ClientRuntime.Character.BaseStats;
            var currentState = ClientRuntime.Character.CurrentState;
            if (!martialArtState.HasLoadedMartialArts)
            {
                ApplyMissingState(force);
                return;
            }

            var snapshot = BuildSnapshot(martialArtState, baseStats, currentState);
            if (!force && string.Equals(lastSnapshot, snapshot, StringComparison.Ordinal))
                return;

            lastSnapshot = snapshot;
            ApplyLoadedState(martialArtState, baseStats, currentState, force: true);
        }

        private void TryReloadMissingData()
        {
            if (!ClientRuntime.IsInitialized || ClientRuntime.Connection.State != ClientConnectionState.Connected)
                return;

            var selectedCharacterId = ClientRuntime.Character.SelectedCharacterId;
            if (!selectedCharacterId.HasValue)
                return;

            if (autoLoadMissingCharacterData &&
                !characterReloadInFlight &&
                (!ClientRuntime.Character.BaseStats.HasValue || !ClientRuntime.Character.CurrentState.HasValue) &&
                CanRetryReload(lastRequestedCharacterId, lastCharacterReloadAttemptTime, selectedCharacterId.Value))
            {
                _ = ReloadCharacterDataAsync(selectedCharacterId.Value);
            }

            if (autoLoadMissingMartialArts &&
                !martialArtReloadInFlight &&
                !ClientRuntime.MartialArts.HasLoadedMartialArts &&
                CanRetryReload(lastRequestedCharacterId, lastMartialArtReloadAttemptTime, selectedCharacterId.Value))
            {
                _ = ReloadMartialArtsAsync(selectedCharacterId.Value);
            }
        }
    }
}
