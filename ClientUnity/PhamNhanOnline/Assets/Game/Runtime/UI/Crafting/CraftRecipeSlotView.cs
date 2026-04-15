using System;
using GameShared.Models;
using PhamNhanOnline.Client.UI.Common;
using PhamNhanOnline.Client.UI.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Crafting
{
    public sealed class CraftRecipeSlotView : MonoBehaviour,
        IUiDragPayloadSource,
        IDropHandler,
        IPointerClickHandler,
        IPointerEnterHandler,
        IPointerExitHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [Header("References")]
        [SerializeField] private Transform dragVisualRoot;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text durationText;
        [SerializeField] private TMP_Text successRateText;
        [SerializeField] private GameObject detailsRoot;
        [SerializeField] private GameObject emptyStateRoot;
        [SerializeField] private GameObject occupiedStateRoot;
        [SerializeField] private GameObject lockedRoot;

        [Header("Text")]
        [SerializeField] private string emptyName = "Empty Name";

        [Header("Drag")]
        [SerializeField] private float draggingAlpha = 0.65f;

        private LearnedPillRecipeModel recipe;
        private bool hasRecipe;
        private bool dragEnabled = true;
        private bool dropEnabled = true;
        private CanvasGroup canvasGroup;
        private CraftRecipeDragGhost dragGhost;
        private InventoryItemPresentation currentPresentation;

        public event Action<LearnedPillRecipeModel> RecipeDropped;
        public event Action Clicked;
        public event Action Hovered;
        public event Action HoverExited;

        public LearnedPillRecipeModel Recipe => recipe;
        public bool HasRecipe => hasRecipe;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (dragVisualRoot == null && iconImage != null)
                dragVisualRoot = iconImage.transform.parent != null ? iconImage.transform.parent : iconImage.transform;

            ApplyEmptyState();
        }

        private void Start()
        {
            ValidateSerializedReferences();
        }

        public void SetRecipe(
            LearnedPillRecipeModel value,
            InventoryItemPresentation presentation,
            string durationLabel,
            string successRateLabel)
        {
            recipe = value;
            hasRecipe = true;
            currentPresentation = presentation;

            if (iconImage != null)
            {
                iconImage.sprite = presentation.IconSprite;
                iconImage.enabled = presentation.IconSprite != null;
            }

            if (nameText != null)
            {
                nameText.text = string.IsNullOrWhiteSpace(value.Name) ? "Dan phuong" : value.Name.Trim();
                nameText.color = presentation.NameColor;
            }

            if (durationText != null)
                durationText.text = string.IsNullOrWhiteSpace(durationLabel) ? string.Empty : durationLabel.Trim();

            if (successRateText != null)
                successRateText.text = string.IsNullOrWhiteSpace(successRateLabel) ? string.Empty : successRateLabel.Trim();

            if (detailsRoot != null)
                detailsRoot.SetActive(true);

            if (emptyStateRoot != null)
                emptyStateRoot.SetActive(false);
            if (occupiedStateRoot != null)
                occupiedStateRoot.SetActive(true);
        }

        public void Clear()
        {
            hasRecipe = false;
            recipe = default;
            currentPresentation = default;

            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            ApplyEmptyState();
            ResetDragVisuals();
        }

        public void SetInteractionLocked(bool locked)
        {
            dragEnabled = !locked;
            dropEnabled = !locked;
            if (lockedRoot != null)
                lockedRoot.SetActive(locked);

            if (locked)
                ResetDragVisuals();
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (!dropEnabled)
                return;

            if (!UiDragPayloadResolver.TryResolve(eventData, out var payload) ||
                payload.Kind != UiDragPayloadKind.Recipe ||
                !payload.HasRecipe)
            {
                return;
            }

            if (payload.SourceKind != UiDragSourceKind.CraftRecipeListItem &&
                payload.SourceKind != UiDragSourceKind.CraftRecipeSlot)
            {
                return;
            }

            if (payload.SourceKind == UiDragSourceKind.CraftRecipeSlot &&
                hasRecipe &&
                recipe.PillRecipeTemplateId == payload.Recipe.PillRecipeTemplateId)
            {
                return;
            }

            RecipeDropped?.Invoke(payload.Recipe);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Clicked?.Invoke();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (hasRecipe)
                Hovered?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (hasRecipe)
                HoverExited?.Invoke();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!dragEnabled)
                return;

            if (!hasRecipe)
                return;

            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = draggingAlpha;
            dragGhost = CraftRecipeDragGhost.Create(transform, dragVisualRoot, currentPresentation.IconSprite, recipe.Name, eventData);
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
            if (!hasRecipe)
            {
                payload = default;
                return false;
            }

            payload = UiDragPayload.FromRecipe(recipe, UiDragSourceKind.CraftRecipeSlot);
            return true;
        }

        private void ApplyEmptyState()
        {
            if (nameText != null)
                nameText.text = emptyName;
            if (durationText != null)
                durationText.text = string.Empty;
            if (successRateText != null)
                successRateText.text = string.Empty;
            if (detailsRoot != null)
                detailsRoot.SetActive(false);
            if (emptyStateRoot != null)
                emptyStateRoot.SetActive(true);
            if (occupiedStateRoot != null)
                occupiedStateRoot.SetActive(false);
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

        private void ValidateSerializedReferences()
        {
            ThrowIfMissing(iconImage, nameof(iconImage));
        }

        private void ThrowIfMissing(UnityEngine.Object value, string fieldName)
        {
            if (value == null)
                throw new InvalidOperationException($"{nameof(CraftRecipeSlotView)} on '{gameObject.name}' is missing required reference '{fieldName}'.");
        }
    }
}
