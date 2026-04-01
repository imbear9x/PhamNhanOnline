using System;
using System.Threading.Tasks;
using GameShared.Messages;
using GameShared.Models;
using GameShared.Packets;
using PhamNhanOnline.Client.Features.Inventory.Application;
using PhamNhanOnline.Client.Features.Targeting.Application;
using PhamNhanOnline.Client.Network.Session;

namespace PhamNhanOnline.Client.Features.World.Application
{
    public sealed class ClientGroundRewardService
    {
        private readonly ClientConnectionService connection;
        private readonly ClientInventoryService inventoryService;
        private readonly ClientTargetState targetState;

        private TaskCompletionSource<GroundRewardPickupResult> pickupCompletionSource;

        public ClientGroundRewardService(
            ClientConnectionService connection,
            ClientInventoryService inventoryService,
            ClientTargetState targetState)
        {
            this.connection = connection;
            this.inventoryService = inventoryService;
            this.targetState = targetState;

            connection.Packets.Subscribe<PickupGroundRewardResultPacket>(HandlePickupGroundRewardResult);
            connection.StateChanged += HandleConnectionStateChanged;
        }

        public event Action<GroundRewardPickupResult> PickupSucceeded;

        public Task<GroundRewardPickupResult> PickupAsync(int rewardId)
        {
            if (connection.State != ClientConnectionState.Connected)
            {
                return Task.FromResult(new GroundRewardPickupResult(
                    false,
                    null,
                    rewardId,
                    Array.Empty<GroundRewardItemModel>(),
                    "Not connected to server."));
            }

            if (pickupCompletionSource != null && !pickupCompletionSource.Task.IsCompleted)
                return pickupCompletionSource.Task;

            pickupCompletionSource = new TaskCompletionSource<GroundRewardPickupResult>();
            connection.Send(new PickupGroundRewardPacket
            {
                RewardId = rewardId
            });
            return pickupCompletionSource.Task;
        }

        private async void HandlePickupGroundRewardResult(PickupGroundRewardResultPacket packet)
        {
            var rewardId = packet.RewardId ?? 0;
            var grantedItems = packet.GrantedItems != null
                ? packet.GrantedItems.ToArray()
                : Array.Empty<GroundRewardItemModel>();
            var result = new GroundRewardPickupResult(
                packet.Success == true,
                packet.Code,
                rewardId,
                grantedItems,
                packet.Success == true
                    ? "Ground reward picked up."
                    : string.Format("Failed to pick up ground reward: {0}", packet.Code ?? MessageCode.UnknownError));

            if (packet.Success == true)
            {
                if (targetState.IsSelectedGroundReward(rewardId))
                    targetState.Clear();

                NotifyPickupSucceeded(result);

                try
                {
                    await inventoryService.LoadInventoryAsync(forceRefresh: true);
                }
                catch
                {
                }
            }

            CompletePending(result);
        }

        private void HandleConnectionStateChanged(ClientConnectionState state)
        {
            if (state != ClientConnectionState.Disconnected)
                return;

            CompletePending(new GroundRewardPickupResult(
                false,
                null,
                0,
                Array.Empty<GroundRewardItemModel>(),
                "Connection closed."));
        }

        private void NotifyPickupSucceeded(GroundRewardPickupResult result)
        {
            var handler = PickupSucceeded;
            if (handler != null)
                handler(result);
        }

        private void CompletePending(GroundRewardPickupResult result)
        {
            var pending = pickupCompletionSource;
            pickupCompletionSource = null;
            if (pending != null)
                pending.TrySetResult(result);
        }
    }
}
