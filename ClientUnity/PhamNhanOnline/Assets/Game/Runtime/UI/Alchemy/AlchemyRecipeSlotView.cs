using System;
using GameShared.Models;
using PhamNhanOnline.Client.UI.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Alchemy
{
    public sealed class AlchemyRecipeSlotView : MonoBehaviour,
        IDropHandler,
        IPointerClickHandler,
        IPointerEnterHandler,
        IPointerExitHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [Header("References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private GameObject emptyStateRoot;
        [SerializeField] private GameObject occupiedStateRoot;
        [SerializeField] private GameObject lockedRoot;

        [Header("Text")]
        [SerializeField] private string emptyName = "Keo dan phuong vao day";
        [SerializeField] private string emptyDetail = "Dan phuong da hoc khong bi mat di khi dua vao o nay.";

        [Header("Drag")]
        [SerializeField] private float draggingAlpha = 0.65f;

        private LearnedPillRecipeModel recipe;
        private bool hasRecipe;
        private bool dragEnabled = true;
        private bool dropEnabled = true;
        private CanvasGroup canvasGroup;
        private AlchemyRecipeDragGhost dragGhost;
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

            ApplyEmptyState();
        }

        private void Start()
        {
            ValidateSerializedReferences();
        }

        public void SetRecipe(LearnedPillRecipeModel value, InventoryItemPresentation presentation)
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
                detailText.text = string.Concat("Ket qua: ", string.IsNullOrWhiteSpace(value.ResultPill.Name) ? "?" : value.ResultPill.Name.Trim());

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

            if (backgroundImage != null)
                backgroundImage.sprite = null;

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

            if (eventData.pointerDrag == null)
                return;

            var listItemView = eventData.pointerDrag.GetComponentInParent<AlchemyRecipeListItemView>();
            if (listItemView != null && listItemView.HasRecipe)
            {
                RecipeDropped?.Invoke(listItemView.Recipe);
                return;
            }

            var slotView = eventData.pointerDrag.GetComponentInParent<AlchemyRecipeSlotView>();
            if (slotView != null && slotView != this && slotView.HasRecipe)
            {
                RecipeDropped?.Invoke(slotView.Recipe);
                return;
            }
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
            dragGhost = AlchemyRecipeDragGhost.Create(transform, currentPresentation.IconSprite, recipe.Name, eventData);
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
            if (nameText != null)
                nameText.text = emptyName;
            if (detailText != null)
                detailText.text = emptyDetail;
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
            ThrowIfMissing(backgroundImage, nameof(backgroundImage));
        }

        private void ThrowIfMissing(UnityEngine.Object value, string fieldName)
        {
            if (value == null)
                throw new InvalidOperationException($"{nameof(AlchemyRecipeSlotView)} on '{gameObject.name}' is missing required reference '{fieldName}'.");
        }
    }
}
