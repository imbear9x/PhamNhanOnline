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

namespace PhamNhanOnline.Client.UI.Skills
{
    public sealed class SkillListItemView : MonoBehaviour,
        IUIDragPayloadSource,
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
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text cooldownText;
        [SerializeField] private GameObject slotRoot;
        [SerializeField] private TMP_Text skillIndexText;
        [SerializeField] private GameObject selectedHighlightRoot;

        [Header("Display")]
        [SerializeField] private float draggingAlpha = 0.65f;

        private PlayerSkillModel item;
        private bool hasItem;
        private bool isSelected;
        private CanvasGroup canvasGroup;
        private SkillDragGhost dragGhost;
        private Sprite currentIconSprite;
        private bool canAssignToLoadout;

        public event Action<SkillListItemView> Clicked;
        public event Action<SkillListItemView> Hovered;
        public event Action<SkillListItemView> HoverExited;

        public PlayerSkillModel Item => item;
        public bool HasItem => hasItem;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void SetItem(PlayerSkillModel value, SkillUIPresentation presentation, bool force = false)
        {
            hasItem = true;
            item = value;
            canAssignToLoadout = value.CanAssignToLoadout;
            ApplyPresentation(presentation);

            if (nameText != null)
                nameText.text = string.IsNullOrWhiteSpace(value.Name) ? "Skill" : value.Name.Trim();

            if (slotRoot != null)
            {
                slotRoot.SetActive(value.IsEquipped && value.EquippedSlotIndex > 0);
            }

            if (skillIndexText != null)
            {
                skillIndexText.text = value.IsEquipped && value.EquippedSlotIndex > 0
                    ? value.EquippedSlotIndex.ToString(CultureInfo.InvariantCulture)
                    : string.Empty;
            }

            if (cooldownText != null)
            {
                cooldownText.text = string.Format(
                    CultureInfo.InvariantCulture,
                    "CD {0:0.##}s",
                    Math.Max(0d, value.CooldownMs / 1000d));
            }

            if (force)
                SetSelected(isSelected, force: true);

            if (canvasGroup != null)
                canvasGroup.alpha = canAssignToLoadout ? 1f : 0.72f;
        }

        public void Clear(bool force = false)
        {
            hasItem = false;
            item = default(PlayerSkillModel);
            currentIconSprite = null;
            canAssignToLoadout = false;

            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            if (nameText != null)
                nameText.text = string.Empty;
            if (cooldownText != null)
                cooldownText.text = string.Empty;
            if (skillIndexText != null)
                skillIndexText.text = string.Empty;
            if (slotRoot != null)
                slotRoot.SetActive(false);

            ResetDragVisuals();
            SetSelected(false, force);

            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
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

            if (!canAssignToLoadout)
                return;

            var handler = Clicked;
            if (handler != null)
                handler(this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!hasItem || !canAssignToLoadout)
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
            if (!hasItem || !canAssignToLoadout)
            {
                payload = default;
                return false;
            }

            payload = UIDragPayload.FromSkill(
                item,
                UIDragSourceKind.SkillListItem,
                item.IsEquipped && item.EquippedSlotIndex > 0 ? item.EquippedSlotIndex : null);
            return true;
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

        private void ApplyPresentation(SkillUIPresentation presentation)
        {
            currentIconSprite = presentation.IconSprite;
            if (iconImage == null)
                return;

            iconImage.sprite = presentation.IconSprite;
            iconImage.enabled = presentation.IconSprite != null;
        }

        private ItemTooltipViewData BuildTooltipData()
        {
            var description = string.Format(
                CultureInfo.InvariantCulture,
                "Cap {0}",
                Math.Max(0, item.SkillLevel));

            if (item.CastRange > 0f)
            {
                description = string.Concat(
                    description,
                    Environment.NewLine,
                    string.Format(CultureInfo.InvariantCulture, "Tam thi trien: {0:0.##}", Math.Max(0f, item.CastRange)));
            }

            description = string.Concat(
                description,
                Environment.NewLine,
                string.Format(CultureInfo.InvariantCulture, "Hoi chieu: {0:0.##}s", Math.Max(0d, item.CooldownMs / 1000d)));

            if (!string.IsNullOrWhiteSpace(item.SourceMartialArtName))
                description = string.Concat(description, Environment.NewLine, "Cong phap: ", item.SourceMartialArtName.Trim());

            if (!string.IsNullOrWhiteSpace(item.Description))
                description = string.Concat(description, Environment.NewLine, item.Description.Trim());

            return new ItemTooltipViewData(
                string.IsNullOrWhiteSpace(item.Name) ? "Skill" : item.Name.Trim(),
                description,
                currentIconSprite,
                Color.white);
        }
    }
}
