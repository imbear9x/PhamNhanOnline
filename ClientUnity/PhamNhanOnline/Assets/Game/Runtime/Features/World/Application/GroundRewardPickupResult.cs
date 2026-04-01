using GameShared.Messages;
using GameShared.Models;

namespace PhamNhanOnline.Client.Features.World.Application
{
    public readonly struct GroundRewardPickupResult
    {
        public GroundRewardPickupResult(
            bool success,
            MessageCode? code,
            int rewardId,
            GroundRewardItemModel[] grantedItems,
            string message)
        {
            Success = success;
            Code = code;
            RewardId = rewardId;
            GrantedItems = grantedItems ?? System.Array.Empty<GroundRewardItemModel>();
            Message = message ?? string.Empty;
        }

        public bool Success { get; }
        public MessageCode? Code { get; }
        public int RewardId { get; }
        public GroundRewardItemModel[] GrantedItems { get; }
        public string Message { get; }
    }
}
