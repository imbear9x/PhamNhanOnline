using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.UI.Common;
using PhamNhanOnline.Client.UI.World;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PhamNhanOnline.Client.UI.Common
{
    public sealed class DropZoneView : MonoBehaviour, IDropHandler, IPointerClickHandler
    {
        public event System.Action<UIDragPayload> PayloadDropped;
        public event System.Action<PointerEventData.InputButton> Clicked;

        public void OnDrop(PointerEventData eventData)
        {
            if (!UIDragPayloadResolver.TryResolve(eventData, out var payload))
            {
                return;
            }

            WorldModalUIManager.Instance?.HideAllViews(force: true);
            PayloadDropped?.Invoke(payload);

            if (!ClientRuntime.IsInitialized)
                return;

            if (payload.Kind == UIDragPayloadKind.InventoryItem &&
                payload.SourceKind == UIDragSourceKind.EquipmentSlot &&
                payload.HasSourceEquipmentSlot)
            {
                _ = WorldCharacterEquipController.Instance.TryUnequipSlotAsync(payload.SourceEquipmentSlotIndex);
                return;
            }

            if (payload.Kind == UIDragPayloadKind.MartialArt &&
                payload.SourceKind == UIDragSourceKind.ActiveMartialArtSlot &&
                payload.HasMartialArt)
            {
                _ = ClientRuntime.MartialArtService.SetActiveMartialArtAsync(0);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null)
                return;

            Clicked?.Invoke(eventData.button);
            WorldModalUIManager.Instance?.HideAllViews(force: true);
        }
    }
}
