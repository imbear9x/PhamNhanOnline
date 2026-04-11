using System;
using GameShared.Models;
using PhamNhanOnline.Client.UI.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Crafting
{
    public sealed class CraftRecipeListItemView : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [Header("References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private TMP_Text durationText;
        [SerializeField] private GameObject selectedHighlightRoot;

        [Header("Drag")]
        [SerializeField] private float draggingAlpha = 0.65f;

        private LearnedPillRecipeModel recipe;
        private bool hasRecipe;
        private bool isSelected;
        private CanvasGroup canvasGroup;
        private CraftRecipeDragGhost dragGhost;
        private InventoryItemPresentation currentPresentation;

        public event Action<CraftRecipeListItemView> Clicked;
        public event Action<CraftRecipeListItemView> Hovered;
        public event Action<CraftRecipeListItemView> HoverExited;

        public LearnedPillRecipeModel Recipe => recipe;
        public bool HasRecipe => hasRecipe;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        private void Start()
        {
            ValidateSerializedReferences();
        }

        public void SetRecipe(LearnedPillRecipeModel value, InventoryItemPresentation presentation, bool force = false)
        {
            recipe = value;
            hasRecipe = true;
            currentPresentation = presentation;

            if (iconImage != null)
            {
                iconImage.sprite = presentation.IconSprite;
                iconImage.enabled = presentation.IconSprite != null;
            }

            if (backgroundImage != null)
                backgroundImage.sprite = presentation.BackgroundSprite;

            if (nameText != null)
            {
                nameText.text = string.IsNullOrWhiteSpace(value.Name) ? "Dan phuong" : value.Name.Trim();
                nameText.color = presentation.NameColor;
            }

            if (detailText != null)
            {
                var resultName = string.IsNullOrWhiteSpace(value.ResultPill.Name)
                    ? "Khong ro ket qua"
                    : value.ResultPill.Name.Trim();
                detailText.text = string.Concat("Ket qua: ", resultName);
            }

            if (durationText != null)
                durationText.text = string.Concat("T/g: ", FormatDuration(value.CraftDurationSeconds));

            if (force)
                SetSelected(isSelected, force: true);
        }

        public void Clear(bool force = false)
        {
            hasRecipe = false;
            recipe = default;
            currentPresentation = default;
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            if (backgroundImage != null)
                backgroundImage.sprite = null;

            if (nameText != null)
                nameText.text = string.Empty;
            if (detailText != null)
                detailText.text = string.Empty;
            if (durationText != null)
                durationText.text = string.Empty;

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
            if (!hasRecipe)
                return;

            Hovered?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!hasRecipe)
                return;

            HoverExited?.Invoke(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!hasRecipe)
                return;

            Clicked?.Invoke(this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!hasRecipe)
                return;

            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = draggingAlpha;
            dragGhost = CraftRecipeDragGhost.Create(transform, currentPresentation.IconSprite, recipe.Name, eventData);
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

        private static string FormatDuration(long totalSeconds)
        {
            var clamped = Math.Max(0L, totalSeconds);
            if (clamped >= 3600L)
                return TimeSpan.FromSeconds(clamped).ToString(@"hh\:mm\:ss");

            return TimeSpan.FromSeconds(clamped).ToString(@"mm\:ss");
        }

        private void ValidateSerializedReferences()
        {
            ThrowIfMissing(iconImage, nameof(iconImage));
            ThrowIfMissing(backgroundImage, nameof(backgroundImage));
        }

        private void ThrowIfMissing(UnityEngine.Object value, string fieldName)
        {
            if (value == null)
                throw new InvalidOperationException($"{nameof(CraftRecipeListItemView)} on '{gameObject.name}' is missing required reference '{fieldName}'.");
        }
    }
}
