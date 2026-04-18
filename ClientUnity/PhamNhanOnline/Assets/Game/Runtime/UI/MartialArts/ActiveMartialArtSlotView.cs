using System;
using GameShared.Models;
using PhamNhanOnline.Client.UI.Common;
using PhamNhanOnline.Client.UI.Inventory;
using PhamNhanOnline.Client.UI.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.MartialArts
{
    public sealed class ActiveMartialArtSlotView : MonoBehaviour,
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
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject emptyStateRoot;
        [SerializeField] private GameObject selectedRoot;

        [Header("Drag")]
        [SerializeField] private float draggingAlpha = 0.65f;

        private PlayerMartialArtModel item;
        private bool hasItem;
        private bool dragEnabled;
        private bool isSelected;
        private bool dragSelectionVisible;
        private CanvasGroup canvasGroup;
        private MartialArtDragGhost dragGhost;
        private Sprite currentIconSprite;
        private MartialArtPresentation currentPresentation;

        public event Action<ActiveMartialArtSlotView> Clicked;
        public event Action<ActiveMartialArtSlotView> Hovered;
        public event Action<ActiveMartialArtSlotView> HoverExited;
        public event Action<PlayerMartialArtModel> MartialArtDropped;

        public PlayerMartialArtModel Item => item;
        public bool HasItem => hasItem;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            ApplyEmptyState();
        }

        public void SetItem(PlayerMartialArtModel value, bool force = false)
        {
            SetItem(value, new MartialArtPresentation(null), force);
        }

        public void SetItem(PlayerMartialArtModel value, MartialArtPresentation presentation, bool force = false)
        {
            _ = force;

            hasItem = true;
            item = value;
            ApplyPresentation(presentation);

            if (emptyStateRoot != null)
                emptyStateRoot.SetActive(false);

            if (iconImage != null)
            {
                iconImage.sprite = presentation.IconSprite;
                iconImage.enabled = presentation.IconSprite != null;
            }

            SetSelected(selected: false, force: true);
        }

        public void Clear(bool force = false)
        {
            _ = force;

            hasItem = false;
            item = default(PlayerMartialArtModel);
            dragEnabled = false;
            isSelected = false;
            dragSelectionVisible = false;
            currentIconSprite = null;
            currentPresentation = default;
            ApplyEmptyState();
        }

        public void SetSelected(bool selected, bool force = false)
        {
            if (!force && isSelected == selected)
                return;

            isSelected = selected;
            ApplySelectionVisual();
        }

        public void SetDragEnabled(bool value)
        {
            dragEnabled = value;
            if (!value)
                ResetDragVisuals();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData != null && IsValidDraggedMartialArt(eventData.pointerDrag != null ? eventData.pointerDrag.transform : null))
                SetDragSelectionVisible(true);

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
            SetDragSelectionVisible(false);

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
            SetDragSelectionVisible(false);

            if (!UIDragPayloadResolver.TryResolve(eventData, out var payload) ||
                payload.Kind != UIDragPayloadKind.MartialArt ||
                payload.SourceKind != UIDragSourceKind.MartialArtListItem ||
                !payload.HasMartialArt)
            {
                return;
            }

            var handler = MartialArtDropped;
            if (handler != null)
                handler(payload.MartialArt);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!dragEnabled || !hasItem || canvasGroup == null)
                return;

            var modalUIManager = WorldModalUIManager.Instance;
            if (modalUIManager != null)
            {
                modalUIManager.HideItemOptionsPopup(force: true);
                modalUIManager.BeginItemInteraction(this, force: true);
            }

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

            payload = UIDragPayload.FromMartialArt(item, UIDragSourceKind.ActiveMartialArtSlot);
            return true;
        }

        private void ApplyEmptyState()
        {
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            if (emptyStateRoot != null)
                emptyStateRoot.SetActive(true);

            ResetDragVisuals();
            SetSelected(false, force: true);
        }

        private void ApplyPresentation(MartialArtPresentation presentation)
        {
            currentPresentation = presentation;
            currentIconSprite = presentation.IconSprite;
        }

        private void ResetDragVisuals()
        {
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.alpha = 1f;
            }

            if (dragGhost != null)
            {
                dragGhost.Dispose();
                dragGhost = null;
            }

            SetDragSelectionVisible(false);
        }

        private bool IsValidDraggedMartialArt(Transform dragTransform)
        {
            if (!UIDragPayloadResolver.TryResolve(dragTransform, out var payload) ||
                payload.Kind != UIDragPayloadKind.MartialArt ||
                payload.SourceKind != UIDragSourceKind.MartialArtListItem ||
                !payload.HasMartialArt)
            {
                return false;
            }

            return true;
        }

        private void SetDragSelectionVisible(bool visible)
        {
            if (dragSelectionVisible == visible)
                return;

            dragSelectionVisible = visible;
            ApplySelectionVisual();
        }

        private void ApplySelectionVisual()
        {
            var visible = isSelected || dragSelectionVisible;
            if (selectedRoot != null && selectedRoot.activeSelf != visible)
                selectedRoot.SetActive(visible);
        }

        private ItemTooltipViewData BuildTooltipData()
        {
            var header = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0} | Tang {1}/{2}",
                string.IsNullOrWhiteSpace(item.Category) ? "Cong phap" : item.Category.Trim(),
                Math.Max(0, item.CurrentStage),
                Math.Max(0, item.MaxStage));
            var description = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
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

    internal sealed class MartialArtDragGhost
    {
        private readonly RectTransform rootRect;
        private readonly RectTransform canvasRect;
        private readonly GameObject rootObject;

        private MartialArtDragGhost(GameObject rootObject, RectTransform rootRect, RectTransform canvasRect)
        {
            this.rootObject = rootObject;
            this.rootRect = rootRect;
            this.canvasRect = canvasRect;
        }

        public static MartialArtDragGhost Create(Transform source, Sprite iconSprite, PointerEventData eventData)
        {
            if (source == null)
                return null;

            var canvas = source.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.rootCanvas == null)
                return null;

            var rootCanvas = canvas.rootCanvas;
            var rootObject = new GameObject("MartialArtDragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            var rootRect = rootObject.GetComponent<RectTransform>();
            rootRect.SetParent(rootCanvas.transform, false);
            rootRect.SetAsLastSibling();
            rootRect.sizeDelta = new Vector2(56f, 56f);

            var canvasGroup = rootObject.GetComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            canvasGroup.alpha = 0.94f;

            var backgroundImage = rootObject.GetComponent<Image>();
            backgroundImage.raycastTarget = false;
            backgroundImage.color = new Color(0f, 0f, 0f, 0.18f);

            var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            var iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.SetParent(rootRect, false);
            iconRect.anchorMin = new Vector2(0.12f, 0.12f);
            iconRect.anchorMax = new Vector2(0.88f, 0.88f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            var iconImage = iconObject.GetComponent<Image>();
            iconImage.raycastTarget = false;
            iconImage.sprite = iconSprite;
            iconImage.color = iconSprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            iconImage.preserveAspect = true;

            var ghost = new MartialArtDragGhost(rootObject, rootRect, rootCanvas.transform as RectTransform);
            ghost.UpdatePosition(eventData);
            return ghost;
        }

        public void UpdatePosition(PointerEventData eventData)
        {
            if (rootRect == null || canvasRect == null || eventData == null)
                return;

            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out localPoint))
            {
                return;
            }

            rootRect.anchoredPosition = localPoint;
        }

        public void Dispose()
        {
            if (rootObject != null)
                UnityEngine.Object.Destroy(rootObject);
        }
    }
}
