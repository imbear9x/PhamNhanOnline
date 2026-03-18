using GameServer.Network.Interface;
using GameServer.World;
using GameShared.Packets;

namespace GameServer.Runtime;

public sealed class MapInstanceLifecycleService
{
    private readonly WorldManager _worldManager;
    private readonly MapCatalog _mapCatalog;
    private readonly CharacterRuntimeService _runtimeService;
    private readonly WorldInterestService _interestService;
    private readonly INetworkSender _network;

    public MapInstanceLifecycleService(
        WorldManager worldManager,
        MapCatalog mapCatalog,
        CharacterRuntimeService runtimeService,
        WorldInterestService interestService,
        INetworkSender network)
    {
        _worldManager = worldManager;
        _mapCatalog = mapCatalog;
        _runtimeService = runtimeService;
        _interestService = interestService;
        _network = network;
    }

    public void HandleAfterWorldTick(MapInstance instance, DateTime utcNow)
    {
        if (!instance.ShouldDestroy(utcNow))
            return;

        if (instance.PlayerCount <= 0)
        {
            _worldManager.MapManager.DestroyInstance(instance.MapId, instance.InstanceId);
            return;
        }

        var homeDefinition = _mapCatalog.ResolveHomeDefinition();
        var closeReason = ResolveCloseReason(instance, utcNow);

        foreach (var player in instance.GetPlayersSnapshot())
        {
            _network.Send(player.ConnectionId, new MapInstanceClosedPacket
            {
                ClosedMapId = instance.MapId,
                ClosedInstanceId = instance.InstanceId,
                Reason = (int)closeReason,
                RedirectMapId = homeDefinition.MapId,
                RedirectZoneIndex = homeDefinition.DefaultZoneIndex,
                RedirectPosX = homeDefinition.DefaultSpawnPosition.X,
                RedirectPosY = homeDefinition.DefaultSpawnPosition.Y
            });

            _runtimeService.UpdatePosition(
                player,
                homeDefinition.MapId,
                homeDefinition.DefaultZoneIndex,
                homeDefinition.DefaultSpawnPosition);
            _interestService.PublishWorldSnapshot(player);
        }

        _worldManager.MapManager.DestroyInstance(instance.MapId, instance.InstanceId);
    }

    private static MapInstanceCloseReason ResolveCloseReason(MapInstance instance, DateTime utcNow)
    {
        if (instance.ExpiresAtUtc.HasValue && utcNow >= instance.ExpiresAtUtc.Value)
            return MapInstanceCloseReason.Expired;

        return MapInstanceCloseReason.Completed;
    }
}
