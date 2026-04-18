using System;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.UI.Common;
using PhamNhanOnline.Client.UI.Inventory;
using PhamNhanOnline.Client.UI.MartialArts;
using TMPro;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.World
{
    public sealed partial class WorldMartialArtPanelController : MonoBehaviour
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

        [Header("References")]
        [SerializeField] private CharacterSummaryView characterSummaryView;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private MartialArtPresentationCatalog presentationCatalog;
        [SerializeField] private ActiveMartialArtSlotView activeMartialArtSlotView;
        [SerializeField] private MartialArtListView martialArtListView;
        [SerializeField] private GameObject estimateRoot;
        [SerializeField] private TMP_Text estimateText;
        [SerializeField] private GameObject breakthroughRoot;
        [SerializeField] private UIButtonView startCultivationButton;
        [SerializeField] private UIButtonView stopCultivationButton;
        [SerializeField] private TMP_Text breakthroughChanceText;
        [SerializeField] private UIButtonView breakthroughButton;

        [Header("Display Text")]
        [SerializeField] private string statusBreakthroughInProgressText = "Dang dot pha";
        [SerializeField] private string statusBreakthroughRequiredText = "Da tu luyen toi dinh phong gap binh canh can dot pha";
        [SerializeField] private string statusCultivatingText = "Dang tu luyen";
        [SerializeField] private string statusNoActiveMartialArtText = "Chua co cong phap";

        private bool actionInFlight;
        private int? popupMartialArtId;
        private bool popupTargetsActiveSlot;
        private PanelActionKind actionKind;
        private string lastStatusMessage = string.Empty;
        private string lastSnapshot = string.Empty;

        private void Awake()
        {
            if (activeMartialArtSlotView != null)
            {
                activeMartialArtSlotView.MartialArtDropped += HandleMartialArtDropped;
                activeMartialArtSlotView.Clicked += HandleActiveMartialArtSlotClicked;
            }

            if (martialArtListView != null)
            {
                martialArtListView.ActiveMartialArtDroppedToList += HandleActiveMartialArtDroppedToList;
                martialArtListView.ItemClicked += HandleMartialArtListItemClicked;
            }

            if (startCultivationButton != null)
            {
                startCultivationButton.Clicked -= HandleStartCultivationButtonClicked;
                startCultivationButton.Clicked += HandleStartCultivationButtonClicked;
            }

            if (stopCultivationButton != null)
            {
                stopCultivationButton.Clicked -= HandleStopCultivationButtonClicked;
                stopCultivationButton.Clicked += HandleStopCultivationButtonClicked;
            }

            if (breakthroughButton != null)
            {
                breakthroughButton.Clicked -= HandleBreakthroughButtonClicked;
                breakthroughButton.Clicked += HandleBreakthroughButtonClicked;
            }
        }

        private void OnEnable()
        {
            RefreshPanel(force: true);
        }

        private void OnDisable()
        {
            HideMartialArtOptionsPopup(force: true);
        }

        private void Update()
        {
            if (!isActiveAndEnabled)
                return;

            RefreshPanel(force: false);
        }

        private void OnDestroy()
        {
            if (activeMartialArtSlotView != null)
            {
                activeMartialArtSlotView.MartialArtDropped -= HandleMartialArtDropped;
                activeMartialArtSlotView.Clicked -= HandleActiveMartialArtSlotClicked;
            }

            if (martialArtListView != null)
            {
                martialArtListView.ActiveMartialArtDroppedToList -= HandleActiveMartialArtDroppedToList;
                martialArtListView.ItemClicked -= HandleMartialArtListItemClicked;
            }

            if (startCultivationButton != null)
                startCultivationButton.Clicked -= HandleStartCultivationButtonClicked;

            if (stopCultivationButton != null)
                stopCultivationButton.Clicked -= HandleStopCultivationButtonClicked;

            if (breakthroughButton != null)
                breakthroughButton.Clicked -= HandleBreakthroughButtonClicked;
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
                    modalUIManager?.HideItemOptionsPopup(force: true);
                    force = true;
                }
            }

            var snapshot = BuildSnapshot(martialArtState, baseStats, currentState);
            if (!force && string.Equals(lastSnapshot, snapshot, StringComparison.Ordinal))
                return;

            lastSnapshot = snapshot;
            ApplyLoadedState(martialArtState, baseStats, currentState, force: true);
        }

    }
}
