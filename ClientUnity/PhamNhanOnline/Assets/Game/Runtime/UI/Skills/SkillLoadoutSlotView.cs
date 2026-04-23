using System;
using GameShared.Models;
using PhamNhanOnline.Client.UI.Common;
using PhamNhanOnline.Client.UI.Inventory;
using PhamNhanOnline.Client.UI.World;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Skills
{
    public sealed class SkillLoadoutSlotView : MonoBehaviour,
        IUIDragPayloadSource,
        IDropHandler,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerClickHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [Header("References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text slotLabelText;
        [SerializeField] private GameObject emptyStateRoot;
        [SerializeField] private GameObject selectedHighlightRoot;

        [Header("Drag")]
        [SerializeField] private float draggingAlpha = 0.65f;

        private int slotIndex = 1;
        private PlayerSkillModel item;
        private bool hasItem;
        private bool dragEnabled = true;
        private bool isSelected;
        private bool dragSelectionVisible;
        private CanvasGroup canvasGroup;
        private SkillDragGhost dragGhost;
        private Sprite currentIconSprite;
        private SkillPresentation currentPresentation;

        public event Action<int, PlayerSkillModel, int?> SkillDropped;
        public event Action<SkillLoadoutSlotView> Clicked;

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
            if (force)
                SetSelected(isSelected, true);
            else
                ApplySelectionVisual();
        }

        public void Clear(bool force = false)
        {
            hasItem = false;
            item = default(PlayerSkillModel);
            currentIconSprite = null;
            currentPresentation = default;
            dragSelectionVisible = false;
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
            if (eventData != null && IsValidDraggedSkill(eventData.pointerDrag != null ? eventData.pointerDrag.transform : null))
                SetDragSelectionVisible(true);

            if (eventData != null && eventData.pointerDrag != null)
                return;

            if (!hasItem)
                return;

            WorldModalUIManager.Instance?.ShowItemTooltip(this, BuildTooltipData(), force: true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetDragSelectionVisible(false);

            if (!hasItem)
                return;

            WorldModalUIManager.Instance?.HideItemTooltip(this, force: true);
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
            if (!hasItem || !dragEnabled)
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
                payload.Kind != UIDragPayloadKind.Skill ||
                !payload.HasSkill)
            {
                return;
            }

            if (payload.SourceKind != UIDragSourceKind.SkillListItem &&
                payload.SourceKind != UIDragSourceKind.SkillLoadoutSlot)
            {
                return;
            }

            if (payload.SourceKind == UIDragSourceKind.SkillLoadoutSlot &&
                payload.HasSourceIndex &&
                payload.SourceIndex == slotIndex)
            {
                return;
            }

            DispatchDroppedSkill(payload.Skill, payload.HasSourceIndex ? payload.SourceIndex : (int?)null);
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

            payload = UIDragPayload.FromSkill(item, UIDragSourceKind.SkillLoadoutSlot, slotIndex);
            return true;
        }

        private void DispatchDroppedSkill(PlayerSkillModel skill, int? sourceSlotIndex)
        {
            var handler = SkillDropped;
            if (handler != null)
                handler(slotIndex, skill, sourceSlotIndex);
        }

        private void ApplyEmptyState()
        {
            ApplyIconVisibility(false);
            if (emptyStateRoot != null)
                emptyStateRoot.SetActive(true);
        }

        private void ApplyIconVisibility(bool visible)
        {
            if (iconImage != null)
                iconImage.gameObject.SetActive(visible);
        }

        private void ApplyPresentation(SkillPresentation presentation)
        {
            currentPresentation = presentation;
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

            SetDragSelectionVisible(false);
        }

        private bool IsValidDraggedSkill(Transform dragTransform)
        {
            if (!UIDragPayloadResolver.TryResolve(dragTransform, out var payload) ||
                payload.Kind != UIDragPayloadKind.Skill ||
                !payload.HasSkill)
            {
                return false;
            }

            if (payload.SourceKind != UIDragSourceKind.SkillListItem &&
                payload.SourceKind != UIDragSourceKind.SkillLoadoutSlot)
            {
                return false;
            }

            return !payload.HasSourceIndex || payload.SourceIndex != slotIndex;
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
            var visible = hasItem || isSelected || dragSelectionVisible;
            if (selectedHighlightRoot != null && selectedHighlightRoot.activeSelf != visible)
                selectedHighlightRoot.SetActive(visible);
        }

        private ItemTooltipViewData BuildTooltipData()
        {
            var description = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "Cap {0}",
                Math.Max(0, item.SkillLevel));

            if (item.CastRange > 0f)
            {
                description = string.Concat(
                    description,
                    Environment.NewLine,
                    string.Format(System.Globalization.CultureInfo.InvariantCulture, "Tam thi trien: {0:0.##}", Math.Max(0f, item.CastRange)));
            }

            description = string.Concat(
                description,
                Environment.NewLine,
                string.Format(System.Globalization.CultureInfo.InvariantCulture, "Hoi chieu: {0:0.##}s", Math.Max(0d, item.CooldownMs / 1000d)));

            if (!string.IsNullOrWhiteSpace(item.SourceMartialArtName))
                description = string.Concat(description, Environment.NewLine, "Cong phap: ", item.SourceMartialArtName.Trim());

            if (!string.IsNullOrWhiteSpace(item.Description))
                description = string.Concat(description, Environment.NewLine, item.Description.Trim());

            return new ItemTooltipViewData(
                string.IsNullOrWhiteSpace(item.Name) ? "Skill" : item.Name.Trim(),
                description,
                currentPresentation.IconSprite,
                Color.white);
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
