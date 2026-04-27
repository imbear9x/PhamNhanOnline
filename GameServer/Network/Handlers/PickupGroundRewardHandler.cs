using GameServer.Config;
using GameServer.DTO;
using GameServer.Network.Interface;
using GameServer.Services;
using GameServer.World;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class PickupGroundRewardHandler : IPacketHandler<PickupGroundRewardPacket>
{
    private readonly ItemService _itemService;
    private readonly GameConfigValues _gameConfig;
    private readonly INetworkSender _network;
    private readonly WorldManager _worldManager;

    public PickupGroundRewardHandler(
        ItemService itemService,
        GameConfigValues gameConfig,
        INetworkSender network,
        WorldManager worldManager)
    {
        _itemService = itemService;
        _gameConfig = gameConfig;
        _network = network;
        _worldManager = worldManager;
    }

    public async Task HandleAsync(ConnectionSession session, PickupGroundRewardPacket packet)
    {
        if (session.Player is null)
        {
            _network.Send(session.ConnectionId, new PickupGroundRewardResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterMustEnterWorld,
                RewardId = packet.RewardId
            });
            return;
        }

        var player = session.Player;
        if (!_worldManager.MapManager.TryGetInstance(player.MapId, player.InstanceId, out var instance))
        {
            _network.Send(session.ConnectionId, new PickupGroundRewardResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterNotInWorldInstance,
                RewardId = packet.RewardId
            });
            return;
        }

        if (!packet.RewardId.HasValue)
        {
            _network.Send(session.ConnectionId, new PickupGroundRewardResultPacket
            {
                Success = false,
                Code = MessageCode.GroundRewardIdInvalid,
                RewardId = packet.RewardId
            });
            return;
        }

        var utcNow = DateTime.UtcNow;
        if (!instance.TryGetGroundRewardPickupPosition(
                player.CharacterData.CharacterId,
                packet.RewardId.Value,
                utcNow,
                out var rewardPosition,
                out var lookupFailureCode))
        {
            _network.Send(session.ConnectionId, new PickupGroundRewardResultPacket
            {
                Success = false,
                Code = lookupFailureCode,
                RewardId = packet.RewardId
            });
            return;
        }

        var maxPickupDistance = MathF.Max(0f, _gameConfig.GroundRewardPickupRadiusServerUnits);
        var pickerPosition = player.CapturePositionSyncAnchor().Position;
        if (System.Numerics.Vector2.DistanceSquared(pickerPosition, rewardPosition) >
            maxPickupDistance * maxPickupDistance)
        {
            var waitResult = await PlayerInteractionMovementWait.WaitUntilWithinRangeAsync(
                player,
                rewardPosition,
                maxPickupDistance,
                DateTime.UtcNow);
            if (waitResult != InteractionMovementWaitResult.Reached)
            {
                if (waitResult == InteractionMovementWaitResult.CharacterDefeated)
                    return;

                _network.Send(session.ConnectionId, new PickupGroundRewardResultPacket
                {
                    Success = false,
                    Code = MessageCode.GroundRewardOutOfRange,
                    RewardId = packet.RewardId
                });
                return;
            }
        }

        if (!instance.TryClaimGroundReward(
                player.CharacterData.CharacterId,
                packet.RewardId.Value,
                player.CapturePositionSyncAnchor().Position,
                maxPickupDistance,
                DateTime.UtcNow,
                out var reward,
                out var failureCode))
        {
            _network.Send(session.ConnectionId, new PickupGroundRewardResultPacket
            {
                Success = false,
                Code = failureCode,
                RewardId = packet.RewardId
            });
            return;
        }

        player.ClearDesiredMovementTarget();

        foreach (var item in reward.Items)
            await _itemService.MoveGroundItemToInventoryAsync(player.CharacterData.CharacterId, item.PlayerItemId, CancellationToken.None);

        _network.Send(session.ConnectionId, new PickupGroundRewardResultPacket
        {
            Success = true,
            Code = MessageCode.None,
            RewardId = reward.Id,
            GrantedItems = reward.Items.Select(x => x.ToModel()).ToList()
        });
    }
}
