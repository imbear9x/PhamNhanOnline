using System;
using PhamNhanOnline.Client.UI.Common;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PhamNhanOnline.Client.UI.Inventory
{
    public sealed class InventoryDropZoneView : MonoBehaviour, IDropHandler
    {
        public event Action<InventoryEquipmentSlot> EquippedItemDropped;

        public void OnDrop(PointerEventData eventData)
        {
            if (!UiDragPayloadResolver.TryResolve(eventData, out var payload) ||
                payload.Kind != UiDragPayloadKind.InventoryItem ||
                payload.SourceKind != UiDragSourceKind.EquipmentSlot ||
                !payload.HasSourceEquipmentSlot)
            {
                return;
            }

            var handler = EquippedItemDropped;
            if (handler != null)
                handler(payload.SourceEquipmentSlot);
        }
    }
}
