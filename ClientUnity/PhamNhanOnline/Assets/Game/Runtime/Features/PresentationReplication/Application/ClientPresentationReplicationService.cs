using System;
using System.Collections.Generic;
using GameShared.Models;
using PhamNhanOnline.Client.Features.Combat.Application;
using PhamNhanOnline.Client.Features.Targeting.Application;
using PhamNhanOnline.Client.Features.World.Application;

namespace PhamNhanOnline.Client.Features.PresentationReplication.Application
{
    public sealed class ClientPresentationReplicationService
    {
        private readonly ClientPresentationReplicationState state;
        private readonly ClientWorldState worldState;
        private readonly Dictionary<PresentationExecutionKey, DateTime> pendingCastReleases =
            new Dictionary<PresentationExecutionKey, DateTime>();

        public ClientPresentationReplicationService(
            ClientPresentationReplicationState state,
            ClientCombatState combatState,
            ClientWorldState worldState)
        {
            this.state = state;
            this.worldState = worldState;

            combatState.SkillCastStarted += HandleSkillCastStarted;
            combatState.SkillImpactResolved += HandleSkillImpactResolved;
            worldState.MapChanged += HandleMapChanged;
            worldState.GroundRewardUpserted += HandleGroundRewardUpserted;
            worldState.GroundRewardRemoved += HandleGroundRewardRemoved;
        }

        public void Tick(DateTime utcNow)
        {
            if (pendingCastReleases.Count == 0)
                return;

            var keysToRelease = new List<PresentationExecutionKey>();
            foreach (var pair in pendingCastReleases)
            {
                if (utcNow >= pair.Value)
                    keysToRelease.Add(pair.Key);
            }

            for (var i = 0; i < keysToRelease.Count; i++)
            {
                var key = keysToRelease[i];
                pendingCastReleases.Remove(key);
                state.Publish(new ClientPresentationReplicationEvent(
                    ClientPresentationReplicationEventKind.SkillCastReleased,
                    key.MapId > 0 ? key.MapId : (int?)null,
                    key.InstanceId > 0 ? key.InstanceId : (int?)null,
                    key.SourceTarget,
                    key.SourceCharacterId,
                    key.Target,
                    key.SkillExecutionId > 0 ? key.SkillExecutionId : (int?)null,
                    key.SkillId > 0 ? key.SkillId : (int?)null,
                    key.SkillCode,
                    key.SkillGroupCode,
                    key.SkillSlotIndex > 0 ? key.SkillSlotIndex : (int?)null,
                    null,
                    null,
                    null,
                    utcNow));
            }
        }

        public void Clear()
        {
            pendingCastReleases.Clear();
            state.Clear();
        }

        private void HandleMapChanged()
        {
            pendingCastReleases.Clear();
            state.Publish(new ClientPresentationReplicationEvent(
                ClientPresentationReplicationEventKind.MapChanged,
                worldState.CurrentMapId,
                null,
                null,
                null,
                null,
                null,
                null,
                string.Empty,
                string.Empty,
                null,
                null,
                null,
                null,
                DateTime.UtcNow));
        }

        private void HandleGroundRewardUpserted(GroundRewardModel reward)
        {
            state.Publish(new ClientPresentationReplicationEvent(
                ClientPresentationReplicationEventKind.GroundRewardUpserted,
                worldState.CurrentMapId,
                null,
                null,
                null,
                WorldTargetHandle.CreateGroundReward(reward.RewardId),
                null,
                null,
                string.Empty,
                string.Empty,
                null,
                null,
                reward.RewardId,
                null,
                DateTime.UtcNow));
        }

        private void HandleGroundRewardRemoved(int rewardId)
        {
            state.Publish(new ClientPresentationReplicationEvent(
                ClientPresentationReplicationEventKind.GroundRewardRemoved,
                worldState.CurrentMapId,
                null,
                null,
                null,
                WorldTargetHandle.CreateGroundReward(rewardId),
                null,
                null,
                string.Empty,
                string.Empty,
                null,
                null,
                rewardId,
                null,
                DateTime.UtcNow));
        }

        private void HandleSkillCastStarted(SkillCastStartedNotice notice)
        {
            state.Publish(new ClientPresentationReplicationEvent(
                ClientPresentationReplicationEventKind.SkillCastStarted,
                notice.MapId,
                notice.InstanceId,
                notice.Caster,
                notice.CasterCharacterId,
                notice.Target,
                notice.SkillExecutionId,
                notice.SkillId,
                notice.SkillCode,
                notice.SkillGroupCode,
                notice.SkillSlotIndex,
                null,
                null,
                null,
                notice.CastStartedAtUtc ?? DateTime.UtcNow));

            if (!notice.CastCompletedAtUtc.HasValue)
                return;

            var key = new PresentationExecutionKey(
                notice.MapId ?? 0,
                notice.InstanceId ?? 0,
                notice.Caster,
                notice.CasterCharacterId,
                notice.Target,
                notice.SkillExecutionId,
                notice.SkillId,
                notice.SkillCode,
                notice.SkillGroupCode,
                notice.SkillSlotIndex);
            pendingCastReleases[key] = notice.CastCompletedAtUtc.Value;
        }

        private void HandleSkillImpactResolved(SkillImpactResolvedNotice notice)
        {
            var key = new PresentationExecutionKey(
                notice.MapId ?? 0,
                notice.InstanceId ?? 0,
                notice.Caster,
                notice.CasterCharacterId,
                notice.Target,
                notice.SkillExecutionId,
                notice.SkillId,
                notice.SkillCode,
                notice.SkillGroupCode,
                notice.SkillSlotIndex);
            pendingCastReleases.Remove(key);

            state.Publish(new ClientPresentationReplicationEvent(
                ClientPresentationReplicationEventKind.SkillImpactResolved,
                notice.MapId,
                notice.InstanceId,
                notice.Caster,
                notice.CasterCharacterId,
                notice.Target,
                notice.SkillExecutionId,
                notice.SkillId,
                notice.SkillCode,
                notice.SkillGroupCode,
                notice.SkillSlotIndex,
                null,
                null,
                notice.Success,
                notice.ResolvedAtUtc ?? DateTime.UtcNow));
        }

        private readonly struct PresentationExecutionKey : IEquatable<PresentationExecutionKey>
        {
            public PresentationExecutionKey(
                int mapId,
                int instanceId,
                WorldTargetHandle? sourceTarget,
                Guid? sourceCharacterId,
                WorldTargetHandle? target,
                int skillExecutionId,
                int skillId,
                string skillCode,
                string skillGroupCode,
                int skillSlotIndex)
            {
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
            }

            public int MapId { get; }
            public int InstanceId { get; }
            public WorldTargetHandle? SourceTarget { get; }
            public Guid? SourceCharacterId { get; }
            public WorldTargetHandle? Target { get; }
            public int SkillExecutionId { get; }
            public int SkillId { get; }
            public string SkillCode { get; }
            public string SkillGroupCode { get; }
            public int SkillSlotIndex { get; }

            public bool Equals(PresentationExecutionKey other)
            {
                return MapId == other.MapId &&
                       InstanceId == other.InstanceId &&
                       Nullable.Equals(SourceTarget, other.SourceTarget) &&
                       SourceCharacterId.Equals(other.SourceCharacterId) &&
                       Nullable.Equals(Target, other.Target) &&
                       SkillExecutionId == other.SkillExecutionId &&
                       SkillId == other.SkillId &&
                       string.Equals(SkillCode, other.SkillCode, StringComparison.Ordinal) &&
                       string.Equals(SkillGroupCode, other.SkillGroupCode, StringComparison.Ordinal) &&
                       SkillSlotIndex == other.SkillSlotIndex;
            }

            public override bool Equals(object obj)
            {
                return obj is PresentationExecutionKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = MapId;
                    hashCode = (hashCode * 397) ^ InstanceId;
                    hashCode = (hashCode * 397) ^ SourceTarget.GetHashCode();
                    hashCode = (hashCode * 397) ^ SourceCharacterId.GetHashCode();
                    hashCode = (hashCode * 397) ^ Target.GetHashCode();
                    hashCode = (hashCode * 397) ^ SkillExecutionId;
                    hashCode = (hashCode * 397) ^ SkillId;
                    hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(SkillCode ?? string.Empty);
                    hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(SkillGroupCode ?? string.Empty);
                    hashCode = (hashCode * 397) ^ SkillSlotIndex;
                    return hashCode;
                }
            }
        }
    }
}
