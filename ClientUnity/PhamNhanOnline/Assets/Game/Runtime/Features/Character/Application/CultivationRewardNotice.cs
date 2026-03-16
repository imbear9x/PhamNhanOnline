using System;

namespace PhamNhanOnline.Client.Features.Character.Application
{
    public struct CultivationRewardNotice
    {
        public CultivationRewardNotice(
            Guid? characterId,
            long cultivationGranted,
            int unallocatedPotentialGranted,
            bool reachedRealmCap,
            bool isOfflineSettlement,
            long? rewardedFromUnixMs,
            long? rewardedToUnixMs)
        {
            CharacterId = characterId;
            CultivationGranted = cultivationGranted;
            UnallocatedPotentialGranted = unallocatedPotentialGranted;
            ReachedRealmCap = reachedRealmCap;
            IsOfflineSettlement = isOfflineSettlement;
            RewardedFromUnixMs = rewardedFromUnixMs;
            RewardedToUnixMs = rewardedToUnixMs;
        }

        public Guid? CharacterId { get; }
        public long CultivationGranted { get; }
        public int UnallocatedPotentialGranted { get; }
        public bool ReachedRealmCap { get; }
        public bool IsOfflineSettlement { get; }
        public long? RewardedFromUnixMs { get; }
        public long? RewardedToUnixMs { get; }
    }
}
