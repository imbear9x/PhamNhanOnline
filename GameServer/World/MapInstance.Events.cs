using System.Numerics;
using GameServer.Runtime;

namespace GameServer.World;

public sealed partial class MapInstance
{
    public IReadOnlyCollection<EnemyDeathRuntimeEvent> DequeuePendingDeaths()
    {
        lock (_sync)
        {
            if (_pendingDeaths.Count == 0)
                return Array.Empty<EnemyDeathRuntimeEvent>();

            var result = new List<EnemyDeathRuntimeEvent>(_pendingDeaths.Count);
            while (_pendingDeaths.Count > 0)
                result.Add(_pendingDeaths.Dequeue());

            return result;
        }
    }

    public IReadOnlyCollection<EnemySpawnRuntimeEvent> DequeuePendingEnemySpawns()
    {
        lock (_sync)
        {
            return DrainQueueUnsafe(_pendingEnemySpawns);
        }
    }

    public IReadOnlyCollection<EnemyDespawnRuntimeEvent> DequeuePendingEnemyDespawns()
    {
        lock (_sync)
        {
            return DrainQueueUnsafe(_pendingEnemyDespawns);
        }
    }

    public IReadOnlyCollection<EnemyHpChangedRuntimeEvent> DequeuePendingEnemyHpChanges()
    {
        lock (_sync)
        {
            return DrainQueueUnsafe(_pendingEnemyHpChanges);
        }
    }

    public IReadOnlyCollection<GroundRewardSpawnRuntimeEvent> DequeuePendingGroundRewardSpawns()
    {
        lock (_sync)
        {
            return DrainQueueUnsafe(_pendingGroundRewardSpawns);
        }
    }

    public IReadOnlyCollection<GroundRewardDespawnRuntimeEvent> DequeuePendingGroundRewardDespawns()
    {
        lock (_sync)
        {
            return DrainQueueUnsafe(_pendingGroundRewardDespawns);
        }
    }

    public IReadOnlyCollection<PlayerDamageRuntimeEvent> DequeuePendingPlayerDamages()
    {
        lock (_sync)
        {
            return DrainQueueUnsafe(_pendingPlayerDamages);
        }
    }

    public IReadOnlyCollection<EnemySkillCastRequestRuntimeEvent> DequeuePendingEnemySkillCastRequests()
    {
        lock (_sync)
        {
            return DrainQueueUnsafe(_pendingEnemySkillCastRequests);
        }
    }

    public IReadOnlyCollection<SkillCastReleaseRuntimeEvent> DequeuePendingSkillCastReleases()
    {
        lock (_sync)
        {
            return DrainQueueUnsafe(_pendingSkillCastReleases);
        }
    }

    public IReadOnlyCollection<SkillImpactResolvedRuntimeEvent> DequeuePendingSkillImpactResolutions()
    {
        lock (_sync)
        {
            return DrainQueueUnsafe(_pendingSkillImpactResolutions);
        }
    }

    public IReadOnlyCollection<SkillImpactDueRuntimeEvent> DequeuePendingSkillImpactDues()
    {
        lock (_sync)
        {
            return DrainQueueUnsafe(_pendingSkillImpactDues);
        }
    }

    private static IReadOnlyCollection<T> DrainQueueUnsafe<T>(Queue<T> queue)
    {
        if (queue.Count == 0)
            return Array.Empty<T>();

        var result = new List<T>(queue.Count);
        while (queue.Count > 0)
            result.Add(queue.Dequeue());

        return result;
    }
}

public sealed record EnemyDeathRuntimeEvent(
    EnemyDefinition Definition,
    Vector2 Position,
    Guid? LastHitPlayerId,
    IReadOnlyList<RewardTargetSnapshot> Targets,
    DateTime KilledAtUtc);

public sealed record EnemySpawnRuntimeEvent(MonsterEntity Enemy);

public readonly record struct EnemyDespawnRuntimeEvent(int EnemyRuntimeId);

public readonly record struct EnemyHpChangedRuntimeEvent(
    int EnemyRuntimeId,
    int CurrentHp,
    int MaxHp,
    EnemyRuntimeState RuntimeState);

public sealed record GroundRewardSpawnRuntimeEvent(GroundRewardEntity Reward);

public readonly record struct GroundRewardDespawnRuntimeEvent(
    int RewardId,
    IReadOnlyList<long> PlayerItemIds,
    bool DestroyItems);

public readonly record struct PlayerDamageRuntimeEvent(
    Guid TargetPlayerId,
    int EnemyRuntimeId,
    int Damage);

public readonly record struct EnemySkillCastRequestRuntimeEvent(
    int EnemyRuntimeId,
    Guid TargetPlayerId,
    int SkillId,
    int SkillSlotIndex);
