using System;
using System.Threading.Tasks;
using GameShared.Messages;
using GameShared.Models;
using GameShared.Packets;
using PhamNhanOnline.Client.Features.Character.Application;
using PhamNhanOnline.Client.Network.Session;

namespace PhamNhanOnline.Client.Features.Inventory.Application
{
    public sealed class ClientInventoryService
    {
        private readonly ClientConnectionService connection;
        private readonly ClientCharacterState characterState;
        private readonly ClientInventoryState inventoryState;

        private TaskCompletionSource<InventoryLoadResult> inventoryCompletionSource;
        private TaskCompletionSource<InventoryActionResult> equipCompletionSource;
        private TaskCompletionSource<InventoryActionResult> unequipCompletionSource;
        private TaskCompletionSource<InventoryActionResult> useMartialArtBookCompletionSource;

        public ClientInventoryService(
            ClientConnectionService connection,
            ClientCharacterState characterState,
            ClientInventoryState inventoryState)
        {
            this.connection = connection;
            this.characterState = characterState;
            this.inventoryState = inventoryState;

            connection.Packets.Subscribe<GetInventoryResultPacket>(HandleGetInventoryResult);
            connection.Packets.Subscribe<EquipInventoryItemResultPacket>(HandleEquipInventoryItemResult);
            connection.Packets.Subscribe<UnequipInventoryItemResultPacket>(HandleUnequipInventoryItemResult);
            connection.Packets.Subscribe<UseMartialArtBookResultPacket>(HandleUseMartialArtBookResult);
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

        public Task<InventoryActionResult> EquipItemAsync(long playerItemId, int targetSlot)
        {
            if (connection.State != ClientConnectionState.Connected)
            {
                return Task.FromResult(new InventoryActionResult(
                    false,
                    null,
                    inventoryState.Items,
                    characterState.BaseStats,
                    characterState.CurrentState,
                    "Not connected to server."));
            }

            if (equipCompletionSource != null && !equipCompletionSource.Task.IsCompleted)
                return equipCompletionSource.Task;

            equipCompletionSource = new TaskCompletionSource<InventoryActionResult>();
            connection.Send(new EquipInventoryItemPacket
            {
                PlayerItemId = playerItemId,
                Slot = targetSlot
            });
            return equipCompletionSource.Task;
        }

        public Task<InventoryActionResult> UnequipItemAsync(int slot)
        {
            if (connection.State != ClientConnectionState.Connected)
            {
                return Task.FromResult(new InventoryActionResult(
                    false,
                    null,
                    inventoryState.Items,
                    characterState.BaseStats,
                    characterState.CurrentState,
                    "Not connected to server."));
            }

            if (unequipCompletionSource != null && !unequipCompletionSource.Task.IsCompleted)
                return unequipCompletionSource.Task;

            unequipCompletionSource = new TaskCompletionSource<InventoryActionResult>();
            connection.Send(new UnequipInventoryItemPacket
            {
                Slot = slot
            });
            return unequipCompletionSource.Task;
        }

        public Task<InventoryActionResult> UseMartialArtBookAsync(long playerItemId)
        {
            if (connection.State != ClientConnectionState.Connected)
            {
                return Task.FromResult(new InventoryActionResult(
                    false,
                    null,
                    inventoryState.Items,
                    characterState.BaseStats,
                    characterState.CurrentState,
                    "Not connected to server."));
            }

            if (useMartialArtBookCompletionSource != null && !useMartialArtBookCompletionSource.Task.IsCompleted)
                return useMartialArtBookCompletionSource.Task;

            useMartialArtBookCompletionSource = new TaskCompletionSource<InventoryActionResult>();
            connection.Send(new UseMartialArtBookPacket
            {
                PlayerItemId = playerItemId
            });
            return useMartialArtBookCompletionSource.Task;
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

        private void HandleEquipInventoryItemResult(EquipInventoryItemResultPacket packet)
        {
            CompletePending(ref equipCompletionSource, ApplyInventoryActionResult(
                packet.Success == true,
                packet.Code,
                packet.Items,
                packet.BaseStats,
                packet.CurrentState,
                "Item equipped.",
                "Failed to equip item"));
        }

        private void HandleUnequipInventoryItemResult(UnequipInventoryItemResultPacket packet)
        {
            CompletePending(ref unequipCompletionSource, ApplyInventoryActionResult(
                packet.Success == true,
                packet.Code,
                packet.Items,
                packet.BaseStats,
                packet.CurrentState,
                "Item unequipped.",
                "Failed to unequip item"));
        }

        private async void HandleUseMartialArtBookResult(UseMartialArtBookResultPacket packet)
        {
            if (packet.BaseStats.HasValue)
                characterState.ApplyBaseStats(packet.BaseStats);

            InventoryItemModel[] resolvedItems = inventoryState.Items;
            if (packet.Success == true)
            {
                try
                {
                    var reloadResult = await LoadInventoryAsync(forceRefresh: true);
                    resolvedItems = reloadResult.Success ? reloadResult.Items : inventoryState.Items;
                }
                catch
                {
                    resolvedItems = inventoryState.Items;
                }
            }

            CompletePending(ref useMartialArtBookCompletionSource, new InventoryActionResult(
                packet.Success == true,
                packet.Code,
                resolvedItems,
                packet.BaseStats,
                characterState.CurrentState,
                packet.Success == true
                    ? "Martial art book used."
                    : string.Format("Failed to use martial art book: {0}", packet.Code ?? MessageCode.UnknownError)));
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
            CompletePending(ref equipCompletionSource, new InventoryActionResult(
                false,
                null,
                Array.Empty<InventoryItemModel>(),
                null,
                null,
                "Connection closed."));
            CompletePending(ref unequipCompletionSource, new InventoryActionResult(
                false,
                null,
                Array.Empty<InventoryItemModel>(),
                null,
                null,
                "Connection closed."));
            CompletePending(ref useMartialArtBookCompletionSource, new InventoryActionResult(
                false,
                null,
                Array.Empty<InventoryItemModel>(),
                null,
                null,
                "Connection closed."));
        }

        private void CompletePending(InventoryLoadResult result)
        {
            var pending = inventoryCompletionSource;
            inventoryCompletionSource = null;
            if (pending != null)
                pending.TrySetResult(result);
        }

        private InventoryActionResult ApplyInventoryActionResult(
            bool success,
            MessageCode? code,
            System.Collections.Generic.List<InventoryItemModel> items,
            CharacterBaseStatsModel? baseStats,
            CharacterCurrentStateModel? currentState,
            string successMessage,
            string failurePrefix)
        {
            var resolvedItems = items != null ? items.ToArray() : inventoryState.Items;
            if (success)
            {
                inventoryState.ApplyInventory(
                    resolvedItems,
                    code ?? MessageCode.None,
                    string.Format("Loaded {0} inventory item(s).", resolvedItems.Length));
            }
            else
            {
                inventoryState.ApplyFailure(
                    code,
                    string.Format("{0}: {1}", failurePrefix, code ?? MessageCode.UnknownError));
            }

            if (baseStats.HasValue)
                characterState.ApplyBaseStats(baseStats);
            if (currentState.HasValue)
                characterState.ApplyCurrentState(currentState);

            return new InventoryActionResult(
                success,
                code,
                resolvedItems,
                baseStats,
                currentState,
                success
                    ? successMessage
                    : string.Format("{0}: {1}", failurePrefix, code ?? MessageCode.UnknownError));
        }

        private static void CompletePending(ref TaskCompletionSource<InventoryActionResult> completionSource, InventoryActionResult result)
        {
            var pending = completionSource;
            completionSource = null;
            if (pending != null)
                pending.TrySetResult(result);
        }
    }
}
