using System;
using PhamNhanOnline.Client.Features.Targeting.Application;

namespace PhamNhanOnline.Client.Features.PresentationReplication.Application
{
    public readonly struct ClientPresentationReplicationEvent
    {
        public ClientPresentationReplicationEvent(
            ClientPresentationReplicationEventKind kind,
            int? mapId,
            int? instanceId,
            WorldTargetHandle? sourceTarget,
            Guid? sourceCharacterId,
            WorldTargetHandle? target,
            int? skillExecutionId,
            int? skillId,
            string skillCode,
            string skillGroupCode,
            int? skillSlotIndex,
            int? runtimeId,
            int? rewardId,
            bool? success,
            DateTime occurredAtUtc)
        {
            Kind = kind;
            MapId = mapId;
            InstanceId = instanceId;
            SourceTarget = sourceTarget;
            SourceCharacterId = sourceCharacterId;
            Target = target;
            SkillExecutionId = skillExecutionId;
            SkillId = skillId;
            SkillCode = skillCode ?? string.Empty;
            SkillGroupCode = skillGroupCode ?? string.Empty;
            SkillSlotIndex = skillSlotIndex;
            RuntimeId = runtimeId;
            RewardId = rewardId;
            Success = success;
            OccurredAtUtc = occurredAtUtc;
        }

        public ClientPresentationReplicationEventKind Kind { get; }
        public int? MapId { get; }
        public int? InstanceId { get; }
        public WorldTargetHandle? SourceTarget { get; }
        public Guid? SourceCharacterId { get; }
        public WorldTargetHandle? Target { get; }
        public int? SkillExecutionId { get; }
        public int? SkillId { get; }
        public string SkillCode { get; }
        public string SkillGroupCode { get; }
        public int? SkillSlotIndex { get; }
        public int? RuntimeId { get; }
        public int? RewardId { get; }
        public bool? Success { get; }
        public DateTime OccurredAtUtc { get; }
    }
}
