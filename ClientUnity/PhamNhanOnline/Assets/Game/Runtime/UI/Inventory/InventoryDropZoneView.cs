using System;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.UI.Common;
using PhamNhanOnline.Client.UI.World;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PhamNhanOnline.Client.UI.Inventory
{
    public sealed class InventoryDropZoneView : MonoBehaviour, IDropHandler, IPointerClickHandler
    {
        public event Action<InventoryEquipmentSlot> EquippedItemDropped;

        public void OnDrop(PointerEventData eventData)
        {
            if (!UIDragPayloadResolver.TryResolve(eventData, out var payload) ||
                payload.Kind != UIDragPayloadKind.InventoryItem ||
                payload.SourceKind != UIDragSourceKind.EquipmentSlot ||
                !payload.HasSourceEquipmentSlot)
            {
                return;
            }

            WorldModalUIManager.Instance?.HideInventoryItemOptionsPopup(force: true);
            var handler = EquippedItemDropped;
            if (handler != null)
                handler(payload.SourceEquipmentSlot);

            if (!ClientRuntime.IsInitialized)
                return;

            _ = ClientRuntime.InventoryService.UnequipItemAsync((int)payload.SourceEquipmentSlot);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
                return;

            WorldModalUIManager.Instance?.HideInventoryItemOptionsPopup(force: true);
        }
    }
}
