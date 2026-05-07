using System.Numerics;
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

    public bool TryBeginGroundRewardClaim(
        Guid pickerCharacterId,
        int rewardId,
        Vector2 pickerPosition,
        float maxPickupDistance,
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

            if (resolvedReward.IsClaiming)
            {
                failureCode = MessageCode.GroundRewardClaimInProgress;
                return false;
            }

            if (resolvedReward.OwnerCharacterId.HasValue &&
                resolvedReward.OwnerCharacterId.Value != pickerCharacterId)
            {
                failureCode = MessageCode.GroundRewardNotOwnedYet;
                return false;
            }

            var resolvedPickupDistance = MathF.Max(0f, maxPickupDistance);
            if (Vector2.DistanceSquared(pickerPosition, resolvedReward.Position) >
                resolvedPickupDistance * resolvedPickupDistance)
            {
                failureCode = MessageCode.GroundRewardOutOfRange;
                return false;
            }

            if (!resolvedReward.TryBeginClaim(pickerCharacterId))
            {
                failureCode = MessageCode.GroundRewardClaimInProgress;
                return false;
            }

            reward = resolvedReward;
            return true;
        }
    }

    public bool CompleteGroundRewardClaim(Guid pickerCharacterId, int rewardId)
    {
        lock (_sync)
        {
            var resolvedReward = GroundRewards.FirstOrDefault(x => x.Id == rewardId);
            if (resolvedReward is null || !resolvedReward.IsClaimingBy(pickerCharacterId))
                return false;

            GroundRewards.Remove(resolvedReward);
            resolvedReward.CompleteClaim(pickerCharacterId);
            _pendingGroundRewardDespawns.Enqueue(new GroundRewardDespawnRuntimeEvent(
                resolvedReward.Id,
                resolvedReward.GetPlayerItemIds(),
                DestroyItems: false));
            return true;
        }
    }

    public void CancelGroundRewardClaim(Guid pickerCharacterId, int rewardId)
    {
        lock (_sync)
        {
            var resolvedReward = GroundRewards.FirstOrDefault(x => x.Id == rewardId);
            resolvedReward?.CancelClaim(pickerCharacterId);
        }
    }

    public bool TryGetGroundRewardPickupPosition(
        Guid pickerCharacterId,
        int rewardId,
        DateTime utcNow,
        out Vector2 rewardPosition,
        out MessageCode failureCode)
    {
        lock (_sync)
        {
            rewardPosition = default;
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

            if (resolvedReward.IsClaiming)
            {
                failureCode = MessageCode.GroundRewardClaimInProgress;
                return false;
            }

            if (resolvedReward.OwnerCharacterId.HasValue &&
                resolvedReward.OwnerCharacterId.Value != pickerCharacterId)
            {
                failureCode = MessageCode.GroundRewardNotOwnedYet;
                return false;
            }

            rewardPosition = resolvedReward.Position;
            return true;
        }
    }
}
