using System;
using GameShared.Packets;
using PhamNhanOnline.Client.Features.Character.Application;
using PhamNhanOnline.Client.Features.Targeting.Application;
using PhamNhanOnline.Client.Network.Session;
using GameShared.Enums;
using GameShared.Models;
using System.Globalization;
using PhamNhanOnline.Client.Core.Logging;

namespace PhamNhanOnline.Client.Features.Combat.Application
{
    public sealed class ClientCombatService
    {
        private const int BasicSkillSlotIndex = 1;
        private readonly ClientConnectionService connection;
        private readonly ClientCombatState combatState;
        private readonly ClientCharacterState characterState;

        public ClientCombatService(
            ClientConnectionService connection,
            ClientCombatState combatState,
            ClientCharacterState characterState)
        {
            this.connection = connection;
            this.combatState = combatState;
            this.characterState = characterState;

            connection.Packets.Subscribe<AttackEnemyResultPacket>(HandleAttackEnemyResult);
            connection.Packets.Subscribe<SkillCastStartedPacket>(HandleSkillCastStarted);
            connection.Packets.Subscribe<SkillImpactResolvedPacket>(HandleSkillImpactResolved);
            connection.StateChanged += HandleConnectionStateChanged;
        }

        public bool TryUseSkill(int skillSlotIndex)
        {
            if (connection.State != ClientConnectionState.Connected)
                return false;

            if (skillSlotIndex <= 0)
                return false;

            if (IsLocalCharacterDead())
                return false;

            if (combatState.HasPendingAttackRequest)
                return false;

            if (combatState.IsLocalCastActive(DateTime.UtcNow))
                return false;

            combatState.MarkPendingAttackRequest(skillSlotIndex);
            connection.Send(new AttackEnemyPacket
            {
                SkillSlotIndex = skillSlotIndex
            });
            return true;
        }

        public bool TryUseSkillOnTarget(int skillSlotIndex, WorldTargetHandle target)
        {
            if (connection.State != ClientConnectionState.Connected)
                return false;

            if (skillSlotIndex <= 0)
                return false;

            if (IsLocalCharacterDead())
                return false;

            if (combatState.HasPendingAttackRequest)
                return false;

            if (combatState.IsLocalCastActive(DateTime.UtcNow))
                return false;

            CombatTargetModel packetTarget;
            if (!TryBuildCombatTarget(target, out packetTarget))
                return false;

            combatState.MarkPendingAttackRequest(skillSlotIndex);
            connection.Send(new AttackEnemyPacket
            {
                Target = packetTarget,
                SkillSlotIndex = skillSlotIndex
            });
            return true;
        }

        public bool TryUseSkillOnEnemy(int skillSlotIndex, int enemyRuntimeId)
        {
            return TryUseSkillOnTarget(
                skillSlotIndex,
                WorldTargetHandle.CreateEnemy(enemyRuntimeId));
        }

        public bool TryUseBasicSkillOnTarget(WorldTargetHandle target)
        {
            return TryUseSkillOnTarget(BasicSkillSlotIndex, target);
        }

        private void HandleAttackEnemyResult(AttackEnemyResultPacket packet)
        {
            var playerSkillId = packet.PlayerSkillId ?? 0;
            var cooldownMs = Math.Max(0, packet.CooldownMs ?? 0);
            var cooldownEndsAtUtc = FromUnixMs(packet.CooldownEndsUnixMs);

            if (packet.Success == true)
            {
                ClientLog.Info(
                    $"AttackEnemy accepted: slot={packet.SkillSlotIndex ?? 0}, " +
                    $"skillId={packet.SkillId ?? 0}, playerSkillId={playerSkillId}, " +
                    $"cooldownMs={cooldownMs}, castStart={packet.CastStartedUnixMs}, impact={packet.ImpactUnixMs}.");
                combatState.ApplyAttackAccepted(
                    packet.SkillExecutionId ?? 0,
                    packet.SkillSlotIndex ?? 0,
                    playerSkillId,
                    cooldownMs,
                    cooldownEndsAtUtc,
                    FromUnixMs(packet.CastStartedUnixMs),
                    FromUnixMs(packet.CastCompletedUnixMs),
                    FromUnixMs(packet.ImpactUnixMs));
                return;
            }

            ClientLog.Warn(
                $"AttackEnemy rejected: code={packet.Code}, slot={packet.SkillSlotIndex ?? 0}, " +
                $"skillId={packet.SkillId ?? 0}, playerSkillId={playerSkillId}.");
            combatState.ApplyAttackRejected(packet.SkillSlotIndex ?? 0, playerSkillId, cooldownMs, cooldownEndsAtUtc);
        }

        private void HandleSkillCastStarted(SkillCastStartedPacket packet)
        {
            ClientLog.Info(
                $"SkillCastStarted: casterChar={packet.CasterCharacterId}, casterRuntime={packet.Caster?.RuntimeId ?? 0}, slot={packet.SkillSlotIndex ?? 0}, " +
                $"skillId={packet.SkillId ?? 0}, playerSkillId={packet.PlayerSkillId ?? 0}, " +
                $"castMs={packet.CastTimeMs ?? 0}, travelMs={packet.TravelTimeMs ?? 0}.");
            combatState.PublishSkillCastStarted(new SkillCastStartedNotice(
                packet.MapId,
                packet.InstanceId,
                TryBuildWorldTargetHandle(packet.Caster, out var castCaster) ? castCaster : (WorldTargetHandle?)null,
                packet.CasterCharacterId,
                TryBuildWorldTargetHandle(packet.Target, out var castTarget) ? castTarget : (WorldTargetHandle?)null,
                packet.SkillExecutionId ?? 0,
                packet.SkillSlotIndex ?? 0,
                packet.PlayerSkillId ?? 0,
                packet.SkillId ?? 0,
                packet.SkillCode ?? string.Empty,
                packet.SkillGroupCode ?? string.Empty,
                packet.CastTimeMs ?? 0,
                packet.TravelTimeMs ?? 0,
                FromUnixMs(packet.CastStartedUnixMs),
                FromUnixMs(packet.CastCompletedUnixMs),
                FromUnixMs(packet.ImpactUnixMs)));

            var selectedCharacterId = characterState.SelectedCharacterId;
            if (!selectedCharacterId.HasValue || !packet.CasterCharacterId.HasValue)
                return;

            if (packet.CasterCharacterId.Value != selectedCharacterId.Value)
                return;

            combatState.ApplyLocalCastStarted(
                packet.SkillExecutionId ?? 0,
                packet.SkillSlotIndex ?? 0,
                packet.PlayerSkillId ?? 0,
                FromUnixMs(packet.CastStartedUnixMs),
                FromUnixMs(packet.CastCompletedUnixMs),
                FromUnixMs(packet.ImpactUnixMs));
        }

        private void HandleSkillImpactResolved(SkillImpactResolvedPacket packet)
        {
            ClientLog.Info(
                $"SkillImpactResolved: casterChar={packet.CasterCharacterId}, casterRuntime={packet.Caster?.RuntimeId ?? 0}, slot={packet.SkillSlotIndex ?? 0}, " +
                $"skillId={packet.SkillId ?? 0}, playerSkillId={packet.PlayerSkillId ?? 0}, " +
                $"success={packet.Success == true}, code={packet.Code}, damage={packet.DamageApplied ?? 0}, " +
                $"remainingHp={packet.RemainingHp ?? 0}, killed={packet.IsKilled == true}.");
            combatState.PublishSkillImpactResolved(new SkillImpactResolvedNotice(
                packet.MapId,
                packet.InstanceId,
                TryBuildWorldTargetHandle(packet.Caster, out var impactCaster) ? impactCaster : (WorldTargetHandle?)null,
                packet.CasterCharacterId,
                TryBuildWorldTargetHandle(packet.Target, out var impactTarget) ? impactTarget : (WorldTargetHandle?)null,
                packet.SkillExecutionId ?? 0,
                packet.SkillSlotIndex ?? 0,
                packet.PlayerSkillId ?? 0,
                packet.SkillId ?? 0,
                packet.SkillCode ?? string.Empty,
                packet.SkillGroupCode ?? string.Empty,
                packet.Success == true,
                packet.Code,
                packet.DamageApplied ?? 0,
                packet.RemainingHp ?? 0,
                packet.IsKilled == true,
                FromUnixMs(packet.ResolvedAtUnixMs)));

            var selectedCharacterId = characterState.SelectedCharacterId;
            if (!selectedCharacterId.HasValue || !packet.CasterCharacterId.HasValue)
                return;

            if (packet.CasterCharacterId.Value != selectedCharacterId.Value)
                return;

            combatState.ClearPendingAttackRequest();
        }

        private void HandleConnectionStateChanged(ClientConnectionState state)
        {
            if (state == ClientConnectionState.Disconnected)
                combatState.Clear();
        }

        private bool IsLocalCharacterDead()
        {
            var currentState = characterState.CurrentState;
            return currentState.HasValue &&
                   (currentState.Value.IsDead ||
                    ClientCharacterRuntimeStateCodes.IsCombatDead(currentState.Value.CurrentState) ||
                    ClientCharacterRuntimeStateCodes.IsPermanentlyDead(currentState.Value.CurrentState));
        }

        private static DateTime? FromUnixMs(long? unixMs)
        {
            if (!unixMs.HasValue || unixMs.Value <= 0)
                return null;

            return DateTimeOffset.FromUnixTimeMilliseconds(unixMs.Value).UtcDateTime;
        }

        private static bool TryBuildCombatTarget(WorldTargetHandle target, out CombatTargetModel packetTarget)
        {
            packetTarget = null!;
            if (!target.IsValid)
                return false;

            switch (target.Kind)
            {
                case WorldTargetKind.Player:
                    Guid characterId;
                    if (!Guid.TryParse(target.TargetId, out characterId))
                        return false;

                    packetTarget = new CombatTargetModel
                    {
                        Kind = CombatTargetKind.Character,
                        CharacterId = characterId
                    };
                    return true;

                case WorldTargetKind.Enemy:
                case WorldTargetKind.Boss:
                    int runtimeId;
                    if (!int.TryParse(target.TargetId, out runtimeId) || runtimeId <= 0)
                        return false;

                    packetTarget = new CombatTargetModel
                    {
                        Kind = target.Kind == WorldTargetKind.Boss ? CombatTargetKind.Boss : CombatTargetKind.Enemy,
                        RuntimeId = runtimeId
                    };
                    return true;

                default:
                    return false;
            }
        }

        private static bool TryBuildWorldTargetHandle(CombatTargetModel target, out WorldTargetHandle handle)
        {
            handle = default;
            if (target == null)
                return false;

            switch (target.Kind)
            {
                case CombatTargetKind.Character:
                    if (!target.CharacterId.HasValue)
                        return false;

                    handle = WorldTargetHandle.CreateObservedCharacter(target.CharacterId.Value);
                    return true;

                case CombatTargetKind.Enemy:
                    if (!target.RuntimeId.HasValue)
                        return false;

                    handle = WorldTargetHandle.CreateEnemy(target.RuntimeId.Value);
                    return true;

                case CombatTargetKind.Boss:
                    if (!target.RuntimeId.HasValue)
                        return false;

                    handle = WorldTargetHandle.CreateEnemy(target.RuntimeId.Value, isBoss: true);
                    return true;

                default:
                    return false;
            }
        }
    }
}
