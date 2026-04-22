using System;
using System.Collections.Generic;
using GameShared.Models;
using PhamNhanOnline.Client.Features.Character.Application;
using PhamNhanOnline.Client.UI.Common;
using PhamNhanOnline.Client.UI.Inventory;
using PhamNhanOnline.Client.UI.MartialArts;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.World
{
    [DisallowMultipleComponent]
    public sealed class WorldCultivationPanelView : ViewModelBase
    {
        [Header("Panel Root")]
        [SerializeField] private GameObject panelRoot;

        [Header("References")]
        [SerializeField] private MartialArtPresentationCatalog presentationCatalog;
        [SerializeField] private WorldCultivationPreviewView cultivationPreviewView;
        [SerializeField] private ArtMaterialSummaryView activeArtMaterialSummaryView;
        [SerializeField] private ActiveMartialArtSlotView activeMartialArtSlotView;
        [SerializeField] private MartialArtListView martialArtListView;
        [SerializeField] private GameObject breakthroughRoot;
        [SerializeField] private UIButtonView startCultivationButton;
        [SerializeField] private UIButtonView stopCultivationButton;
        [SerializeField] private UIButtonView breakthroughButton;
        [SerializeField] private UIButtonView closeButton;

        public event Action<PlayerMartialArtModel> ActiveMartialArtDropped;
        public event Action<ActiveMartialArtSlotView> ActiveMartialArtClicked;
        public event Action<PlayerMartialArtModel> MartialArtListItemClicked;
        public event Action<PlayerMartialArtModel> ActiveMartialArtDroppedToList;
        public event Action StartCultivationClicked;
        public event Action StopCultivationClicked;
        public event Action BreakthroughClicked;
        public event Action CloseClicked;

        protected override GameObject ResolveViewRoot()
        {
            return panelRoot != null ? panelRoot : gameObject;
        }

        protected override void Awake()
        {
            base.Awake();
            BindChildEvents();
        }

        private void OnDestroy()
        {
            UnbindChildEvents();
        }

        public void ValidateSerializedReferences()
        {
            ThrowIfMissing(presentationCatalog, nameof(presentationCatalog));
            ThrowIfMissing(cultivationPreviewView, nameof(cultivationPreviewView));
            ThrowIfMissing(activeArtMaterialSummaryView, nameof(activeArtMaterialSummaryView));
            ThrowIfMissing(activeMartialArtSlotView, nameof(activeMartialArtSlotView));
            ThrowIfMissing(martialArtListView, nameof(martialArtListView));
            ThrowIfMissing(breakthroughRoot, nameof(breakthroughRoot));
            ThrowIfMissing(startCultivationButton, nameof(startCultivationButton));
            ThrowIfMissing(stopCultivationButton, nameof(stopCultivationButton));
            ThrowIfMissing(breakthroughButton, nameof(breakthroughButton));
            cultivationPreviewView.ValidateSerializedReferences();
        }

        public void Show()
        {
            ShowView();
        }

        public void Hide(bool force = false)
        {
            SetViewVisible(false, force);
        }

        private void SetActiveArtVisualState(bool hasActiveMartialArt, bool force)
        {
            if (activeMartialArtSlotView != null && (force || !activeMartialArtSlotView.gameObject.activeSelf))
                activeMartialArtSlotView.gameObject.SetActive(true);

            if (activeArtMaterialSummaryView != null)
            {
                if (hasActiveMartialArt)
                    activeArtMaterialSummaryView.Show(force);
                else
                    activeArtMaterialSummaryView.Hide(force);
            }
        }

        public void SetActiveMartialArt(
            PlayerMartialArtModel martialArt,
            bool isSelected,
            bool dragEnabled,
            bool force)
        {
            var presentation = presentationCatalog != null
                ? presentationCatalog.Resolve(martialArt)
                : new MartialArtPresentation(null);
            var levelText = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "T\u1EA7ng {0}",
                Math.Max(0, martialArt.CurrentStage));
            var expText = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0}   /   {1}",
                Math.Max(0L, martialArt.CurrentExp),
                Math.Max(0L, martialArt.ExpRequired));
            var artQiRate = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0:0.##}%",
                Math.Max(0d, martialArt.QiAbsorptionRate) * 100d);

            activeMartialArtSlotView?.SetItem(martialArt, presentation, force);
            activeMartialArtSlotView?.SetSelected(isSelected, force);
            activeMartialArtSlotView?.SetDragEnabled(dragEnabled);
            activeArtMaterialSummaryView?.SetData(
                presentation.IconSprite,
                martialArt.Name ?? string.Empty,
                artQiRate,
                levelText,
                expText,
                Math.Max(0L, martialArt.CurrentExp),
                Math.Max(0L, martialArt.ExpRequired),
                force);
            SetActiveArtVisualState(hasActiveMartialArt: true, force: force);
        }

        public void ClearActiveMartialArt(bool force)
        {
            activeMartialArtSlotView?.Clear(force);
            activeArtMaterialSummaryView?.Clear(force);
            SetActiveArtVisualState(hasActiveMartialArt: false, force: force);
        }

        public void SetMartialArtList(
            IReadOnlyList<PlayerMartialArtModel> martialArts,
            int? selectedMartialArtId,
            bool force)
        {
            martialArtListView?.SetItems(martialArts, selectedMartialArtId, presentationCatalog, force);
        }

        public void ClearMartialArtList(bool force)
        {
            martialArtListView?.Clear(force);
        }

        public void SetCultivationPreview(
            PlayerMartialArtModel? activeMartialArt,
            CultivationPreviewModel? preview,
            string estimateText,
            string breakthroughText,
            string statusText,
            bool estimateVisible,
            bool force)
        {
            var name = activeMartialArt.HasValue
                ? activeMartialArt.Value.Name ?? string.Empty
                : string.Empty;
            var icon = activeMartialArt.HasValue && presentationCatalog != null
                ? presentationCatalog.Resolve(activeMartialArt.Value).IconSprite
                : null;
            var levelText = activeMartialArt.HasValue
                ? string.Format(System.Globalization.CultureInfo.InvariantCulture, "T\u1EA7ng {0}", Math.Max(0, activeMartialArt.Value.CurrentStage))
                : string.Empty;
            var expText = activeMartialArt.HasValue
                ? string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "{0}   /   {1}",
                    Math.Max(0L, activeMartialArt.Value.CurrentExp),
                    Math.Max(0L, activeMartialArt.Value.ExpRequired))
                : string.Empty;
            var artQiRate = preview.HasValue
                ? string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.##}%", Math.Max(0d, preview.Value.QiAbsorptionRate) * 100d)
                : activeMartialArt.HasValue
                    ? string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.##}%", Math.Max(0d, activeMartialArt.Value.QiAbsorptionRate) * 100d)
                    : string.Empty;
            var mapQiDensity = preview.HasValue
                ? Math.Max(0d, preview.Value.SpiritualEnergyPerMinute).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)
                : string.Empty;
            var realmQiBonus = preview.HasValue
                ? string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.##}%", Math.Max(0d, preview.Value.RealmAbsorptionMultiplier) * 100d)
                : string.Empty;

            if (activeMartialArt.HasValue)
            {
                activeArtMaterialSummaryView?.SetData(
                    icon,
                    name,
                    artQiRate,
                    levelText,
                    expText,
                    Math.Max(0L, activeMartialArt.Value.CurrentExp),
                    Math.Max(0L, activeMartialArt.Value.ExpRequired),
                    force);
            }
            else
            {
                activeArtMaterialSummaryView?.Clear(force);
            }

            cultivationPreviewView?.SetData(
                name,
                icon,
                levelText,
                expText,
                activeMartialArt.HasValue ? Math.Max(0L, activeMartialArt.Value.CurrentExp) : 0L,
                activeMartialArt.HasValue ? Math.Max(0L, activeMartialArt.Value.ExpRequired) : 0L,
                artQiRate,
                mapQiDensity,
                realmQiBonus,
                estimateText,
                breakthroughText,
                statusText,
                estimateVisible,
                force);
        }

        public void ClearCultivationPreview(bool force)
        {
            cultivationPreviewView?.Clear(force);
        }

        public void ShowRewardExp(CultivationRewardNotice notice)
        {
            cultivationPreviewView?.ShowRewardExp(notice);
        }

        public void SetBreakthroughRootVisible(bool visible)
        {
            if (breakthroughRoot != null)
                breakthroughRoot.SetActive(visible);
        }

        public void SetStartCultivationButtonState(bool visible, bool interactable, bool force)
        {
            SetButtonState(startCultivationButton, visible, interactable, force);
        }

        public void SetStopCultivationButtonState(bool visible, bool interactable, bool force)
        {
            SetButtonState(stopCultivationButton, visible, interactable, force);
        }

        public void SetBreakthroughButtonState(bool visible, bool interactable, bool force)
        {
            SetButtonState(breakthroughButton, visible, interactable, force);
        }

        public void ShowItemOptionsPopup(IReadOnlyList<ItemOptionEntry> options, bool force = false)
        {
            WorldModalUIManager.Instance?.ShowItemOptionsPopup(options, force);
        }

        public void HideItemOptionsPopup(bool force = false)
        {
            WorldModalUIManager.Instance?.HideItemOptionsPopup(force);
        }

        public void HideItemTooltip(bool force = false)
        {
            WorldModalUIManager.Instance?.HideItemTooltip(force: force);
        }

        private void BindChildEvents()
        {
            if (activeMartialArtSlotView != null)
            {
                activeMartialArtSlotView.MartialArtDropped -= HandleActiveMartialArtDropped;
                activeMartialArtSlotView.MartialArtDropped += HandleActiveMartialArtDropped;
                activeMartialArtSlotView.Clicked -= HandleActiveMartialArtClicked;
                activeMartialArtSlotView.Clicked += HandleActiveMartialArtClicked;
            }

            if (martialArtListView != null)
            {
                martialArtListView.ActiveMartialArtDroppedToList -= HandleActiveMartialArtDroppedToList;
                martialArtListView.ActiveMartialArtDroppedToList += HandleActiveMartialArtDroppedToList;
                martialArtListView.ItemClicked -= HandleMartialArtListItemClicked;
                martialArtListView.ItemClicked += HandleMartialArtListItemClicked;
            }

            if (startCultivationButton != null)
            {
                startCultivationButton.Clicked -= HandleStartCultivationClicked;
                startCultivationButton.Clicked += HandleStartCultivationClicked;
            }

            if (stopCultivationButton != null)
            {
                stopCultivationButton.Clicked -= HandleStopCultivationClicked;
                stopCultivationButton.Clicked += HandleStopCultivationClicked;
            }

            if (breakthroughButton != null)
            {
                breakthroughButton.Clicked -= HandleBreakthroughClicked;
                breakthroughButton.Clicked += HandleBreakthroughClicked;
            }

            if (closeButton != null)
            {
                closeButton.Clicked -= HandleCloseClicked;
                closeButton.Clicked += HandleCloseClicked;
            }
        }

        private void UnbindChildEvents()
        {
            if (activeMartialArtSlotView != null)
            {
                activeMartialArtSlotView.MartialArtDropped -= HandleActiveMartialArtDropped;
                activeMartialArtSlotView.Clicked -= HandleActiveMartialArtClicked;
            }

            if (martialArtListView != null)
            {
                martialArtListView.ActiveMartialArtDroppedToList -= HandleActiveMartialArtDroppedToList;
                martialArtListView.ItemClicked -= HandleMartialArtListItemClicked;
            }

            if (startCultivationButton != null)
                startCultivationButton.Clicked -= HandleStartCultivationClicked;

            if (stopCultivationButton != null)
                stopCultivationButton.Clicked -= HandleStopCultivationClicked;

            if (breakthroughButton != null)
                breakthroughButton.Clicked -= HandleBreakthroughClicked;

            if (closeButton != null)
                closeButton.Clicked -= HandleCloseClicked;
        }

        private void HandleActiveMartialArtDropped(PlayerMartialArtModel martialArt)
        {
            ActiveMartialArtDropped?.Invoke(martialArt);
        }

        private void HandleActiveMartialArtClicked(ActiveMartialArtSlotView slotView)
        {
            ActiveMartialArtClicked?.Invoke(slotView);
        }

        private void HandleMartialArtListItemClicked(PlayerMartialArtModel martialArt)
        {
            MartialArtListItemClicked?.Invoke(martialArt);
        }

        private void HandleActiveMartialArtDroppedToList(PlayerMartialArtModel martialArt)
        {
            ActiveMartialArtDroppedToList?.Invoke(martialArt);
        }

        private void HandleStartCultivationClicked()
        {
            StartCultivationClicked?.Invoke();
        }

        private void HandleStopCultivationClicked()
        {
            StopCultivationClicked?.Invoke();
        }

        private void HandleBreakthroughClicked()
        {
            BreakthroughClicked?.Invoke();
        }

        private void HandleCloseClicked()
        {
            CloseClicked?.Invoke();
        }

        private static void SetButtonState(UIButtonView button, bool visible, bool interactable, bool force)
        {
            if (button == null)
                return;

            if (button.gameObject.activeSelf != visible)
                button.gameObject.SetActive(visible);

            button.SetInteractable(interactable, force: force || !visible);
        }

        private void ThrowIfMissing(UnityEngine.Object value, string fieldName)
        {
            if (value == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(WorldCultivationPanelView)} on '{gameObject.name}' is missing required reference '{fieldName}'.");
            }
        }
    }
}
