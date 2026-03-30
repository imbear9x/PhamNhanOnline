using System;
using PhamNhanOnline.Client.Features.Targeting.Application;

namespace PhamNhanOnline.Client.Features.Combat.Presentation
{
    public enum SkillPresentationArchetype
    {
        None = 0,
        MeleeWeaponSwing = 1,
        WeaponProjectile = 2,
        HandProjectile = 3,
        SummonStrike = 4,
        SelfBuff = 5
    }

    public enum CharacterPresentationSocketType
    {
        None = 0,
        Root = 1,
        VisualRoot = 2,
        Weapon = 3,
        HandLeft = 4,
        HandRight = 5,
        Chest = 6,
        Head = 7,
        Ground = 8,
        TargetCenter = 9
    }

    public enum SkillPresentationPhase
    {
        None = 0,
        CastStarted = 1,
        Released = 2,
        ImpactResolved = 3,
        Completed = 4,
        Cancelled = 5
    }

    public readonly struct SkillExecutionKey : IEquatable<SkillExecutionKey>
    {
        public SkillExecutionKey(int mapId, int instanceId, int skillExecutionId)
        {
            MapId = mapId;
            InstanceId = instanceId;
            SkillExecutionId = skillExecutionId;
        }

        public int MapId { get; }
        public int InstanceId { get; }
        public int SkillExecutionId { get; }

        public bool IsValid => MapId > 0 && InstanceId > 0 && SkillExecutionId > 0;

        public bool Equals(SkillExecutionKey other)
        {
            return MapId == other.MapId &&
                   InstanceId == other.InstanceId &&
                   SkillExecutionId == other.SkillExecutionId;
        }

        public override bool Equals(object obj)
        {
            return obj is SkillExecutionKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = MapId;
                hashCode = (hashCode * 397) ^ InstanceId;
                hashCode = (hashCode * 397) ^ SkillExecutionId;
                return hashCode;
            }
        }

        public override string ToString()
        {
            return string.Format("{0}:{1}:{2}", MapId, InstanceId, SkillExecutionId);
        }
    }

    public readonly struct SkillPresentationLookupContext
    {
        public SkillPresentationLookupContext(
            int skillId,
            long playerSkillId,
            int skillSlotIndex,
            string skillCode,
            string skillGroupCode)
        {
            SkillId = skillId;
            PlayerSkillId = playerSkillId;
            SkillSlotIndex = skillSlotIndex;
            SkillCode = skillCode ?? string.Empty;
            SkillGroupCode = skillGroupCode ?? string.Empty;
        }

        public int SkillId { get; }
        public long PlayerSkillId { get; }
        public int SkillSlotIndex { get; }
        public string SkillCode { get; }
        public string SkillGroupCode { get; }
    }

    public readonly struct SkillPresentationExecutionSnapshot
    {
        public SkillPresentationExecutionSnapshot(
            SkillExecutionKey key,
            Guid? casterCharacterId,
            WorldTargetHandle? target,
            int skillSlotIndex,
            long playerSkillId,
            int skillId,
            SkillPresentationArchetype archetype,
            SkillPresentationPhase phase,
            DateTime? castStartedAtUtc,
            DateTime? castCompletedAtUtc,
            DateTime? impactAtUtc,
            DateTime? resolvedAtUtc)
        {
            Key = key;
            CasterCharacterId = casterCharacterId;
            Target = target;
            SkillSlotIndex = skillSlotIndex;
            PlayerSkillId = playerSkillId;
            SkillId = skillId;
            Archetype = archetype;
            Phase = phase;
            CastStartedAtUtc = castStartedAtUtc;
            CastCompletedAtUtc = castCompletedAtUtc;
            ImpactAtUtc = impactAtUtc;
            ResolvedAtUtc = resolvedAtUtc;
        }

        public SkillExecutionKey Key { get; }
        public Guid? CasterCharacterId { get; }
        public WorldTargetHandle? Target { get; }
        public int SkillSlotIndex { get; }
        public long PlayerSkillId { get; }
        public int SkillId { get; }
        public SkillPresentationArchetype Archetype { get; }
        public SkillPresentationPhase Phase { get; }
        public DateTime? CastStartedAtUtc { get; }
        public DateTime? CastCompletedAtUtc { get; }
        public DateTime? ImpactAtUtc { get; }
        public DateTime? ResolvedAtUtc { get; }
    }
}
