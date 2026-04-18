using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.UI.Common;
using PhamNhanOnline.Client.UI.World;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PhamNhanOnline.Client.UI.Common
{
    public sealed class DropZoneView : MonoBehaviour, IDropHandler, IPointerClickHandler
    {
        public void OnDrop(PointerEventData eventData)
        {
            if (!UIDragPayloadResolver.TryResolve(eventData, out var payload))
            {
                return;
            }

            WorldModalUIManager.Instance?.HideAllViews(force: true);

            if (!ClientRuntime.IsInitialized)
                return;

            if (payload.Kind == UIDragPayloadKind.InventoryItem &&
                payload.SourceKind == UIDragSourceKind.EquipmentSlot &&
                payload.HasSourceEquipmentSlot)
            {
                _ = ClientRuntime.InventoryService.UnequipItemAsync((int)payload.SourceEquipmentSlot);
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
            if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
                return;

            WorldModalUIManager.Instance?.HideAllViews(force: true);
        }
    }
}
