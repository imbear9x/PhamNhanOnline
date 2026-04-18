using System;
using System.Globalization;
using GameShared.Models;
using PhamNhanOnline.Client.UI.Common;
using PhamNhanOnline.Client.UI.Inventory;
using PhamNhanOnline.Client.UI.World;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.MartialArts
{
    public sealed class MartialArtListItemView : LoopScrollViewItem,
        IUIDragPayloadSource,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerClickHandler,
        IDropHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [Header("References")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject activeIndicatorRoot;
        [SerializeField] private GameObject selectedHighlightRoot;

        [Header("Display")]
        [SerializeField] private float draggingAlpha = 0.65f;

        private PlayerMartialArtModel item;
        private bool hasItem;
        private int lastMartialArtId = int.MinValue;
        private int lastCurrentStage = int.MinValue;
        private int lastMaxStage = int.MinValue;
        private bool lastActiveState;
        private double lastQiAbsorptionRate = double.MinValue;
        private Sprite lastIconSprite;
        private bool isSelected;
        private CanvasGroup canvasGroup;
        private MartialArtDragGhost dragGhost;
        private MartialArtPresentation currentPresentation;

        public event Action<MartialArtListItemView> Clicked;
        public event Action<MartialArtListItemView> Hovered;
        public event Action<MartialArtListItemView> HoverExited;
        public event Action<PlayerMartialArtModel> ActiveMartialArtDropped;

        public PlayerMartialArtModel Item => item;
        public bool HasItem => hasItem;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void SetItem(PlayerMartialArtModel value, bool force = false)
        {
            SetItem(value, new MartialArtPresentation(null), force);
        }

        public void SetItem(PlayerMartialArtModel value, MartialArtPresentation presentation, bool force = false)
        {
            hasItem = true;
            item = value;
            currentPresentation = presentation;

            if (!force &&
                lastMartialArtId == value.MartialArtId &&
                lastCurrentStage == value.CurrentStage &&
                lastMaxStage == value.MaxStage &&
                lastActiveState == value.IsActive &&
                Math.Abs(lastQiAbsorptionRate - value.QiAbsorptionRate) < 0.0001d &&
                lastIconSprite == presentation.IconSprite)
            {
                return;
            }

            lastMartialArtId = value.MartialArtId;
            lastCurrentStage = value.CurrentStage;
            lastMaxStage = value.MaxStage;
            lastActiveState = value.IsActive;
            lastQiAbsorptionRate = value.QiAbsorptionRate;
            lastIconSprite = presentation.IconSprite;

            if (iconImage != null)
            {
                iconImage.sprite = presentation.IconSprite;
                iconImage.color = presentation.IconSprite != null
                    ? Color.white
                    : new Color(1f, 1f, 1f, 0f);
            }

            if (activeIndicatorRoot != null)
                activeIndicatorRoot.SetActive(value.IsActive);

        }

        public void Clear(bool force = false)
        {
            hasItem = false;
            item = default(PlayerMartialArtModel);
            currentPresentation = default;
            lastMartialArtId = int.MinValue;
            lastCurrentStage = int.MinValue;
            lastMaxStage = int.MinValue;
            lastActiveState = false;
            lastQiAbsorptionRate = double.MinValue;
            lastIconSprite = null;

            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.color = new Color(1f, 1f, 1f, 0f);
            }

            if (activeIndicatorRoot != null)
                activeIndicatorRoot.SetActive(false);

            ResetDragVisuals();
            if (force)
                SetSelected(false, force: true);
        }

        public override void OnItemRecycled()
        {
            Clear(force: true);
        }

        public void SetSelected(bool selected, bool force = false)
        {
            if (!force && isSelected == selected)
                return;

            isSelected = selected;
            if (selectedHighlightRoot != null)
                selectedHighlightRoot.SetActive(selected);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData != null && eventData.pointerDrag != null)
                return;

            if (!hasItem)
                return;

            WorldModalUIManager.Instance?.ShowItemTooltip(this, BuildTooltipData(), force: true);
            var handler = Hovered;
            if (handler != null)
                handler(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!hasItem)
                return;

            WorldModalUIManager.Instance?.HideItemTooltip(this, force: true);
            var handler = HoverExited;
            if (handler != null)
                handler(this);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!hasItem)
                return;

            WorldModalUIManager.Instance?.BeginItemInteraction(this, force: true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!hasItem)
                return;

            WorldModalUIManager.Instance?.EndItemInteraction(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!hasItem)
                return;

            var handler = Clicked;
            if (handler != null)
                handler(this);

            eventData?.Use();
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (!UIDragPayloadResolver.TryResolve(eventData, out var payload) ||
                payload.Kind != UIDragPayloadKind.MartialArt ||
                payload.SourceKind != UIDragSourceKind.ActiveMartialArtSlot ||
                !payload.HasMartialArt)
            {
                return;
            }

            var handler = ActiveMartialArtDropped;
            if (handler != null)
                handler(payload.MartialArt);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!hasItem)
                return;

            var modalUIManager = WorldModalUIManager.Instance;
            if (modalUIManager != null)
            {
                modalUIManager.HideItemOptionsPopup(force: true);
                modalUIManager.BeginItemInteraction(this, force: true);
            }

            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = draggingAlpha;
            dragGhost = MartialArtDragGhost.Create(transform, currentPresentation.IconSprite, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragGhost != null)
                dragGhost.UpdatePosition(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            ResetDragVisuals();
            var modalUIManager = WorldModalUIManager.Instance;
            if (modalUIManager != null)
            {
                modalUIManager.EndItemInteraction(this);
                modalUIManager.HideItemTooltip(this, force: true);
            }
        }

        public bool TryCreateDragPayload(out UIDragPayload payload)
        {
            if (!hasItem)
            {
                payload = default;
                return false;
            }

            payload = UIDragPayload.FromMartialArt(item, UIDragSourceKind.MartialArtListItem);
            return true;
        }

        private void ResetDragVisuals()
        {
            if (canvasGroup == null)
                return;

            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;

            if (dragGhost != null)
            {
                dragGhost.Dispose();
                dragGhost = null;
            }
        }

        private ItemTooltipViewData BuildTooltipData()
        {
            var header = string.Format(
                CultureInfo.InvariantCulture,
                "{0} | Tang {1}/{2}",
                string.IsNullOrWhiteSpace(item.Category) ? "Cong phap" : item.Category.Trim(),
                Math.Max(0, item.CurrentStage),
                Math.Max(0, item.MaxStage));
            var description = string.Format(
                CultureInfo.InvariantCulture,
                "{0}{1}Qi x{2:0.##}",
                header,
                Environment.NewLine,
                Math.Max(0d, item.QiAbsorptionRate));

            if (!string.IsNullOrWhiteSpace(item.Description))
                description = string.Concat(description, Environment.NewLine, item.Description.Trim());

            return new ItemTooltipViewData(
                item.Name,
                description,
                currentPresentation.IconSprite,
                Color.white);
        }
    }
}
