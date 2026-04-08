using System;
using System.Globalization;
using GameShared.Models;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Skills
{
    public sealed class SkillListItemView : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [Header("References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private TMP_Text cooldownText;
        [SerializeField] private GameObject selectedHighlightRoot;

        [Header("Display")]
        [SerializeField] private float draggingAlpha = 0.65f;

        private PlayerSkillModel item;
        private bool hasItem;
        private bool isSelected;
        private CanvasGroup canvasGroup;
        private SkillDragGhost dragGhost;
        private Sprite currentIconSprite;

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

        public void SetItem(PlayerSkillModel value, SkillPresentation presentation, bool force = false)
        {
            hasItem = true;
            item = value;
            ApplyPresentation(presentation);

            if (nameText != null)
                nameText.text = string.IsNullOrWhiteSpace(value.Name) ? "Skill" : value.Name.Trim();

            if (detailText != null)
            {
                var header = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} | Cap {1} | Mo tang {2}",
                    string.IsNullOrWhiteSpace(value.SourceMartialArtName) ? "Khong ro nguon" : value.SourceMartialArtName.Trim(),
                    Math.Max(1, value.SkillLevel),
                    Math.Max(0, value.UnlockStage));
                detailText.text = string.IsNullOrWhiteSpace(value.Description)
                    ? header
                    : string.Concat(header, Environment.NewLine, value.Description.Trim());
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

            if (nameText != null)
                nameText.text = string.Empty;
            if (detailText != null)
                detailText.text = string.Empty;
            if (cooldownText != null)
                cooldownText.text = string.Empty;

            ResetDragVisuals();
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

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!hasItem)
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

        private void ApplyPresentation(SkillPresentation presentation)
        {
            currentIconSprite = presentation.IconSprite;
            if (iconImage == null)
                return;

            iconImage.sprite = presentation.IconSprite;
            iconImage.enabled = presentation.IconSprite != null;
        }
    }
}
