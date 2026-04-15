using System;
using System.Globalization;
using GameShared.Models;
using PhamNhanOnline.Client.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.MartialArts
{
    public sealed class MartialArtListItemView : MonoBehaviour,
        IUiDragPayloadSource,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler,
        IDropHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [Header("References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private TMP_Text qiRateText;
        [SerializeField] private GameObject activeBadgeRoot;
        [SerializeField] private GameObject selectedHighlightRoot;

        [Header("Display")]
        [SerializeField] private float draggingAlpha = 0.65f;

        private PlayerMartialArtModel item;
        private bool hasItem;
        private bool isSelected;
        private CanvasGroup canvasGroup;
        private MartialArtDragGhost dragGhost;
        private Sprite currentIconSprite;

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
            ApplyPresentation(presentation);

            if (nameText != null)
                nameText.text = string.IsNullOrWhiteSpace(value.Name) ? "Cong phap" : value.Name.Trim();

            if (detailText != null)
            {
                var header = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} | Tang {1}/{2}",
                    string.IsNullOrWhiteSpace(value.Category) ? "Chua phan loai" : value.Category.Trim(),
                    Math.Max(0, value.CurrentStage),
                    Math.Max(0, value.MaxStage));
                detailText.text = string.IsNullOrWhiteSpace(value.Description)
                    ? header
                    : string.Concat(header, Environment.NewLine, value.Description.Trim());
            }

            if (qiRateText != null)
            {
                qiRateText.text = string.Format(
                    CultureInfo.InvariantCulture,
                    "Qi x{0:0.##}",
                    Math.Max(0d, value.QiAbsorptionRate));
            }

            if (activeBadgeRoot != null)
                activeBadgeRoot.SetActive(value.IsActive);

            if (force)
                SetSelected(isSelected, force: true);
        }

        public void Clear(bool force = false)
        {
            hasItem = false;
            item = default(PlayerMartialArtModel);
            currentIconSprite = null;

            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }
            if (nameText != null)
                nameText.text = string.Empty;
            if (detailText != null)
                detailText.text = string.Empty;
            if (qiRateText != null)
                qiRateText.text = string.Empty;
            if (activeBadgeRoot != null)
                activeBadgeRoot.SetActive(false);

            ResetDragVisuals();
            SetSelected(false, force: force);
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
            if (!hasItem)
                return;

            var handler = Hovered;
            if (handler != null)
                handler(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!hasItem)
                return;

            var handler = HoverExited;
            if (handler != null)
                handler(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!hasItem)
                return;

            var handler = Clicked;
            if (handler != null)
                handler(this);
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (!UiDragPayloadResolver.TryResolve(eventData, out var payload) ||
                payload.Kind != UiDragPayloadKind.MartialArt ||
                payload.SourceKind != UiDragSourceKind.ActiveMartialArtSlot ||
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

            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = draggingAlpha;
            dragGhost = MartialArtDragGhost.Create(transform, currentIconSprite, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragGhost != null)
                dragGhost.UpdatePosition(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            ResetDragVisuals();
        }

        public bool TryCreateDragPayload(out UiDragPayload payload)
        {
            if (!hasItem)
            {
                payload = default;
                return false;
            }

            payload = UiDragPayload.FromMartialArt(item, UiDragSourceKind.MartialArtListItem);
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

        private void ApplyPresentation(MartialArtPresentation presentation)
        {
            currentIconSprite = presentation.IconSprite;
            if (iconImage == null)
                return;

            iconImage.sprite = presentation.IconSprite;
            iconImage.enabled = presentation.IconSprite != null;
        }
    }
}
