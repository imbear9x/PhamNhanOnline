using System;
using System.Collections.Generic;
using GameShared.Models;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.Inventory
{
    public sealed class InventoryPanelView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InventoryItemGridView inventoryGridView;

        public event Action<InventoryItemModel> ItemClicked;
        public event Action<InventoryItemModel> ItemHovered;
        public event Action ItemHoverExited;

        private void Awake()
        {
            if (inventoryGridView != null)
            {
                inventoryGridView.ItemClicked += HandleGridItemClicked;
                inventoryGridView.ItemHovered += HandleGridItemHovered;
                inventoryGridView.ItemHoverExited += HandleGridItemHoverExited;
            }
        }

        private void OnDisable()
        {
            HideTooltip(force: true);
        }

        private void OnDestroy()
        {
            if (inventoryGridView != null)
            {
                inventoryGridView.ItemClicked -= HandleGridItemClicked;
                inventoryGridView.ItemHovered -= HandleGridItemHovered;
                inventoryGridView.ItemHoverExited -= HandleGridItemHoverExited;
            }
        }

        public void SetItems(
            IReadOnlyList<InventoryItemModel> value,
            InventoryItemPresentationCatalog valuePresentationCatalog,
            long? valueSelectedPlayerItemId,
            bool force = false)
        {
            if (inventoryGridView != null)
            {
                inventoryGridView.SetItems(value ?? Array.Empty<InventoryItemModel>(), valuePresentationCatalog, force);
                inventoryGridView.SetSelectedItem(valueSelectedPlayerItemId, force: true);
            }
        }

        public void SetSelectedItem(long? playerItemId, bool force = false)
        {
            if (inventoryGridView != null)
                inventoryGridView.SetSelectedItem(playerItemId, force);
        }

        public void SetTooltipSuppressed(bool suppressed, bool force = false)
        {
            if (inventoryGridView != null)
                inventoryGridView.SetTooltipSuppressed(suppressed, force);
        }

        public void HideTooltip(bool force = false)
        {
            if (inventoryGridView != null)
                inventoryGridView.HideTooltip(force);
        }

        public void Clear(bool force = false)
        {
            if (inventoryGridView != null)
                inventoryGridView.Clear(force);
        }

        private void HandleGridItemClicked(InventoryItemModel item)
        {
            ItemClicked?.Invoke(item);
        }

        private void HandleGridItemHovered(InventoryItemModel item)
        {
            ItemHovered?.Invoke(item);
        }

        private void HandleGridItemHoverExited()
        {
            ItemHoverExited?.Invoke();
        }
    }
}
