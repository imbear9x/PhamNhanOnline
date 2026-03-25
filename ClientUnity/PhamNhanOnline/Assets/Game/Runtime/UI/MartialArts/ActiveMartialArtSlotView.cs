using System;
using GameShared.Models;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.MartialArts
{
    public sealed class ActiveMartialArtSlotView : MonoBehaviour, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("References")]
        [SerializeField] private Image iconImage;

        [Header("Drag")]
        [SerializeField] private float draggingAlpha = 0.65f;

        private PlayerMartialArtModel item;
        private bool hasItem;
        private bool dragEnabled;
        private CanvasGroup canvasGroup;
        private MartialArtDragGhost dragGhost;
        private Sprite currentIconSprite;

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
            hasItem = true;
            item = value;
            ApplyPresentation(presentation);
            ApplyIconVisibility(true);
        }

        public void Clear(bool force = false)
        {
            hasItem = false;
            item = default(PlayerMartialArtModel);
            dragEnabled = false;
            currentIconSprite = null;
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }
            ResetDragVisuals();
            ApplyEmptyState();
        }

        public void SetDragEnabled(bool value)
        {
            dragEnabled = value;
            if (!value)
                ResetDragVisuals();
        }

        public void OnDrop(PointerEventData eventData)
        {
            var listItemView = eventData.pointerDrag != null
                ? eventData.pointerDrag.GetComponentInParent<MartialArtListItemView>()
                : null;

            if (listItemView == null || !listItemView.HasItem)
                return;

            var handler = MartialArtDropped;
            if (handler != null)
                handler(listItemView.Item);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!dragEnabled || !hasItem || canvasGroup == null)
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

        private void ApplyEmptyState()
        {
            ApplyIconVisibility(false);
        }

        private void ApplyIconVisibility(bool visible)
        {
            if (iconImage != null)
                iconImage.gameObject.SetActive(visible);
        }

        private void ApplyPresentation(MartialArtPresentation presentation)
        {
            currentIconSprite = presentation.IconSprite;
            if (iconImage != null)
            {
                iconImage.sprite = presentation.IconSprite;
                iconImage.enabled = presentation.IconSprite != null;
            }
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
