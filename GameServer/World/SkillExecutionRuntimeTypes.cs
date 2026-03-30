using System.Numerics;
using GameShared.Messages;
using GameServer.Runtime;
using GameShared.Enums;
using GameShared.Models;

namespace GameServer.World;

public readonly record struct CombatTargetReference(
    CombatTargetKind Kind,
    Guid? CharacterId,
    int? RuntimeId,
    Vector2? GroundPosition)
{
    public bool IsValid
    {
        get
        {
            return Kind switch
            {
                CombatTargetKind.None => false,
                CombatTargetKind.Character => CharacterId.HasValue,
                CombatTargetKind.Enemy or CombatTargetKind.Boss or CombatTargetKind.Dummy or CombatTargetKind.Npc => RuntimeId.HasValue && RuntimeId.Value > 0,
                CombatTargetKind.GroundPoint => GroundPosition.HasValue,
                _ => false
            };
        }
    }

    public CombatTargetModel ToModel()
    {
        return new CombatTargetModel
        {
            Kind = Kind,
            CharacterId = CharacterId,
            RuntimeId = RuntimeId,
            GroundPosX = GroundPosition?.X,
            GroundPosY = GroundPosition?.Y
        };
    }

    public static bool TryFromModel(CombatTargetModel? model, out CombatTargetReference target)
    {
        target = default;
        if (model is null || !model.Kind.HasValue)
            return false;

        Vector2? groundPosition = null;
        if (model.GroundPosX.HasValue && model.GroundPosY.HasValue)
            groundPosition = new Vector2(model.GroundPosX.Value, model.GroundPosY.Value);

        target = new CombatTargetReference(
            model.Kind.Value,
            model.CharacterId,
            model.RuntimeId,
            groundPosition);
        return target.IsValid;
    }
}

public sealed class PendingSkillExecution
{
    public PendingSkillExecution(
        int executionId,
        Guid casterPlayerId,
        Guid casterCharacterId,
        long playerSkillId,
        int skillId,
        int skillSlotIndex,
        SkillTargetType targetType,
        CombatStatSnapshot casterStats,
        CombatTargetReference? target,
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
        TargetType = targetType;
        CasterStats = casterStats;
        Target = target;
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
    public SkillTargetType TargetType { get; }
    public CombatStatSnapshot CasterStats { get; }
    public CombatTargetReference? Target { get; }
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

public readonly record struct CombatTargetSnapshot(
    CombatTargetKind Kind,
    Guid? CharacterId,
    int? RuntimeId,
    Vector2 Position,
    bool IsAlive);

public readonly record struct SkillCastReleaseRuntimeEvent(
    PendingSkillExecution Execution);

public readonly record struct SkillImpactDueRuntimeEvent(
    PendingSkillExecution Execution);

public readonly record struct SkillImpactResolvedRuntimeEvent(
    int ExecutionId,
    Guid CasterPlayerId,
    Guid CasterCharacterId,
    CombatTargetReference? Target,
    int SkillSlotIndex,
    long PlayerSkillId,
    int SkillId,
    bool Applied,
    MessageCode Code,
    int DamageApplied,
    int? RemainingHp,
    bool IsKilled,
    DateTime ResolvedAtUtc);
