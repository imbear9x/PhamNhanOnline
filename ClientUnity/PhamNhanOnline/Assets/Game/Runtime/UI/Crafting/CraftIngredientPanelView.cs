using System;
using System.Collections.Generic;
using System.Globalization;
using GameShared.Models;
using PhamNhanOnline.Client.UI.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PhamNhanOnline.Client.UI.Crafting
{
    public sealed class CraftIngredientPanelView : MonoBehaviour
    {
        public readonly struct SlotState
        {
            public SlotState(
                int inputId,
                InventoryItemPresentation presentation,
                int currentQuantity,
                int requiredQuantity,
                bool hasSelection,
                bool locked,
                bool showEmptyIcon)
            {
                InputId = inputId;
                Presentation = presentation;
                CurrentQuantity = currentQuantity;
                RequiredQuantity = requiredQuantity;
                HasSelection = hasSelection;
                Locked = locked;
                ShowEmptyIcon = showEmptyIcon;
            }

            public int InputId { get; }
            public InventoryItemPresentation Presentation { get; }
            public int CurrentQuantity { get; }
            public int RequiredQuantity { get; }
            public bool HasSelection { get; }
            public bool Locked { get; }
            public bool ShowEmptyIcon { get; }
        }

        [Header("References")]
        [SerializeField] private RectTransform requiredIngredientSlotsRoot;
        [SerializeField] private CraftMaterialSlotView requiredIngredientSlotTemplate;
        [SerializeField] private RectTransform optionalIngredientSlotsRoot;
        [SerializeField] private CraftMaterialSlotView optionalIngredientSlotTemplate;

        [Header("Behavior")]
        [SerializeField] [Range(1, 6)] private int maxRequiredIngredientSlots = 6;

        private readonly List<CraftMaterialSlotView> requiredIngredientSlotViews = new List<CraftMaterialSlotView>();
        private readonly Dictionary<CraftMaterialSlotView, int> requiredInputIdBySlotView = new Dictionary<CraftMaterialSlotView, int>();
        private readonly List<CraftMaterialSlotView> optionalIngredientSlotViews = new List<CraftMaterialSlotView>();
        private readonly Dictionary<CraftMaterialSlotView, int> optionalInputIdBySlotView = new Dictionary<CraftMaterialSlotView, int>();

        public event Action<int, InventoryItemModel> InventoryItemDropped;
        public event Action<int, bool, PointerEventData.InputButton> SlotClicked;

        private void Start()
        {
            ValidateSerializedReferences();
            RebuildRequiredIngredientSlots(0);
            RebuildOptionalIngredientSlots(0);
        }

        private void OnDestroy()
        {
            UnbindRequiredIngredientSlots();
            UnbindOptionalIngredientSlots();
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
            ClearRequiredIngredientViews();
            ClearOptionalIngredientViews();
        }

        private void ApplyRequiredSlots(IReadOnlyList<SlotState> requiredSlots)
        {
            if (requiredSlots == null || requiredSlots.Count == 0)
            {
                ClearRequiredIngredientViews();
                return;
            }

            var slotCount = Math.Min(Math.Max(1, maxRequiredIngredientSlots), requiredSlots.Count);
            if (requiredSlots.Count > slotCount)
            {
                Debug.LogError(
                    $"CraftIngredientPanelView on '{gameObject.name}' received {requiredSlots.Count} required slots but UI supports only {slotCount}.");
            }

            RebuildRequiredIngredientSlots(slotCount);
            for (var i = 0; i < requiredIngredientSlotViews.Count; i++)
            {
                var slotView = requiredIngredientSlotViews[i];
                if (i >= requiredSlots.Count)
                {
                    slotView.gameObject.SetActive(false);
                    continue;
                }

                var slotState = requiredSlots[i];
                requiredInputIdBySlotView[slotView] = slotState.InputId;
                slotView.gameObject.SetActive(true);
                slotView.SetState(
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
                ClearOptionalIngredientViews();
                return;
            }

            RebuildOptionalIngredientSlots(optionalSlots.Count);
            for (var i = 0; i < optionalIngredientSlotViews.Count; i++)
            {
                var slotView = optionalIngredientSlotViews[i];
                if (i >= optionalSlots.Count)
                {
                    slotView.gameObject.SetActive(false);
                    continue;
                }

                var slotState = optionalSlots[i];
                optionalInputIdBySlotView[slotView] = slotState.InputId;
                slotView.gameObject.SetActive(true);
                slotView.SetState(
                    slotState.Presentation,
                    slotState.CurrentQuantity,
                    slotState.RequiredQuantity,
                    slotState.HasSelection,
                    slotState.Locked,
                    slotState.ShowEmptyIcon);
            }
        }

        private void RebuildRequiredIngredientSlots(int requiredCount)
        {
            if (requiredIngredientSlotsRoot == null || requiredIngredientSlotTemplate == null)
                return;

            requiredIngredientSlotTemplate.gameObject.SetActive(false);
            while (requiredIngredientSlotViews.Count < requiredCount)
            {
                var slotView = Instantiate(requiredIngredientSlotTemplate, requiredIngredientSlotsRoot);
                slotView.name = string.Concat(requiredIngredientSlotTemplate.name, "_", requiredIngredientSlotViews.Count.ToString(CultureInfo.InvariantCulture));
                slotView.gameObject.SetActive(true);
                slotView.InventoryItemDropped += HandleSlotInventoryItemDropped;
                slotView.Clicked += HandleSlotClicked;
                requiredIngredientSlotViews.Add(slotView);
            }

            while (requiredIngredientSlotViews.Count > requiredCount)
            {
                var index = requiredIngredientSlotViews.Count - 1;
                var slotView = requiredIngredientSlotViews[index];
                requiredIngredientSlotViews.RemoveAt(index);
                requiredInputIdBySlotView.Remove(slotView);
                slotView.InventoryItemDropped -= HandleSlotInventoryItemDropped;
                slotView.Clicked -= HandleSlotClicked;
                Destroy(slotView.gameObject);
            }

            requiredIngredientSlotsRoot.gameObject.SetActive(requiredCount > 0);
        }

        private void RebuildOptionalIngredientSlots(int requiredCount)
        {
            if (optionalIngredientSlotsRoot == null || optionalIngredientSlotTemplate == null)
                return;

            optionalIngredientSlotTemplate.gameObject.SetActive(false);
            while (optionalIngredientSlotViews.Count < requiredCount)
            {
                var slotView = Instantiate(optionalIngredientSlotTemplate, optionalIngredientSlotsRoot);
                slotView.name = string.Concat(optionalIngredientSlotTemplate.name, "_", optionalIngredientSlotViews.Count.ToString(CultureInfo.InvariantCulture));
                slotView.gameObject.SetActive(true);
                slotView.InventoryItemDropped += HandleSlotInventoryItemDropped;
                slotView.Clicked += HandleSlotClicked;
                optionalIngredientSlotViews.Add(slotView);
            }

            while (optionalIngredientSlotViews.Count > requiredCount)
            {
                var index = optionalIngredientSlotViews.Count - 1;
                var slotView = optionalIngredientSlotViews[index];
                optionalIngredientSlotViews.RemoveAt(index);
                optionalInputIdBySlotView.Remove(slotView);
                slotView.InventoryItemDropped -= HandleSlotInventoryItemDropped;
                slotView.Clicked -= HandleSlotClicked;
                Destroy(slotView.gameObject);
            }

            optionalIngredientSlotsRoot.gameObject.SetActive(requiredCount > 0);
        }

        private void HandleSlotInventoryItemDropped(CraftMaterialSlotView slotView, InventoryItemModel item)
        {
            var inputId = ResolveInputId(slotView);
            if (inputId <= 0)
                return;

            InventoryItemDropped?.Invoke(inputId, item);
        }

        private void HandleSlotClicked(CraftMaterialSlotView slotView, PointerEventData.InputButton button)
        {
            var inputId = ResolveInputId(slotView);
            if (inputId <= 0)
                return;

            SlotClicked?.Invoke(inputId, optionalInputIdBySlotView.ContainsKey(slotView), button);
        }

        private int ResolveInputId(CraftMaterialSlotView slotView)
        {
            if (slotView == null)
                return 0;

            if (requiredInputIdBySlotView.TryGetValue(slotView, out var requiredInputId))
                return requiredInputId;

            if (optionalInputIdBySlotView.TryGetValue(slotView, out var optionalInputId))
                return optionalInputId;

            return 0;
        }

        private void UnbindRequiredIngredientSlots()
        {
            for (var i = 0; i < requiredIngredientSlotViews.Count; i++)
            {
                var slotView = requiredIngredientSlotViews[i];
                if (slotView == null)
                    continue;

                slotView.InventoryItemDropped -= HandleSlotInventoryItemDropped;
                slotView.Clicked -= HandleSlotClicked;
            }
        }

        private void UnbindOptionalIngredientSlots()
        {
            for (var i = 0; i < optionalIngredientSlotViews.Count; i++)
            {
                var slotView = optionalIngredientSlotViews[i];
                if (slotView == null)
                    continue;

                slotView.InventoryItemDropped -= HandleSlotInventoryItemDropped;
                slotView.Clicked -= HandleSlotClicked;
            }
        }

        private void ClearRequiredIngredientViews()
        {
            if (requiredIngredientSlotsRoot != null)
                requiredIngredientSlotsRoot.gameObject.SetActive(false);

            for (var i = 0; i < requiredIngredientSlotViews.Count; i++)
            {
                var slotView = requiredIngredientSlotViews[i];
                if (slotView == null)
                    continue;

                slotView.gameObject.SetActive(false);
                slotView.Clear();
            }
        }

        private void ClearOptionalIngredientViews()
        {
            if (optionalIngredientSlotsRoot != null)
                optionalIngredientSlotsRoot.gameObject.SetActive(false);

            for (var i = 0; i < optionalIngredientSlotViews.Count; i++)
            {
                var slotView = optionalIngredientSlotViews[i];
                if (slotView == null)
                    continue;

                slotView.gameObject.SetActive(false);
                slotView.Clear();
            }
        }

        private void ValidateSerializedReferences()
        {
            ThrowIfMissing(requiredIngredientSlotsRoot, nameof(requiredIngredientSlotsRoot));
            ThrowIfMissing(requiredIngredientSlotTemplate, nameof(requiredIngredientSlotTemplate));
            ThrowIfMissing(optionalIngredientSlotsRoot, nameof(optionalIngredientSlotsRoot));
            ThrowIfMissing(optionalIngredientSlotTemplate, nameof(optionalIngredientSlotTemplate));
        }

        private void ThrowIfMissing(UnityEngine.Object value, string fieldName)
        {
            if (value == null)
                throw new InvalidOperationException($"{nameof(CraftIngredientPanelView)} on '{gameObject.name}' is missing required reference '{fieldName}'.");
        }
    }
}
