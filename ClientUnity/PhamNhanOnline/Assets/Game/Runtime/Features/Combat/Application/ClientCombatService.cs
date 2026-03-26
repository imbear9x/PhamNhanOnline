using System;
using GameShared.Packets;
using PhamNhanOnline.Client.Features.Character.Application;
using PhamNhanOnline.Client.Network.Session;

namespace PhamNhanOnline.Client.Features.Combat.Application
{
    public sealed class ClientCombatService
    {
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

        public bool TryUseSkillOnEnemy(int skillSlotIndex, int enemyRuntimeId)
        {
            if (connection.State != ClientConnectionState.Connected)
                return false;

            if (skillSlotIndex <= 0 || enemyRuntimeId <= 0)
                return false;

            if (combatState.HasPendingAttackRequest)
                return false;

            if (combatState.IsLocalCastActive(DateTime.UtcNow))
                return false;

            combatState.MarkPendingAttackRequest(skillSlotIndex);
            connection.Send(new AttackEnemyPacket
            {
                EnemyRuntimeId = enemyRuntimeId,
                SkillSlotIndex = skillSlotIndex
            });
            return true;
        }

        private void HandleAttackEnemyResult(AttackEnemyResultPacket packet)
        {
            var playerSkillId = packet.PlayerSkillId ?? 0;
            var cooldownMs = Math.Max(0, packet.CooldownMs ?? 0);
            var cooldownEndsAtUtc = FromUnixMs(packet.CooldownEndsUnixMs);

            if (packet.Success == true)
            {
                combatState.ApplyAttackAccepted(
                    packet.SkillSlotIndex ?? 0,
                    playerSkillId,
                    cooldownMs,
                    cooldownEndsAtUtc,
                    FromUnixMs(packet.CastStartedUnixMs),
                    FromUnixMs(packet.CastCompletedUnixMs),
                    FromUnixMs(packet.ImpactUnixMs));
                return;
            }

            combatState.ApplyAttackRejected(playerSkillId, cooldownMs, cooldownEndsAtUtc);
        }

        private void HandleSkillCastStarted(SkillCastStartedPacket packet)
        {
            var selectedCharacterId = characterState.SelectedCharacterId;
            if (!selectedCharacterId.HasValue || !packet.CasterCharacterId.HasValue)
                return;

            if (packet.CasterCharacterId.Value != selectedCharacterId.Value)
                return;

            combatState.ApplyLocalCastStarted(
                packet.SkillSlotIndex ?? 0,
                packet.PlayerSkillId ?? 0,
                FromUnixMs(packet.CastStartedUnixMs),
                FromUnixMs(packet.CastCompletedUnixMs),
                FromUnixMs(packet.ImpactUnixMs));
        }

        private void HandleSkillImpactResolved(SkillImpactResolvedPacket packet)
        {
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

        private static DateTime? FromUnixMs(long? unixMs)
        {
            if (!unixMs.HasValue || unixMs.Value <= 0)
                return null;

            return DateTimeOffset.FromUnixTimeMilliseconds(unixMs.Value).UtcDateTime;
        }
    }
}
