using System;
using System.Collections.Generic;
using GameShared.Models;
using PhamNhanOnline.Client.UI.World;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.Inventory
{
    public sealed class InventoryPanelView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InventoryItemGridView inventoryGridView;

        public event Action<InventoryItemModel> ItemClicked;

        private void Awake()
        {
            if (inventoryGridView != null)
                inventoryGridView.ItemClicked += HandleGridItemClicked;
        }

        private void OnDisable()
        {
            HideTooltip(force: true);
        }

        private void OnDestroy()
        {
            if (inventoryGridView != null)
                inventoryGridView.ItemClicked -= HandleGridItemClicked;
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
            WorldModalUIManager.Instance?.SetItemTooltipSuppressed(this, suppressed, force);
        }

        public void HideTooltip(bool force = false)
        {
            WorldModalUIManager.Instance?.HideItemTooltip(force: force);
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
    }
}
