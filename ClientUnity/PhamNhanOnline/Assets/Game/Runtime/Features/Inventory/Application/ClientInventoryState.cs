using System;
using GameShared.Messages;
using GameShared.Models;

namespace PhamNhanOnline.Client.Features.Inventory.Application
{
    public sealed class ClientInventoryState
    {
        public event Action Changed;

        public bool HasLoadedInventory { get; private set; }
        public bool IsLoading { get; private set; }
        public MessageCode? LastResultCode { get; private set; }
        public string LastStatusMessage { get; private set; } = string.Empty;
        public DateTime? LastLoadedAtUtc { get; private set; }
        public InventoryItemModel[] Items { get; private set; } = Array.Empty<InventoryItemModel>();

        public void BeginLoading()
        {
            if (IsLoading)
                return;

            IsLoading = true;
            NotifyChanged();
        }

        public void ApplyInventory(InventoryItemModel[] items, MessageCode? code, string statusMessage)
        {
            HasLoadedInventory = true;
            IsLoading = false;
            LastResultCode = code;
            LastStatusMessage = statusMessage ?? string.Empty;
            LastLoadedAtUtc = DateTime.UtcNow;
            Items = items ?? Array.Empty<InventoryItemModel>();
            NotifyChanged();
        }

        public void ApplyFailure(MessageCode? code, string statusMessage)
        {
            IsLoading = false;
            LastResultCode = code;
            LastStatusMessage = statusMessage ?? string.Empty;
            NotifyChanged();
        }

        public bool TryGetItem(long playerItemId, out InventoryItemModel item)
        {
            for (var i = 0; i < Items.Length; i++)
            {
                if (Items[i].PlayerItemId != playerItemId)
                    continue;

                item = Items[i];
                return true;
            }

            item = default;
            return false;
        }

        public void Clear()
        {
            HasLoadedInventory = false;
            IsLoading = false;
            LastResultCode = null;
            LastStatusMessage = string.Empty;
            LastLoadedAtUtc = null;
            Items = Array.Empty<InventoryItemModel>();
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            var handler = Changed;
            if (handler != null)
                handler();
        }
    }
}
