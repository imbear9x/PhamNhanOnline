using System.Numerics;
using GameShared.Messages;

namespace GameServer.World;

public sealed class PendingSkillExecution
{
    public PendingSkillExecution(
        int executionId,
        Guid casterPlayerId,
        Guid casterCharacterId,
        long playerSkillId,
        int skillId,
        int skillSlotIndex,
        int enemyRuntimeId,
        int damage,
        int castTimeMs,
        int travelTimeMs,
        DateTime castStartedAtUtc,
        DateTime castCompletedAtUtc,
        DateTime impactAtUtc)
    {
        ExecutionId = executionId;
        CasterPlayerId = casterPlayerId;
        CasterCharacterId = casterCharacterId;
        PlayerSkillId = playerSkillId;
        SkillId = skillId;
        SkillSlotIndex = skillSlotIndex;
        EnemyRuntimeId = enemyRuntimeId;
        Damage = damage;
        CastTimeMs = castTimeMs;
        TravelTimeMs = travelTimeMs;
        CastStartedAtUtc = castStartedAtUtc;
        CastCompletedAtUtc = castCompletedAtUtc;
        ImpactAtUtc = impactAtUtc;
    }

    public int ExecutionId { get; }
    public Guid CasterPlayerId { get; }
    public Guid CasterCharacterId { get; }
    public long PlayerSkillId { get; }
    public int SkillId { get; }
    public int SkillSlotIndex { get; }
    public int EnemyRuntimeId { get; }
    public int Damage { get; }
    public int CastTimeMs { get; }
    public int TravelTimeMs { get; }
    public DateTime CastStartedAtUtc { get; }
    public DateTime CastCompletedAtUtc { get; }
    public DateTime ImpactAtUtc { get; }
    public bool CastReleased { get; private set; }

    public void MarkCastReleased()
    {
        CastReleased = true;
    }
}

public readonly record struct EnemyTargetSnapshot(
    int EnemyRuntimeId,
    Vector2 Position,
    bool IsAlive);

public readonly record struct SkillCastReleaseRuntimeEvent(
    Guid CasterPlayerId,
    int ExecutionId);

public readonly record struct SkillImpactResolvedRuntimeEvent(
    Guid CasterPlayerId,
    Guid CasterCharacterId,
    int EnemyRuntimeId,
    int SkillSlotIndex,
    long PlayerSkillId,
    int SkillId,
    bool Applied,
    MessageCode Code,
    int DamageApplied,
    int RemainingHp,
    bool IsKilled,
    DateTime ResolvedAtUtc);
