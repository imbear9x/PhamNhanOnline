using GameServer.Network.Interface;
using GameServer.Runtime;
using GameServer.World;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class CharacterPositionSyncHandler : IPacketHandler<CharacterPositionSyncPacket>
{
    private readonly CharacterRuntimeService _runtimeService;
    private readonly MapCatalog _mapCatalog;

    public CharacterPositionSyncHandler(
        CharacterRuntimeService runtimeService,
        MapCatalog mapCatalog)
    {
        _runtimeService = runtimeService;
        _mapCatalog = mapCatalog;
    }

    public Task HandleAsync(ConnectionSession session, CharacterPositionSyncPacket packet)
    {
        if (session.Player == null || !packet.CurrentPosX.HasValue || !packet.CurrentPosY.HasValue)
            return Task.CompletedTask;

        var player = session.Player;
        if (!_mapCatalog.TryGet(player.MapId, out var definition))
            return Task.CompletedTask;

        var position = definition.ClampPosition(new System.Numerics.Vector2(packet.CurrentPosX.Value, packet.CurrentPosY.Value));
        _runtimeService.UpdatePosition(player, player.MapId, player.ZoneIndex, position, notifySelf: false);
        return Task.CompletedTask;
    }
}
