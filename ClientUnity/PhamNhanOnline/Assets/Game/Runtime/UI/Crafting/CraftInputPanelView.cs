using System;
using System.Collections.Generic;
using System.Globalization;
using GameShared.Models;
using PhamNhanOnline.Client.UI.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace PhamNhanOnline.Client.UI.Crafting
{
    public sealed class CraftInputPanelView : MonoBehaviour
    {
        public readonly struct SlotState
        {
            public SlotState(
                int inputId,
                int acceptedItemTemplateId,
                InventoryItemPresentation presentation,
                int currentQuantity,
                int requiredQuantity,
                bool hasSelection,
                bool locked,
                bool showEmptyIcon)
            {
                InputId = inputId;
                AcceptedItemTemplateId = acceptedItemTemplateId;
                Presentation = presentation;
                CurrentQuantity = currentQuantity;
                RequiredQuantity = requiredQuantity;
                HasSelection = hasSelection;
                Locked = locked;
                ShowEmptyIcon = showEmptyIcon;
            }

            public int InputId { get; }
            public int AcceptedItemTemplateId { get; }
            public InventoryItemPresentation Presentation { get; }
            public int CurrentQuantity { get; }
            public int RequiredQuantity { get; }
            public bool HasSelection { get; }
            public bool Locked { get; }
            public bool ShowEmptyIcon { get; }
        }

        [Header("References")]
        [FormerlySerializedAs("requiredIngredientSlotsRoot")]
        [SerializeField] private RectTransform requiredInputSlotsRoot;
        [FormerlySerializedAs("requiredIngredientSlotTemplate")]
        [SerializeField] private CraftInputMaterialSlotView requiredInputSlotTemplate;
        [FormerlySerializedAs("optionalIngredientSlotsRoot")]
        [SerializeField] private RectTransform optionalInputSlotsRoot;
        [FormerlySerializedAs("optionalIngredientSlotTemplate")]
        [SerializeField] private CraftInputMaterialSlotView optionalInputSlotTemplate;

        [Header("Behavior")]
        [FormerlySerializedAs("maxRequiredIngredientSlots")]
        [SerializeField] [Range(1, 6)] private int maxRequiredInputSlots = 6;

        private readonly List<CraftInputMaterialSlotView> requiredInputSlotViews = new List<CraftInputMaterialSlotView>();
        private readonly Dictionary<CraftInputMaterialSlotView, int> requiredInputIdBySlotView = new Dictionary<CraftInputMaterialSlotView, int>();
        private readonly List<CraftInputMaterialSlotView> optionalInputSlotViews = new List<CraftInputMaterialSlotView>();
        private readonly Dictionary<CraftInputMaterialSlotView, int> optionalInputIdBySlotView = new Dictionary<CraftInputMaterialSlotView, int>();

        public event Action<int, InventoryItemModel> InventoryItemDropped;
        public event Action<int, bool, PointerEventData.InputButton> SlotClicked;
        public event Action<int, bool> SlotHovered;
        public event Action SlotHoverExited;

        private void Start()
        {
            ValidateSerializedReferences();
            RebuildRequiredInputSlots(0);
            RebuildOptionalInputSlots(0);
        }

        private void OnDestroy()
        {
            UnbindRequiredInputSlots();
            UnbindOptionalInputSlots();
        }

        public void SetSlots(
            IReadOnlyList<SlotState> requiredSlots,
            IReadOnlyList<SlotState> optionalSlots)
        {
            ApplyRequiredSlots(requiredSlots ?? Array.Empty<SlotState>());
            ApplyOptionalSlots(optionalSlots ?? Array.Empty<SlotState>());
        }

        public void Clear()
        {
            ClearRequiredInputViews();
            ClearOptionalInputViews();
        }

        private void ApplyRequiredSlots(IReadOnlyList<SlotState> requiredSlots)
        {
            if (requiredSlots == null || requiredSlots.Count == 0)
            {
                ClearRequiredInputViews();
                return;
            }

            var slotCount = Math.Min(Math.Max(1, maxRequiredInputSlots), requiredSlots.Count);
            if (requiredSlots.Count > slotCount)
            {
                Debug.LogError(
                    $"CraftInputPanelView on '{gameObject.name}' received {requiredSlots.Count} required slots but UI supports only {slotCount}.");
            }

            RebuildRequiredInputSlots(slotCount);
            for (var i = 0; i < requiredInputSlotViews.Count; i++)
            {
                var slotView = requiredInputSlotViews[i];
                if (i >= requiredSlots.Count)
                {
                    slotView.gameObject.SetActive(false);
                    continue;
                }

                var slotState = requiredSlots[i];
                requiredInputIdBySlotView[slotView] = slotState.InputId;
                slotView.gameObject.SetActive(true);
                slotView.SetState(
                    slotState.InputId,
                    slotState.AcceptedItemTemplateId,
                    slotState.Presentation,
                    slotState.CurrentQuantity,
                    slotState.RequiredQuantity,
                    slotState.HasSelection,
                    slotState.Locked,
                    slotState.ShowEmptyIcon);
            }
        }

        private void ApplyOptionalSlots(IReadOnlyList<SlotState> optionalSlots)
        {
            if (optionalSlots == null || optionalSlots.Count == 0)
            {
                ClearOptionalInputViews();
                return;
            }

            RebuildOptionalInputSlots(optionalSlots.Count);
            for (var i = 0; i < optionalInputSlotViews.Count; i++)
            {
                var slotView = optionalInputSlotViews[i];
                if (i >= optionalSlots.Count)
                {
                    slotView.gameObject.SetActive(false);
                    continue;
                }

                var slotState = optionalSlots[i];
                optionalInputIdBySlotView[slotView] = slotState.InputId;
                slotView.gameObject.SetActive(true);
                slotView.SetState(
                    slotState.InputId,
                    slotState.AcceptedItemTemplateId,
                    slotState.Presentation,
                    slotState.CurrentQuantity,
                    slotState.RequiredQuantity,
                    slotState.HasSelection,
                    slotState.Locked,
                    slotState.ShowEmptyIcon);
            }
        }

        private void RebuildRequiredInputSlots(int requiredCount)
        {
            if (requiredInputSlotsRoot == null || requiredInputSlotTemplate == null)
                return;

            requiredInputSlotTemplate.gameObject.SetActive(false);
            while (requiredInputSlotViews.Count < requiredCount)
            {
                var slotView = Instantiate(requiredInputSlotTemplate, requiredInputSlotsRoot);
                slotView.name = string.Concat(requiredInputSlotTemplate.name, "_", requiredInputSlotViews.Count.ToString(CultureInfo.InvariantCulture));
                slotView.gameObject.SetActive(true);
                slotView.InventoryItemDropped += HandleSlotInventoryItemDropped;
                slotView.Clicked += HandleSlotClicked;
                slotView.Hovered += HandleSlotHovered;
                slotView.HoverExited += HandleSlotHoverExited;
                requiredInputSlotViews.Add(slotView);
            }

            while (requiredInputSlotViews.Count > requiredCount)
            {
                var index = requiredInputSlotViews.Count - 1;
                var slotView = requiredInputSlotViews[index];
                requiredInputSlotViews.RemoveAt(index);
                requiredInputIdBySlotView.Remove(slotView);
                slotView.InventoryItemDropped -= HandleSlotInventoryItemDropped;
                slotView.Clicked -= HandleSlotClicked;
                slotView.Hovered -= HandleSlotHovered;
                slotView.HoverExited -= HandleSlotHoverExited;
                Destroy(slotView.gameObject);
            }

            requiredInputSlotsRoot.gameObject.SetActive(requiredCount > 0);
        }

        private void RebuildOptionalInputSlots(int requiredCount)
        {
            if (optionalInputSlotsRoot == null || optionalInputSlotTemplate == null)
                return;

            optionalInputSlotTemplate.gameObject.SetActive(false);
            while (optionalInputSlotViews.Count < requiredCount)
            {
                var slotView = Instantiate(optionalInputSlotTemplate, optionalInputSlotsRoot);
                slotView.name = string.Concat(optionalInputSlotTemplate.name, "_", optionalInputSlotViews.Count.ToString(CultureInfo.InvariantCulture));
                slotView.gameObject.SetActive(true);
                slotView.InventoryItemDropped += HandleSlotInventoryItemDropped;
                slotView.Clicked += HandleSlotClicked;
                slotView.Hovered += HandleSlotHovered;
                slotView.HoverExited += HandleSlotHoverExited;
                optionalInputSlotViews.Add(slotView);
            }

            while (optionalInputSlotViews.Count > requiredCount)
            {
                var index = optionalInputSlotViews.Count - 1;
                var slotView = optionalInputSlotViews[index];
                optionalInputSlotViews.RemoveAt(index);
                optionalInputIdBySlotView.Remove(slotView);
                slotView.InventoryItemDropped -= HandleSlotInventoryItemDropped;
                slotView.Clicked -= HandleSlotClicked;
                slotView.Hovered -= HandleSlotHovered;
                slotView.HoverExited -= HandleSlotHoverExited;
                Destroy(slotView.gameObject);
            }

            optionalInputSlotsRoot.gameObject.SetActive(requiredCount > 0);
        }

        private void HandleSlotInventoryItemDropped(CraftInputMaterialSlotView slotView, InventoryItemModel item)
        {
            var inputId = ResolveInputId(slotView);
            if (inputId <= 0)
                return;

            InventoryItemDropped?.Invoke(inputId, item);
        }

        private void HandleSlotClicked(CraftInputMaterialSlotView slotView, PointerEventData.InputButton button)
        {
            var inputId = ResolveInputId(slotView);
            if (inputId <= 0)
                return;

            SlotClicked?.Invoke(inputId, optionalInputIdBySlotView.ContainsKey(slotView), button);
        }

        private void HandleSlotHovered(CraftInputMaterialSlotView slotView)
        {
            var inputId = ResolveInputId(slotView);
            if (inputId <= 0)
                return;

            SlotHovered?.Invoke(inputId, optionalInputIdBySlotView.ContainsKey(slotView));
        }

        private void HandleSlotHoverExited(CraftInputMaterialSlotView slotView)
        {
            SlotHoverExited?.Invoke();
        }

        private int ResolveInputId(CraftInputMaterialSlotView slotView)
        {
            if (slotView == null)
                return 0;

            if (requiredInputIdBySlotView.TryGetValue(slotView, out var requiredInputId))
                return requiredInputId;

            if (optionalInputIdBySlotView.TryGetValue(slotView, out var optionalInputId))
                return optionalInputId;

            return 0;
        }

        private void UnbindRequiredInputSlots()
        {
            for (var i = 0; i < requiredInputSlotViews.Count; i++)
            {
                var slotView = requiredInputSlotViews[i];
                if (slotView == null)
                    continue;

                slotView.InventoryItemDropped -= HandleSlotInventoryItemDropped;
                slotView.Clicked -= HandleSlotClicked;
                slotView.Hovered -= HandleSlotHovered;
                slotView.HoverExited -= HandleSlotHoverExited;
            }
        }

        private void UnbindOptionalInputSlots()
        {
            for (var i = 0; i < optionalInputSlotViews.Count; i++)
            {
                var slotView = optionalInputSlotViews[i];
                if (slotView == null)
                    continue;

                slotView.InventoryItemDropped -= HandleSlotInventoryItemDropped;
                slotView.Clicked -= HandleSlotClicked;
                slotView.Hovered -= HandleSlotHovered;
                slotView.HoverExited -= HandleSlotHoverExited;
            }
        }

        private void ClearRequiredInputViews()
        {
            if (requiredInputSlotsRoot != null)
                requiredInputSlotsRoot.gameObject.SetActive(false);

            for (var i = 0; i < requiredInputSlotViews.Count; i++)
            {
                var slotView = requiredInputSlotViews[i];
                if (slotView == null)
                    continue;

                slotView.gameObject.SetActive(false);
                slotView.Clear();
            }
        }

        private void ClearOptionalInputViews()
        {
            if (optionalInputSlotsRoot != null)
                optionalInputSlotsRoot.gameObject.SetActive(false);

            for (var i = 0; i < optionalInputSlotViews.Count; i++)
            {
                var slotView = optionalInputSlotViews[i];
                if (slotView == null)
                    continue;

                slotView.gameObject.SetActive(false);
                slotView.Clear();
            }
        }

        private void ValidateSerializedReferences()
        {
            ThrowIfMissing(requiredInputSlotsRoot, nameof(requiredInputSlotsRoot));
            ThrowIfMissing(requiredInputSlotTemplate, nameof(requiredInputSlotTemplate));
            ThrowIfMissing(optionalInputSlotsRoot, nameof(optionalInputSlotsRoot));
            ThrowIfMissing(optionalInputSlotTemplate, nameof(optionalInputSlotTemplate));
        }

        private void ThrowIfMissing(UnityEngine.Object value, string fieldName)
        {
            if (value == null)
                throw new InvalidOperationException($"{nameof(CraftInputPanelView)} on '{gameObject.name}' is missing required reference '{fieldName}'.");
        }
    }
}
