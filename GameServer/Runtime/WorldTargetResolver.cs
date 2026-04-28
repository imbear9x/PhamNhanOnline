using GameServer.World;
using GameShared.Messages;

namespace GameServer.Runtime;

public sealed class WorldTargetResolver
{
    private readonly MapCatalog _mapCatalog;

    public WorldTargetResolver(MapCatalog mapCatalog)
    {
        _mapCatalog = mapCatalog;
    }

    public WorldTargetResolveResult ResolveForPlayerInteraction(
        PlayerSession player,
        MapInstance instance,
        WorldTargetRef target,
        DateTime utcNow)
    {
        if (!IsPlayerInInstance(player, instance))
            return WorldTargetResolveResult.Failed(MessageCode.CharacterNotInWorldInstance);

        return target.Kind switch
        {
            WorldTargetKind.Portal => ResolvePortal(player, instance, target),
            WorldTargetKind.GroundReward => ResolveGroundReward(player, instance, target, utcNow),
            WorldTargetKind.Player or
                WorldTargetKind.Enemy or
                WorldTargetKind.Boss or
                WorldTargetKind.Npc or
                WorldTargetKind.Dummy or
                WorldTargetKind.GroundPoint => ResolveCombatTarget(player, instance, target),
            _ => WorldTargetResolveResult.Failed(MessageCode.SkillTargetInvalid)
        };
    }

    private WorldTargetResolveResult ResolvePortal(
        PlayerSession player,
        MapInstance instance,
        WorldTargetRef target)
    {
        if (!target.PortalId.HasValue ||
            !_mapCatalog.TryGetPortal(player.MapId, target.PortalId.Value, out var portal) ||
            !portal.IsEnabled)
        {
            return WorldTargetResolveResult.Failed(MessageCode.MapPortalInvalid);
        }

        return WorldTargetResolveResult.Resolved(new WorldTargetSnapshot(
            target,
            player.MapId,
            instance.InstanceId,
            player.ZoneIndex,
            portal.SourcePosition,
            IsAlive: true,
            IsInteractable: true,
            MessageCode.None,
            CombatTarget: null));
    }

    private static WorldTargetResolveResult ResolveGroundReward(
        PlayerSession player,
        MapInstance instance,
        WorldTargetRef target,
        DateTime utcNow)
    {
        if (!target.GroundRewardId.HasValue)
            return WorldTargetResolveResult.Failed(MessageCode.GroundRewardIdInvalid);

        if (!instance.TryGetGroundRewardPickupPosition(
                player.CharacterData.CharacterId,
                target.GroundRewardId.Value,
                utcNow,
                out var rewardPosition,
                out var failureCode))
        {
            return WorldTargetResolveResult.Failed(failureCode);
        }

        return WorldTargetResolveResult.Resolved(new WorldTargetSnapshot(
            target,
            player.MapId,
            instance.InstanceId,
            player.ZoneIndex,
            rewardPosition,
            IsAlive: true,
            IsInteractable: true,
            MessageCode.None,
            CombatTarget: null));
    }

    private static WorldTargetResolveResult ResolveCombatTarget(
        PlayerSession player,
        MapInstance instance,
        WorldTargetRef target)
    {
        if (!target.TryToCombatTarget(out var combatTarget) ||
            !instance.TryGetCombatTargetSnapshot(combatTarget, out var combatSnapshot) ||
            !combatSnapshot.IsAlive)
        {
            return WorldTargetResolveResult.Failed(MessageCode.SkillTargetInvalid);
        }

        return WorldTargetResolveResult.Resolved(new WorldTargetSnapshot(
            target,
            player.MapId,
            instance.InstanceId,
            player.ZoneIndex,
            combatSnapshot.Position,
            combatSnapshot.IsAlive,
            IsInteractable: true,
            MessageCode.None,
            combatSnapshot));
    }

    private static bool IsPlayerInInstance(PlayerSession player, MapInstance instance)
    {
        return player.MapId == instance.MapId &&
               player.InstanceId == instance.InstanceId &&
               player.ZoneIndex == instance.ZoneIndex;
    }
}
