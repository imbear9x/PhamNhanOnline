using System.Numerics;
using GameServer.DTO;
using GameServer.Exceptions;
using GameServer.Network.Interface;
using GameServer.Runtime;
using GameServer.Services;
using GameServer.World;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class AttackEnemyHandler : IPacketHandler<AttackEnemyPacket>
{
    private readonly CharacterCultivationService _cultivationService;
    private readonly CharacterRuntimeService _characterRuntimeService;
    private readonly SkillService _skillService;
    private readonly INetworkSender _network;
    private readonly WorldManager _worldManager;
    private readonly WorldInterestService _worldInterestService;

    public AttackEnemyHandler(
        CharacterCultivationService cultivationService,
        CharacterRuntimeService characterRuntimeService,
        SkillService skillService,
        INetworkSender network,
        WorldManager worldManager,
        WorldInterestService worldInterestService)
    {
        _cultivationService = cultivationService;
        _characterRuntimeService = characterRuntimeService;
        _skillService = skillService;
        _network = network;
        _worldManager = worldManager;
        _worldInterestService = worldInterestService;
    }

    public async Task HandleAsync(ConnectionSession session, AttackEnemyPacket packet)
    {
        if (session.Player is null || !packet.EnemyRuntimeId.HasValue || !packet.SkillSlotIndex.HasValue)
        {
            _network.Send(session.ConnectionId, new AttackEnemyResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterMustEnterWorld,
                EnemyRuntimeId = packet.EnemyRuntimeId,
                SkillSlotIndex = packet.SkillSlotIndex
            });
            return;
        }

        var player = session.Player;
        if (_cultivationService.IsCultivating(player))
        {
            _network.Send(session.ConnectionId, new AttackEnemyResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterCannotMoveWhileCultivating,
                EnemyRuntimeId = packet.EnemyRuntimeId,
                SkillSlotIndex = packet.SkillSlotIndex
            });
            return;
        }

        var runtimeSnapshot = player.RuntimeState.CaptureSnapshot();
        if (runtimeSnapshot.CurrentState.CurrentState == CharacterRuntimeStateCodes.Casting || player.IsCastingSkill)
        {
            _network.Send(session.ConnectionId, new AttackEnemyResultPacket
            {
                Success = false,
                Code = MessageCode.SkillAlreadyCasting,
                EnemyRuntimeId = packet.EnemyRuntimeId,
                SkillSlotIndex = packet.SkillSlotIndex
            });
            return;
        }

        if (!_worldManager.MapManager.TryGetInstance(player.MapId, player.InstanceId, out var instance))
        {
            _network.Send(session.ConnectionId, new AttackEnemyResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterNotInWorldInstance,
                EnemyRuntimeId = packet.EnemyRuntimeId,
                SkillSlotIndex = packet.SkillSlotIndex
            });
            return;
        }

        try
        {
            var castContext = await _skillService.ResolveEquippedSkillForCombatAsync(
                player.CharacterData.CharacterId,
                packet.SkillSlotIndex.Value);

            var utcNow = DateTime.UtcNow;
            if (player.IsSkillOnCooldown(castContext.PlayerSkillId, utcNow, out var cooldownUntilUtc))
            {
                _network.Send(session.ConnectionId, new AttackEnemyResultPacket
                {
                    Success = false,
                    Code = MessageCode.SkillOnCooldown,
                    EnemyRuntimeId = packet.EnemyRuntimeId,
                    SkillSlotIndex = packet.SkillSlotIndex,
                    PlayerSkillId = castContext.PlayerSkillId,
                    SkillId = castContext.SkillId,
                    CooldownMs = castContext.Skill.CooldownMs,
                    CooldownEndsUnixMs = ToUnixMs(cooldownUntilUtc)
                });
                return;
            }

            if (!instance.TryGetEnemySnapshot(packet.EnemyRuntimeId.Value, out var enemySnapshot) || !enemySnapshot.IsAlive)
            {
                _network.Send(session.ConnectionId, new AttackEnemyResultPacket
                {
                    Success = false,
                    Code = MessageCode.EnemyNotFound,
                    EnemyRuntimeId = packet.EnemyRuntimeId,
                    SkillSlotIndex = packet.SkillSlotIndex,
                    PlayerSkillId = castContext.PlayerSkillId,
                    SkillId = castContext.SkillId
                });
                return;
            }

            var castRange = Math.Max(0f, castContext.Skill.CastRange);
            if (castRange > 0f &&
                Vector2.DistanceSquared(player.Position, enemySnapshot.Position) > castRange * castRange)
            {
                _network.Send(session.ConnectionId, new AttackEnemyResultPacket
                {
                    Success = false,
                    Code = MessageCode.SkillTargetOutOfRange,
                    EnemyRuntimeId = packet.EnemyRuntimeId,
                    SkillSlotIndex = packet.SkillSlotIndex,
                    PlayerSkillId = castContext.PlayerSkillId,
                    SkillId = castContext.SkillId
                });
                return;
            }

            var damage = Math.Max(1, runtimeSnapshot.BaseStats.GetEffectiveAttack());
            var cooldownEndsAtUtc = utcNow.AddMilliseconds(Math.Max(0, castContext.Skill.CooldownMs));
            var execution = instance.EnqueueSkillExecution(
                player.PlayerId,
                player.CharacterData.CharacterId,
                castContext.PlayerSkillId,
                castContext.SkillId,
                castContext.SkillSlotIndex,
                packet.EnemyRuntimeId.Value,
                damage,
                castContext.Skill.CastTimeMs,
                castContext.Skill.TravelTimeMs,
                utcNow);

            if (!player.TryBeginSkillCast(
                    execution.ExecutionId,
                    castContext.PlayerSkillId,
                    execution.CastCompletedAtUtc,
                    cooldownEndsAtUtc))
            {
                _network.Send(session.ConnectionId, new AttackEnemyResultPacket
                {
                    Success = false,
                    Code = MessageCode.SkillAlreadyCasting,
                    EnemyRuntimeId = packet.EnemyRuntimeId,
                    SkillSlotIndex = packet.SkillSlotIndex,
                    PlayerSkillId = castContext.PlayerSkillId,
                    SkillId = castContext.SkillId
                });
                return;
            }

            if (castContext.Skill.CastTimeMs > 0)
            {
                _characterRuntimeService.ApplyCurrentStateMutation(
                    player,
                    state => state with { CurrentState = CharacterRuntimeStateCodes.Casting },
                    persist: false);
            }

            _worldInterestService.NotifySkillCastStarted(instance, execution);
            _network.Send(session.ConnectionId, new AttackEnemyResultPacket
            {
                Success = true,
                Code = MessageCode.None,
                EnemyRuntimeId = packet.EnemyRuntimeId,
                SkillSlotIndex = packet.SkillSlotIndex,
                PlayerSkillId = castContext.PlayerSkillId,
                SkillId = castContext.SkillId,
                CooldownMs = castContext.Skill.CooldownMs,
                CooldownEndsUnixMs = ToUnixMs(cooldownEndsAtUtc),
                CastStartedUnixMs = ToUnixMs(execution.CastStartedAtUtc),
                CastCompletedUnixMs = ToUnixMs(execution.CastCompletedAtUtc),
                ImpactUnixMs = ToUnixMs(execution.ImpactAtUtc),
                DamageApplied = 0,
                RemainingHp = enemySnapshot.IsAlive ? null : 0,
                IsKilled = false
            });
        }
        catch (GameException ex)
        {
            _network.Send(session.ConnectionId, new AttackEnemyResultPacket
            {
                Success = false,
                Code = ex.Code,
                EnemyRuntimeId = packet.EnemyRuntimeId,
                SkillSlotIndex = packet.SkillSlotIndex
            });
        }
    }

    private static long ToUnixMs(DateTime utc)
    {
        var normalized = utc.Kind == DateTimeKind.Utc
            ? utc
            : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return new DateTimeOffset(normalized).ToUnixTimeMilliseconds();
    }
}
