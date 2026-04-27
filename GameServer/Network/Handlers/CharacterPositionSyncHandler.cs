using System.Numerics;
using GameServer.Network.Interface;
using GameServer.Runtime;
using GameServer.World;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class CharacterPositionSyncHandler : IPacketHandler<CharacterPositionSyncPacket>
{
    private const float ArriveDistance = 0.01f;

    private readonly CharacterCultivationService _cultivationService;
    private readonly MapCatalog _mapCatalog;

    public CharacterPositionSyncHandler(
        CharacterCultivationService cultivationService,
        MapCatalog mapCatalog)
    {
        _cultivationService = cultivationService;
        _mapCatalog = mapCatalog;
    }

    public Task HandleAsync(ConnectionSession session, CharacterPositionSyncPacket packet)
    {
        if (session.Player == null || !packet.CurrentPosX.HasValue || !packet.CurrentPosY.HasValue)
            return Task.CompletedTask;

        if (!float.IsFinite(packet.CurrentPosX.Value) || !float.IsFinite(packet.CurrentPosY.Value))
            return Task.CompletedTask;

        var player = session.Player;
        var utcNow = DateTime.UtcNow;
        var runtimeSnapshot = player.RuntimeState.CaptureSnapshot();
        var currentState = runtimeSnapshot.CurrentState.CurrentState;
        if (CharacterRuntimeStateCodes.IsDefeated(runtimeSnapshot.CurrentState))
        {
            player.ClearDesiredMovementTarget();
            return Task.CompletedTask;
        }

        if (_cultivationService.IsCultivating(player) ||
            currentState == CharacterRuntimeStateCodes.Practicing ||
            currentState == CharacterRuntimeStateCodes.Casting ||
            player.IsStunned(utcNow))
            return Task.CompletedTask;

        if (!_mapCatalog.TryGet(player.MapId, out var definition))
            return Task.CompletedTask;

        var targetPosition = definition.ClampPosition(new Vector2(packet.CurrentPosX.Value, packet.CurrentPosY.Value));
        var anchor = player.CapturePositionSyncAnchor();

        if (Vector2.DistanceSquared(anchor.Position, targetPosition) <= ArriveDistance * ArriveDistance)
            player.ClearDesiredMovementTarget();
        else
            player.SetDesiredMovementTarget(targetPosition, utcNow);

        return Task.CompletedTask;
    }
}
