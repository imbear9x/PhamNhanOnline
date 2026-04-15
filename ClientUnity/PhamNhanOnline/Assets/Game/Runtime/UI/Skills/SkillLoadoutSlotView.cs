using System;
using GameShared.Models;
using PhamNhanOnline.Client.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Skills
{
    public sealed class SkillLoadoutSlotView : MonoBehaviour, IUiDragPayloadSource, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text slotLabelText;
        [SerializeField] private GameObject emptyStateRoot;
        [SerializeField] private GameObject occupiedStateRoot;
        [SerializeField] private GameObject selectedHighlightRoot;

        [Header("Slot")]
        [SerializeField] private int slotIndex = 1;

        [Header("Drag")]
        [SerializeField] private float draggingAlpha = 0.65f;

        private PlayerSkillModel item;
        private bool hasItem;
        private bool dragEnabled = true;
        private bool isSelected;
        private CanvasGroup canvasGroup;
        private SkillDragGhost dragGhost;
        private Sprite currentIconSprite;

        public event Action<int, PlayerSkillModel> SkillDropped;

        public int SlotIndex => slotIndex;
        public PlayerSkillModel Item => item;
        public bool HasItem => hasItem;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            ApplyEmptyState();
        }

        public void SetSlotIndex(int value, bool force = false)
        {
            slotIndex = Math.Max(1, value);
            if (slotLabelText != null)
                slotLabelText.text = slotIndex.ToString();
        }

        public void SetItem(PlayerSkillModel value, SkillPresentation presentation, bool force = false)
        {
            hasItem = true;
            item = value;
            ApplyPresentation(presentation);
            ApplyIconVisibility(true);
            if (emptyStateRoot != null)
                emptyStateRoot.SetActive(false);
            if (occupiedStateRoot != null)
                occupiedStateRoot.SetActive(true);
            if (force)
                SetSelected(isSelected, true);
        }

        public void Clear(bool force = false)
        {
            hasItem = false;
            item = default(PlayerSkillModel);
            currentIconSprite = null;
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            ResetDragVisuals();
            ApplyEmptyState();
            SetSelected(false, force);
        }

        public void SetSelected(bool selected, bool force = false)
        {
            if (!force && isSelected == selected)
                return;

            isSelected = selected;
            if (selectedHighlightRoot != null)
                selectedHighlightRoot.SetActive(selected);
        }

        public void SetDragEnabled(bool value)
        {
            dragEnabled = value;
            if (!value)
                ResetDragVisuals();
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (!UiDragPayloadResolver.TryResolve(eventData, out var payload) ||
                payload.Kind != UiDragPayloadKind.Skill ||
                !payload.HasSkill)
            {
                return;
            }

            if (payload.SourceKind != UiDragSourceKind.SkillListItem &&
                payload.SourceKind != UiDragSourceKind.SkillLoadoutSlot)
            {
                return;
            }

            if (payload.SourceKind == UiDragSourceKind.SkillLoadoutSlot &&
                payload.HasSourceIndex &&
                payload.SourceIndex == slotIndex)
            {
                return;
            }

            DispatchDroppedSkill(payload.Skill);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!dragEnabled || !hasItem || canvasGroup == null)
                return;

            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = draggingAlpha;
            dragGhost = SkillDragGhost.Create(transform, currentIconSprite, eventData);
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

            payload = UiDragPayload.FromSkill(item, UiDragSourceKind.SkillLoadoutSlot, slotIndex);
            return true;
        }

        private void DispatchDroppedSkill(PlayerSkillModel skill)
        {
            var handler = SkillDropped;
            if (handler != null)
                handler(slotIndex, skill);
        }

        private void ApplyEmptyState()
        {
            ApplyIconVisibility(false);
            if (emptyStateRoot != null)
                emptyStateRoot.SetActive(true);
            if (occupiedStateRoot != null)
                occupiedStateRoot.SetActive(false);
        }

        private void ApplyIconVisibility(bool visible)
        {
            if (iconImage != null)
                iconImage.gameObject.SetActive(visible);
        }

        private void ApplyPresentation(SkillPresentation presentation)
        {
            currentIconSprite = presentation.IconSprite;
            if (iconImage == null)
                return;

            iconImage.sprite = presentation.IconSprite;
            iconImage.enabled = presentation.IconSprite != null;
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
        }
    }

    internal sealed class SkillDragGhost
    {
        private readonly RectTransform rootRect;
        private readonly RectTransform canvasRect;
        private readonly GameObject rootObject;

        private SkillDragGhost(GameObject rootObject, RectTransform rootRect, RectTransform canvasRect)
        {
            this.rootObject = rootObject;
            this.rootRect = rootRect;
            this.canvasRect = canvasRect;
        }

        public static SkillDragGhost Create(Transform source, Sprite iconSprite, PointerEventData eventData)
        {
            if (source == null)
                return null;

            var canvas = source.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.rootCanvas == null)
                return null;

            var rootCanvas = canvas.rootCanvas;
            var rootObject = new GameObject("SkillDragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
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

            var ghost = new SkillDragGhost(rootObject, rootRect, rootCanvas.transform as RectTransform);
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
