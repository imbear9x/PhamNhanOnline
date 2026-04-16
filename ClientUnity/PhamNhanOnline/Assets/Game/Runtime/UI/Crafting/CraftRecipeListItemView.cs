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
    public sealed class CraftRecipeListItemView : LoopScrollViewItem,
        IUIDragPayloadSource,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [Header("References")]
        [SerializeField] private Transform dragVisualRoot;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
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

            if (dragVisualRoot == null && iconImage != null)
                dragVisualRoot = iconImage.transform.parent != null ? iconImage.transform.parent : iconImage.transform;
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

            if (nameText != null)
                nameText.text = string.IsNullOrWhiteSpace(value.Name) ? "Dan phuong" : value.Name.Trim();

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

            if (nameText != null)
                nameText.text = string.Empty;

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

        public bool TryCreateDragPayload(out UIDragPayload payload)
        {
            if (!hasRecipe)
            {
                payload = default;
                return false;
            }

            payload = UIDragPayload.FromRecipe(recipe, UIDragSourceKind.CraftRecipeListItem);
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

        private void ValidateSerializedReferences()
        {
            ThrowIfMissing(iconImage, nameof(iconImage));
        }

        private void ThrowIfMissing(UnityEngine.Object value, string fieldName)
        {
            if (value == null)
                throw new InvalidOperationException($"{nameof(CraftRecipeListItemView)} on '{gameObject.name}' is missing required reference '{fieldName}'.");
        }
    }
}
