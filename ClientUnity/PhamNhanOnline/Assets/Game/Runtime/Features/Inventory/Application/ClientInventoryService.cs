using System;
using System.Threading.Tasks;
using GameShared.Messages;
using GameShared.Models;
using GameShared.Packets;
using PhamNhanOnline.Client.Network.Session;

namespace PhamNhanOnline.Client.Features.Inventory.Application
{
    public sealed class ClientInventoryService
    {
        private readonly ClientConnectionService connection;
        private readonly ClientInventoryState inventoryState;

        private TaskCompletionSource<InventoryLoadResult> inventoryCompletionSource;

        public ClientInventoryService(ClientConnectionService connection, ClientInventoryState inventoryState)
        {
            this.connection = connection;
            this.inventoryState = inventoryState;

            connection.Packets.Subscribe<GetInventoryResultPacket>(HandleGetInventoryResult);
            connection.StateChanged += HandleConnectionStateChanged;
        }

        public Task<InventoryLoadResult> LoadInventoryAsync(bool forceRefresh = false)
        {
            if (connection.State != ClientConnectionState.Connected)
            {
                return Task.FromResult(new InventoryLoadResult(
                    false,
                    null,
                    inventoryState.Items,
                    "Not connected to server.",
                    false));
            }

            if (!forceRefresh && inventoryState.HasLoadedInventory && !inventoryState.IsLoading)
            {
                return Task.FromResult(new InventoryLoadResult(
                    true,
                    inventoryState.LastResultCode ?? MessageCode.None,
                    inventoryState.Items,
                    "Inventory loaded from cache.",
                    true));
            }

            if (inventoryCompletionSource != null && !inventoryCompletionSource.Task.IsCompleted)
                return inventoryCompletionSource.Task;

            inventoryCompletionSource = new TaskCompletionSource<InventoryLoadResult>();
            inventoryState.BeginLoading();
            connection.Send(new GetInventoryPacket());
            return inventoryCompletionSource.Task;
        }

        private void HandleGetInventoryResult(GetInventoryResultPacket packet)
        {
            var items = packet.Items != null ? packet.Items.ToArray() : Array.Empty<InventoryItemModel>();
            if (packet.Success == true)
            {
                inventoryState.ApplyInventory(
                    items,
                    packet.Code ?? MessageCode.None,
                    string.Format("Loaded {0} inventory item(s).", items.Length));
            }
            else
            {
                inventoryState.ApplyFailure(
                    packet.Code,
                    string.Format("Failed to load inventory: {0}", packet.Code ?? MessageCode.UnknownError));
            }

            CompletePending(new InventoryLoadResult(
                packet.Success == true,
                packet.Code,
                packet.Success == true ? items : inventoryState.Items,
                packet.Success == true
                    ? string.Format("Loaded {0} inventory item(s).", items.Length)
                    : string.Format("Failed to load inventory: {0}", packet.Code ?? MessageCode.UnknownError),
                false));
        }

        private void HandleConnectionStateChanged(ClientConnectionState state)
        {
            if (state != ClientConnectionState.Disconnected)
                return;

            inventoryState.Clear();
            CompletePending(new InventoryLoadResult(
                false,
                null,
                Array.Empty<InventoryItemModel>(),
                "Connection closed.",
                false));
        }

        private void CompletePending(InventoryLoadResult result)
        {
            var pending = inventoryCompletionSource;
            inventoryCompletionSource = null;
            if (pending != null)
                pending.TrySetResult(result);
        }
    }
}
