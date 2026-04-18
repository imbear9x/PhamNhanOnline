using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GameShared.Models;
using PhamNhanOnline.Client.UI.Crafting;
using PhamNhanOnline.Client.UI.Inventory;
using PhamNhanOnline.Client.UI.Potential;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.World
{
    [DisallowMultipleComponent]
    public sealed class WorldModalUIManager : MonoBehaviour
    {
        private enum ModalViewKind
        {
            None = 0,
            ItemTooltip = 1,
            CraftRecipeTooltip = 2,
            ItemOptionsPopup = 3,
            QuantityPopup = 4,
            PotentialUpgradeOptionsPopup = 5
        }

        public static WorldModalUIManager Instance { get; private set; }

        [Header("Tooltip References")]
        [SerializeField] private ItemTooltipView inventoryItemTooltipView;
        [SerializeField] private CraftRecipeTooltipView craftRecipeTooltipView;

        [Header("Tooltip Order")]
        [SerializeField] private int inventoryItemTooltipOrderId = 100;
        [SerializeField] private int craftRecipeTooltipOrderId = 110;

        [Header("Popup References")]
        [SerializeField] private ItemOptionsPopupView inventoryItemOptionsPopupView;
        [SerializeField] private InventoryUseQuantityPopupView inventoryUseQuantityPopupView;
        [SerializeField] private PotentialUpgradeOptionsPopupView potentialUpgradeOptionsPopupView;

        [Header("Popup Order")]
        [SerializeField] private int inventoryItemOptionsPopupOrderId = 200;
        [SerializeField] private int quantityPopupOrderId = 210;
        [SerializeField] private int potentialUpgradeOptionsPopupOrderId = 220;

        private readonly HashSet<int> itemTooltipSuppressors = new HashSet<int>();
        private readonly Dictionary<int, ModalViewKind> activeModalKindsByOrderId = new Dictionary<int, ModalViewKind>();
        private int? activeItemTooltipOwnerKey;

        public bool IsItemOptionsPopupVisible =>
            inventoryItemOptionsPopupView != null && inventoryItemOptionsPopupView.IsVisible;

        public bool IsQuantityPopupVisible =>
            inventoryUseQuantityPopupView != null && inventoryUseQuantityPopupView.IsVisible;

        public bool IsPotentialUpgradeOptionsPopupVisible =>
            potentialUpgradeOptionsPopupView != null && potentialUpgradeOptionsPopupView.IsVisible;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning(
                    $"Duplicate {nameof(WorldModalUIManager)} detected on '{gameObject.name}'. " +
                    $"Keeping '{Instance.gameObject.name}' and disabling this component.");
                enabled = false;
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void ShowItemTooltip(object owner, ItemTooltipViewData data, bool force = false)
        {
            if (inventoryItemTooltipView == null || owner == null)
                return;

            activeItemTooltipOwnerKey = ResolveOwnerKey(owner);
            if (IsItemTooltipBlocked())
                return;

            BeginShow(ModalViewKind.ItemTooltip, inventoryItemTooltipOrderId);
            inventoryItemTooltipView.Show(data, force);
        }

        public void HideItemTooltip(object owner = null, bool force = false)
        {
            if (inventoryItemTooltipView == null)
                return;

            if (owner != null)
            {
                var ownerKey = ResolveOwnerKey(owner);
                if (!force && activeItemTooltipOwnerKey.HasValue && activeItemTooltipOwnerKey.Value != ownerKey)
                    return;

                if (activeItemTooltipOwnerKey.HasValue && activeItemTooltipOwnerKey.Value == ownerKey)
                    activeItemTooltipOwnerKey = null;
            }
            else
            {
                activeItemTooltipOwnerKey = null;
            }

            inventoryItemTooltipView.Hide(force);
            EndHide(ModalViewKind.ItemTooltip, inventoryItemTooltipOrderId);
        }

        public void BeginItemInteraction(object owner, bool force = false)
        {
            if (owner == null)
                return;

            var ownerKey = ResolveOwnerKey(owner);
            itemTooltipSuppressors.Add(ownerKey);
            HideItemTooltip(owner, force: true);
            if (force)
                HideItemTooltip(force: true);
        }

        public void EndItemInteraction(object owner, bool force = false)
        {
            if (owner == null)
                return;

            var ownerKey = ResolveOwnerKey(owner);
            itemTooltipSuppressors.Remove(ownerKey);
            if (force)
                HideItemTooltip(owner, force: true);
        }

        public void ShowRecipeTooltip(
            PillRecipeDetailModel detail,
            System.Func<PillRecipeInputModel, int> quantityResolver,
            bool force = false)
        {
            if (craftRecipeTooltipView == null)
                return;

            BeginShow(ModalViewKind.CraftRecipeTooltip, craftRecipeTooltipOrderId);
            craftRecipeTooltipView.Show(detail, quantityResolver, force);
        }

        public void HideRecipeTooltip(bool force = false)
        {
            if (craftRecipeTooltipView != null)
                craftRecipeTooltipView.Hide(force);

            EndHide(ModalViewKind.CraftRecipeTooltip, craftRecipeTooltipOrderId);
        }

        public void ShowItemOptionsPopup(
            IReadOnlyList<ItemOptionEntry> options,
            bool force = false)
        {
            if (inventoryItemOptionsPopupView == null)
                return;

            BeginShow(ModalViewKind.ItemOptionsPopup, inventoryItemOptionsPopupOrderId);
            inventoryItemOptionsPopupView.Show(options, force);
        }

        public void HideItemOptionsPopup(bool force = false)
        {
            if (inventoryItemOptionsPopupView != null)
                inventoryItemOptionsPopupView.Hide(force);

            EndHide(ModalViewKind.ItemOptionsPopup, inventoryItemOptionsPopupOrderId);
        }

        public void ShowQuantityPopup(
            int maxQuantityValue,
            System.Action<int> onConfirm,
            System.Action onCancel = null,
            string titleOverride = null,
            string headerOverride = null,
            int initialQuantity = 1)
        {
            if (inventoryUseQuantityPopupView == null)
                return;

            BeginShow(ModalViewKind.QuantityPopup, quantityPopupOrderId);
            inventoryUseQuantityPopupView.Show(
                maxQuantityValue,
                onConfirm,
                onCancel,
                titleOverride,
                headerOverride,
                initialQuantity);
        }

        public void HideQuantityPopup(bool force = false)
        {
            if (inventoryUseQuantityPopupView != null)
                inventoryUseQuantityPopupView.Hide(force);

            EndHide(ModalViewKind.QuantityPopup, quantityPopupOrderId);
        }

        public void ShowPotentialUpgradeOptionsPopup(
            RectTransform anchor,
            string title,
            IReadOnlyList<PotentialUpgradeOptionsPopupView.OptionEntry> options,
            bool force = false)
        {
            if (potentialUpgradeOptionsPopupView == null)
                return;

            BeginShow(ModalViewKind.PotentialUpgradeOptionsPopup, potentialUpgradeOptionsPopupOrderId);
            potentialUpgradeOptionsPopupView.Show(anchor, title, options, force);
        }

        public void HidePotentialUpgradeOptionsPopup(bool force = false)
        {
            if (potentialUpgradeOptionsPopupView != null)
                potentialUpgradeOptionsPopupView.Hide(force);

            EndHide(ModalViewKind.PotentialUpgradeOptionsPopup, potentialUpgradeOptionsPopupOrderId);
        }

        public void HideAllViews(bool force = false)
        {
            itemTooltipSuppressors.Clear();
            activeItemTooltipOwnerKey = null;
            HideItemTooltip(force: force);
            HideRecipeTooltip(force);
            HideItemOptionsPopup(force);
            HideQuantityPopup(force);
            HidePotentialUpgradeOptionsPopup(force);
        }

        private void BeginShow(ModalViewKind requestedKind, int orderId)
        {
            if (!activeModalKindsByOrderId.TryGetValue(orderId, out var activeKind) || activeKind == requestedKind)
            {
                activeModalKindsByOrderId[orderId] = requestedKind;
                return;
            }

            HideModal(activeKind, force: true);
            activeModalKindsByOrderId[orderId] = requestedKind;
        }

        private void EndHide(ModalViewKind hiddenKind, int orderId)
        {
            if (activeModalKindsByOrderId.TryGetValue(orderId, out var activeKind) && activeKind == hiddenKind)
                activeModalKindsByOrderId.Remove(orderId);
        }

        private void HideModal(ModalViewKind kind, bool force)
        {
            switch (kind)
            {
                case ModalViewKind.ItemTooltip:
                    HideItemTooltip(force: force);
                    break;
                case ModalViewKind.CraftRecipeTooltip:
                    HideRecipeTooltip(force);
                    break;
                case ModalViewKind.ItemOptionsPopup:
                    HideItemOptionsPopup(force);
                    break;
                case ModalViewKind.QuantityPopup:
                    HideQuantityPopup(force);
                    break;
                case ModalViewKind.PotentialUpgradeOptionsPopup:
                    HidePotentialUpgradeOptionsPopup(force);
                    break;
            }
        }

        private static int ResolveOwnerKey(object owner)
        {
            if (owner is Object unityObject)
                return unityObject.GetInstanceID();

            return RuntimeHelpers.GetHashCode(owner);
        }

        private bool IsItemTooltipBlocked()
        {
            return itemTooltipSuppressors.Count > 0 ||
                   IsItemOptionsPopupVisible ||
                   IsQuantityPopupVisible ||
                   IsPotentialUpgradeOptionsPopupVisible;
        }
    }
}
