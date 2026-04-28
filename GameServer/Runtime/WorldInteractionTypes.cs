using System.Numerics;
using GameServer.World;
using GameShared.Enums;
using GameShared.Messages;

namespace GameServer.Runtime;

public enum WorldInteractionActionKind
{
    PortalTravel = 1,
    GroundRewardPickup = 2,
    CombatSkill = 3
}

public enum WorldTargetKind
{
    Player = 1,
    Enemy = 2,
    Boss = 3,
    Npc = 4,
    Portal = 5,
    GroundReward = 6,
    GroundPoint = 7,
    Dummy = 8
}

public readonly record struct WorldTargetRef(
    WorldTargetKind Kind,
    Guid? CharacterId,
    int? RuntimeId,
    int? PortalId,
    int? GroundRewardId,
    Vector2? GroundPosition)
{
    public static WorldTargetRef Portal(int portalId)
    {
        return new WorldTargetRef(WorldTargetKind.Portal, null, null, portalId, null, null);
    }

    public static WorldTargetRef GroundReward(int rewardId)
    {
        return new WorldTargetRef(WorldTargetKind.GroundReward, null, null, null, rewardId, null);
    }

    public static WorldTargetRef Player(Guid characterId)
    {
        return new WorldTargetRef(WorldTargetKind.Player, characterId, null, null, null, null);
    }

    public static WorldTargetRef FromCombatTarget(CombatTargetReference target)
    {
        return target.Kind switch
        {
            CombatTargetKind.Character => new WorldTargetRef(WorldTargetKind.Player, target.CharacterId, null, null, null, null),
            CombatTargetKind.Enemy => new WorldTargetRef(WorldTargetKind.Enemy, null, target.RuntimeId, null, null, null),
            CombatTargetKind.Boss => new WorldTargetRef(WorldTargetKind.Boss, null, target.RuntimeId, null, null, null),
            CombatTargetKind.Npc => new WorldTargetRef(WorldTargetKind.Npc, null, target.RuntimeId, null, null, null),
            CombatTargetKind.Dummy => new WorldTargetRef(WorldTargetKind.Dummy, null, target.RuntimeId, null, null, null),
            CombatTargetKind.GroundPoint => new WorldTargetRef(WorldTargetKind.GroundPoint, null, null, null, null, target.GroundPosition),
            _ => default
        };
    }

    public bool TryToCombatTarget(out CombatTargetReference combatTarget)
    {
        combatTarget = Kind switch
        {
            WorldTargetKind.Player => new CombatTargetReference(CombatTargetKind.Character, CharacterId, null, null),
            WorldTargetKind.Enemy => new CombatTargetReference(CombatTargetKind.Enemy, null, RuntimeId, null),
            WorldTargetKind.Boss => new CombatTargetReference(CombatTargetKind.Boss, null, RuntimeId, null),
            WorldTargetKind.Npc => new CombatTargetReference(CombatTargetKind.Npc, null, RuntimeId, null),
            WorldTargetKind.Dummy => new CombatTargetReference(CombatTargetKind.Dummy, null, RuntimeId, null),
            WorldTargetKind.GroundPoint => new CombatTargetReference(CombatTargetKind.GroundPoint, null, null, GroundPosition),
            _ => default
        };
        return combatTarget.IsValid;
    }
}

public readonly record struct WorldTargetSnapshot(
    WorldTargetRef Target,
    int MapId,
    int InstanceId,
    int ZoneIndex,
    Vector2 Position,
    bool IsAlive,
    bool IsInteractable,
    MessageCode FailureCode,
    CombatTargetSnapshot? CombatTarget);

public readonly record struct WorldTargetResolveResult(
    bool Success,
    WorldTargetSnapshot Snapshot,
    MessageCode FailureCode)
{
    public static WorldTargetResolveResult Resolved(WorldTargetSnapshot snapshot)
    {
        return new WorldTargetResolveResult(true, snapshot, MessageCode.None);
    }

    public static WorldTargetResolveResult Failed(MessageCode failureCode)
    {
        return new WorldTargetResolveResult(false, default, failureCode);
    }
}

public enum WorldInteractionGateStatus
{
    Success = 1,
    Failed = 2,
    CharacterDefeated = 3
}

public readonly record struct WorldInteractionGateRequest(
    PlayerSession Player,
    MapInstance Instance,
    WorldTargetRef Target,
    float MaxDistance,
    WorldInteractionActionKind ActionKind,
    string ActionName,
    MessageCode OutOfRangeCode);

public readonly record struct WorldInteractionGateResult(
    WorldInteractionGateStatus Status,
    MessageCode Code,
    WorldTargetSnapshot TargetSnapshot,
    InteractionMovementWaitResult? WaitResult,
    WorldRuntimeSettlementResult? SettlementResult)
{
    public bool Success => Status == WorldInteractionGateStatus.Success;

    public bool SuppressFailure => Status == WorldInteractionGateStatus.CharacterDefeated;

    public static WorldInteractionGateResult Succeeded(WorldTargetSnapshot targetSnapshot)
    {
        return new WorldInteractionGateResult(
            WorldInteractionGateStatus.Success,
            MessageCode.None,
            targetSnapshot,
            null,
            null);
    }

    public static WorldInteractionGateResult Failed(MessageCode code)
    {
        return new WorldInteractionGateResult(
            WorldInteractionGateStatus.Failed,
            code,
            default,
            null,
            null);
    }

    public static WorldInteractionGateResult Failed(MessageCode code, InteractionMovementWaitResult waitResult)
    {
        return new WorldInteractionGateResult(
            WorldInteractionGateStatus.Failed,
            code,
            default,
            waitResult,
            null);
    }

    public static WorldInteractionGateResult Failed(MessageCode code, WorldRuntimeSettlementResult settlementResult)
    {
        return new WorldInteractionGateResult(
            WorldInteractionGateStatus.Failed,
            code,
            default,
            null,
            settlementResult);
    }

    public static WorldInteractionGateResult CharacterDefeated(InteractionMovementWaitResult? waitResult = null)
    {
        return new WorldInteractionGateResult(
            WorldInteractionGateStatus.CharacterDefeated,
            MessageCode.CharacterActionsRestricted,
            default,
            waitResult,
            null);
    }

    public static WorldInteractionGateResult CharacterDefeated(WorldRuntimeSettlementResult settlementResult)
    {
        return new WorldInteractionGateResult(
            WorldInteractionGateStatus.CharacterDefeated,
            MessageCode.CharacterActionsRestricted,
            default,
            null,
            settlementResult);
    }
}
