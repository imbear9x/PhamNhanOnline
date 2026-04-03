using GameShared.Messages;

namespace GameServer.World;

public sealed partial class MapInstance
{
    public void AddGroundReward(GroundRewardEntity reward)
    {
        lock (_sync)
        {
            GroundRewards.Add(reward);
            _pendingGroundRewardSpawns.Enqueue(new GroundRewardSpawnRuntimeEvent(reward));
        }
    }

    public int AllocateGroundRewardId()
    {
        lock (_sync)
        {
            return _nextGroundRewardId++;
        }
    }

    public bool TryClaimGroundReward(
        Guid pickerCharacterId,
        int rewardId,
        DateTime utcNow,
        out GroundRewardEntity reward,
        out MessageCode failureCode)
    {
        lock (_sync)
        {
            reward = null!;
            failureCode = MessageCode.None;

            var resolvedReward = GroundRewards.FirstOrDefault(x => x.Id == rewardId);
            if (resolvedReward is null)
            {
                failureCode = MessageCode.GroundRewardNotFound;
                return false;
            }

            resolvedReward.Update(utcNow);
            if (resolvedReward.IsDestroyed)
            {
                GroundRewards.Remove(resolvedReward);
                _pendingGroundRewardDespawns.Enqueue(new GroundRewardDespawnRuntimeEvent(
                    resolvedReward.Id,
                    resolvedReward.GetPlayerItemIds(),
                    DestroyItems: true));
                failureCode = MessageCode.GroundRewardExpired;
                return false;
            }

            if (resolvedReward.OwnerCharacterId.HasValue &&
                resolvedReward.OwnerCharacterId.Value != pickerCharacterId)
            {
                failureCode = MessageCode.GroundRewardNotOwnedYet;
                return false;
            }

            GroundRewards.Remove(resolvedReward);
            _pendingGroundRewardDespawns.Enqueue(new GroundRewardDespawnRuntimeEvent(
                resolvedReward.Id,
                resolvedReward.GetPlayerItemIds(),
                DestroyItems: false));
            reward = resolvedReward;
            return true;
        }
    }
}
