using System;
using GameShared.Messages;
using PhamNhanOnline.Client.Features.Targeting.Application;

namespace PhamNhanOnline.Client.Features.Combat.Application
{
    public readonly struct SkillCastStartedNotice
    {
        public SkillCastStartedNotice(
            int? mapId,
            int? instanceId,
            WorldTargetHandle? caster,
            Guid? casterCharacterId,
            WorldTargetHandle? target,
            int skillExecutionId,
            int skillSlotIndex,
            long playerSkillId,
            int skillId,
            string skillCode,
            string skillGroupCode,
            int castTimeMs,
            int travelTimeMs,
            DateTime? castStartedAtUtc,
            DateTime? castCompletedAtUtc,
            DateTime? impactAtUtc)
        {
            MapId = mapId;
            InstanceId = instanceId;
            Caster = caster;
            CasterCharacterId = casterCharacterId;
            Target = target;
            SkillExecutionId = skillExecutionId;
            SkillSlotIndex = skillSlotIndex;
            PlayerSkillId = playerSkillId;
            SkillId = skillId;
            SkillCode = skillCode ?? string.Empty;
            SkillGroupCode = skillGroupCode ?? string.Empty;
            CastTimeMs = castTimeMs;
            TravelTimeMs = travelTimeMs;
            CastStartedAtUtc = castStartedAtUtc;
            CastCompletedAtUtc = castCompletedAtUtc;
            ImpactAtUtc = impactAtUtc;
        }

        public int? MapId { get; }
        public int? InstanceId { get; }
        public WorldTargetHandle? Caster { get; }
        public Guid? CasterCharacterId { get; }
        public WorldTargetHandle? Target { get; }
        public int SkillExecutionId { get; }
        public int SkillSlotIndex { get; }
        public long PlayerSkillId { get; }
        public int SkillId { get; }
        public string SkillCode { get; }
        public string SkillGroupCode { get; }
        public int CastTimeMs { get; }
        public int TravelTimeMs { get; }
        public DateTime? CastStartedAtUtc { get; }
        public DateTime? CastCompletedAtUtc { get; }
        public DateTime? ImpactAtUtc { get; }
    }

    public readonly struct SkillImpactResolvedNotice
    {
        public SkillImpactResolvedNotice(
            int? mapId,
            int? instanceId,
            WorldTargetHandle? caster,
            Guid? casterCharacterId,
            WorldTargetHandle? target,
            int skillExecutionId,
            int skillSlotIndex,
            long playerSkillId,
            int skillId,
            string skillCode,
            string skillGroupCode,
            bool success,
            MessageCode? code,
            int damageApplied,
            int remainingHp,
            bool isKilled,
            DateTime? resolvedAtUtc)
        {
            MapId = mapId;
            InstanceId = instanceId;
            Caster = caster;
            CasterCharacterId = casterCharacterId;
            Target = target;
            SkillExecutionId = skillExecutionId;
            SkillSlotIndex = skillSlotIndex;
            PlayerSkillId = playerSkillId;
            SkillId = skillId;
            SkillCode = skillCode ?? string.Empty;
            SkillGroupCode = skillGroupCode ?? string.Empty;
            Success = success;
            Code = code;
            DamageApplied = damageApplied;
            RemainingHp = remainingHp;
            IsKilled = isKilled;
            ResolvedAtUtc = resolvedAtUtc;
        }

        public int? MapId { get; }
        public int? InstanceId { get; }
        public WorldTargetHandle? Caster { get; }
        public Guid? CasterCharacterId { get; }
        public WorldTargetHandle? Target { get; }
        public int SkillExecutionId { get; }
        public int SkillSlotIndex { get; }
        public long PlayerSkillId { get; }
        public int SkillId { get; }
        public string SkillCode { get; }
        public string SkillGroupCode { get; }
        public bool Success { get; }
        public MessageCode? Code { get; }
        public int DamageApplied { get; }
        public int RemainingHp { get; }
        public bool IsKilled { get; }
        public DateTime? ResolvedAtUtc { get; }
    }
}
