using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PhamNhanOnline.Client.UI.Inventory
{
    public sealed class InventoryDropZoneView : MonoBehaviour, IDropHandler
    {
        public event Action<InventoryEquipmentSlot> EquippedItemDropped;

        public void OnDrop(PointerEventData eventData)
        {
            var equipmentSlotView = eventData.pointerDrag != null
                ? eventData.pointerDrag.GetComponent<EquipmentSlotView>()
                : null;

            if (equipmentSlotView == null || !equipmentSlotView.HasItem)
                return;

            var handler = EquippedItemDropped;
            if (handler != null)
                handler(equipmentSlotView.SlotType);
        }
    }
}
