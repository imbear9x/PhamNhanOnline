using GameServer.Config;
using GameServer.Exceptions;
using GameServer.Network.Interface;
using GameServer.Runtime;
using GameServer.Services;
using GameServer.World;
using GameShared.Enums;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class AttackEnemyHandler : IPacketHandler<AttackEnemyPacket>
{
    private readonly CharacterRuntimeService _characterRuntimeService;
    private readonly GameConfigValues _gameConfig;
    private readonly SkillExecutionService _skillExecutionService;
    private readonly SkillService _skillService;
    private readonly INetworkSender _network;
    private readonly WorldManager _worldManager;
    private readonly WorldInterestService _worldInterestService;
    private readonly WorldInteractionGate _interactionGate;

    public AttackEnemyHandler(
        CharacterRuntimeService characterRuntimeService,
        GameConfigValues gameConfig,
        SkillExecutionService skillExecutionService,
        SkillService skillService,
        INetworkSender network,
        WorldManager worldManager,
        WorldInterestService worldInterestService,
        WorldInteractionGate interactionGate)
    {
        _characterRuntimeService = characterRuntimeService;
        _gameConfig = gameConfig;
        _skillExecutionService = skillExecutionService;
        _skillService = skillService;
        _network = network;
        _worldManager = worldManager;
        _worldInterestService = worldInterestService;
        _interactionGate = interactionGate;
    }

    public async Task HandleAsync(ConnectionSession session, AttackEnemyPacket packet)
    {
        if (session.Player is null || !packet.SkillSlotIndex.HasValue)
        {
            _network.Send(session.ConnectionId, new AttackEnemyResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterMustEnterWorld,
                Target = packet.Target,
                SkillSlotIndex = packet.SkillSlotIndex
            });
            return;
        }

        var player = session.Player;
        var utcNow = DateTime.UtcNow;
        var startGateResult = _interactionGate.CheckPlayerCanStartAction(
            player,
            WorldInteractionActionKind.CombatSkill,
            "AttackEnemy",
            utcNow);
        if (!startGateResult.Success)
        {
            if (startGateResult.SuppressFailure)
                return;

            _network.Send(session.ConnectionId, new AttackEnemyResultPacket
            {
                Success = false,
                Code = startGateResult.Code,
                Target = packet.Target,
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
                Target = packet.Target,
                SkillSlotIndex = packet.SkillSlotIndex
            });
            return;
        }

        try
        {
            var castContext = await _skillService.ResolveEquippedSkillForCombatAsync(
                player.CharacterData.CharacterId,
                packet.SkillSlotIndex.Value);

            var hasTarget = CombatTargetReference.TryFromModel(packet.Target, out var requestedTarget);
            CombatTargetSnapshot targetSnapshot = default;
            var interactionTarget = WorldTargetRef.Player(player.CharacterData.CharacterId);
            var interactionRange = -1f;

            if (player.IsSkillOnCooldown(castContext.PlayerSkillId, utcNow, out var cooldownUntilUtc))
            {
                _network.Send(session.ConnectionId, new AttackEnemyResultPacket
                {
                    Success = false,
                    Code = MessageCode.SkillOnCooldown,
                    Target = packet.Target,
                    SkillSlotIndex = packet.SkillSlotIndex,
                    PlayerSkillId = castContext.PlayerSkillId,
                    SkillId = castContext.SkillId,
                    CooldownMs = castContext.Skill.CooldownMs,
                    CooldownEndsUnixMs = ToUnixMs(cooldownUntilUtc)
                });
                return;
            }

            if (castContext.Skill.TargetType is SkillTargetType.EnemyArea or SkillTargetType.AllyArea or SkillTargetType.GroundArea or SkillTargetType.AllEnemiesMap or SkillTargetType.AllAlliesMap or SkillTargetType.AllUnitsMap)
            {
                _network.Send(session.ConnectionId, new AttackEnemyResultPacket
                {
                    Success = false,
                    Code = MessageCode.SkillTargetTypeNotSupported,
                    Target = packet.Target,
                    SkillSlotIndex = packet.SkillSlotIndex,
                    PlayerSkillId = castContext.PlayerSkillId,
                    SkillId = castContext.SkillId
                });
                return;
            }

            switch (castContext.Skill.TargetType)
            {
                case SkillTargetType.Self:
                    interactionTarget = WorldTargetRef.Player(player.CharacterData.CharacterId);
                    interactionRange = -1f;
                    break;

                case SkillTargetType.SingleEnemy:
                case SkillTargetType.SingleAlly:
                    if (!hasTarget)
                    {
                        _network.Send(session.ConnectionId, new AttackEnemyResultPacket
                        {
                            Success = false,
                            Code = MessageCode.SkillTargetRequired,
                            Target = packet.Target,
                            SkillSlotIndex = packet.SkillSlotIndex,
                            PlayerSkillId = castContext.PlayerSkillId,
                            SkillId = castContext.SkillId
                        });
                        return;
                    }

                    if (!instance.TryGetCombatTargetSnapshot(requestedTarget, out targetSnapshot) || !targetSnapshot.IsAlive)
                    {
                        _network.Send(session.ConnectionId, new AttackEnemyResultPacket
                        {
                            Success = false,
                            Code = MessageCode.SkillTargetInvalid,
                            Target = packet.Target,
                            SkillSlotIndex = packet.SkillSlotIndex,
                            PlayerSkillId = castContext.PlayerSkillId,
                            SkillId = castContext.SkillId
                        });
                        return;
                    }

                    if (!IsTargetCompatible(player, castContext.Skill.TargetType, requestedTarget, targetSnapshot))
                    {
                        _network.Send(session.ConnectionId, new AttackEnemyResultPacket
                        {
                            Success = false,
                            Code = MessageCode.SkillTargetInvalid,
                            Target = packet.Target,
                            SkillSlotIndex = packet.SkillSlotIndex,
                            PlayerSkillId = castContext.PlayerSkillId,
                            SkillId = castContext.SkillId
                        });
                        return;
                    }

                    var castRange = Math.Max(0f, castContext.Skill.CastRange);
                    var rangeGrace = Math.Max(0f, _gameConfig.CombatSkillRangeGraceBufferUnits);
                    var effectiveRange = castRange > 0f
                        ? castRange + rangeGrace
                        : -1f;
                    interactionTarget = WorldTargetRef.FromCombatTarget(requestedTarget);
                    interactionRange = effectiveRange;
                    break;
            }

            var gateResult = await _interactionGate.PrepareAsync(new WorldInteractionGateRequest(
                player,
                instance,
                interactionTarget,
                interactionRange,
                WorldInteractionActionKind.CombatSkill,
                "AttackEnemy",
                MessageCode.SkillTargetOutOfRange));
            if (!gateResult.Success)
            {
                if (gateResult.SuppressFailure)
                    return;

                _network.Send(session.ConnectionId, new AttackEnemyResultPacket
                {
                    Success = false,
                    Code = gateResult.Code,
                    Target = packet.Target,
                    SkillSlotIndex = packet.SkillSlotIndex,
                    PlayerSkillId = castContext.PlayerSkillId,
                    SkillId = castContext.SkillId
                });
                return;
            }

            if (hasTarget && castContext.Skill.TargetType != SkillTargetType.Self)
            {
                if (!gateResult.TargetSnapshot.CombatTarget.HasValue ||
                    !gateResult.TargetSnapshot.CombatTarget.Value.IsAlive)
                {
                    _network.Send(session.ConnectionId, new AttackEnemyResultPacket
                    {
                        Success = false,
                        Code = MessageCode.SkillTargetInvalid,
                        Target = packet.Target,
                        SkillSlotIndex = packet.SkillSlotIndex,
                        PlayerSkillId = castContext.PlayerSkillId,
                        SkillId = castContext.SkillId
                    });
                    return;
                }

                targetSnapshot = gateResult.TargetSnapshot.CombatTarget.Value;
                if (!IsTargetCompatible(player, castContext.Skill.TargetType, requestedTarget, targetSnapshot))
                {
                    _network.Send(session.ConnectionId, new AttackEnemyResultPacket
                    {
                        Success = false,
                        Code = MessageCode.SkillTargetInvalid,
                        Target = packet.Target,
                        SkillSlotIndex = packet.SkillSlotIndex,
                        PlayerSkillId = castContext.PlayerSkillId,
                        SkillId = castContext.SkillId
                    });
                    return;
                }
            }

            player.ClearDesiredMovementTarget();
            utcNow = DateTime.UtcNow;
            var cooldownEndsAtUtc = utcNow.AddMilliseconds(Math.Max(0, castContext.Skill.CooldownMs));
            var execution = instance.EnqueueSkillExecution(
                new CombatTargetReference(
                    GameShared.Enums.CombatTargetKind.Character,
                    player.CharacterData.CharacterId,
                    null,
                    null),
                player.PlayerId,
                player.CharacterData.CharacterId,
                castContext.PlayerSkillId,
                castContext.SkillId,
                castContext.Skill.Code,
                castContext.Skill.GroupCode,
                castContext.SkillSlotIndex,
                castContext.Skill.TargetType,
                _skillExecutionService.CaptureCasterStats(player, utcNow),
                hasTarget ? requestedTarget : null,
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
                    Target = packet.Target,
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
                Target = packet.Target,
                SkillExecutionId = execution.ExecutionId,
                SkillSlotIndex = packet.SkillSlotIndex,
                PlayerSkillId = castContext.PlayerSkillId,
                SkillId = castContext.SkillId,
                CooldownMs = castContext.Skill.CooldownMs,
                CooldownEndsUnixMs = ToUnixMs(cooldownEndsAtUtc),
                CastStartedUnixMs = ToUnixMs(execution.CastStartedAtUtc),
                CastCompletedUnixMs = ToUnixMs(execution.CastCompletedAtUtc),
                ImpactUnixMs = ToUnixMs(execution.ImpactAtUtc),
                DamageApplied = 0,
                RemainingHp = null,
                IsKilled = false
            });
        }
        catch (GameException ex)
        {
            _network.Send(session.ConnectionId, new AttackEnemyResultPacket
            {
                Success = false,
                Code = ex.Code,
                Target = packet.Target,
                SkillSlotIndex = packet.SkillSlotIndex
            });
        }
    }

    private static bool IsTargetCompatible(
        PlayerSession caster,
        SkillTargetType targetType,
        CombatTargetReference requestedTarget,
        CombatTargetSnapshot snapshot)
    {
        var isSelfCharacter = snapshot.Kind == CombatTargetKind.Character &&
                              snapshot.CharacterId.HasValue &&
                              snapshot.CharacterId.Value == caster.CharacterData.CharacterId;

        return targetType switch
        {
            SkillTargetType.Self => isSelfCharacter,
            SkillTargetType.SingleEnemy => requestedTarget.Kind switch
            {
                CombatTargetKind.Character => !isSelfCharacter,
                CombatTargetKind.Enemy => true,
                CombatTargetKind.Boss => true,
                CombatTargetKind.Dummy => true,
                _ => false
            },
            SkillTargetType.SingleAlly => requestedTarget.Kind switch
            {
                CombatTargetKind.Character => true,
                CombatTargetKind.Npc => true,
                _ => false
            },
            _ => false
        };
    }

    private static long ToUnixMs(DateTime utc)
    {
        var normalized = utc.Kind == DateTimeKind.Utc
            ? utc
            : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return new DateTimeOffset(normalized).ToUnixTimeMilliseconds();
    }
}
