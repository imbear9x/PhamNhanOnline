using GameServer.Network.Interface;
using GameServer.Runtime;
using GameServer.Services;
using GameServer.World;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class DropInventoryItemHandler : IPacketHandler<DropInventoryItemPacket>
{
    private const int DropOwnershipSeconds = 10;
    private const int DropFreeForAllSeconds = 50;

    private readonly ItemService _itemService;
    private readonly ItemDefinitionCatalog _itemDefinitions;
    private readonly INetworkSender _network;
    private readonly WorldManager _worldManager;

    public DropInventoryItemHandler(
        ItemService itemService,
        ItemDefinitionCatalog itemDefinitions,
        INetworkSender network,
        WorldManager worldManager)
    {
        _itemService = itemService;
        _itemDefinitions = itemDefinitions;
        _network = network;
        _worldManager = worldManager;
    }

    public async Task HandleAsync(ConnectionSession session, DropInventoryItemPacket packet)
    {
        if (session.Player is null)
        {
            _network.Send(session.ConnectionId, new DropInventoryItemResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterMustEnterWorld,
                PlayerItemId = packet.PlayerItemId,
                Quantity = packet.Quantity
            });
            return;
        }

        var player = session.Player;
        if (!_worldManager.MapManager.TryGetInstance(player.MapId, player.InstanceId, out var instance))
        {
            _network.Send(session.ConnectionId, new DropInventoryItemResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterNotInWorldInstance,
                PlayerItemId = packet.PlayerItemId,
                Quantity = packet.Quantity
            });
            return;
        }

        try
        {
            var droppedItem = await _itemService.MoveItemToGroundAsync(
                player.CharacterData.CharacterId,
                packet.PlayerItemId!.Value,
                packet.Quantity!.Value,
                CancellationToken.None);

            if (!_itemDefinitions.TryGetItem(droppedItem.ItemTemplateId, out var definition))
            {
                _network.Send(session.ConnectionId, new DropInventoryItemResultPacket
                {
                    Success = false,
                    Code = MessageCode.InventoryItemInvalid,
                    PlayerItemId = packet.PlayerItemId,
                    Quantity = packet.Quantity
                });
                return;
            }

            var utcNow = DateTime.UtcNow;
            var freeAtUtc = utcNow.AddSeconds(DropOwnershipSeconds);
            var destroyAtUtc = freeAtUtc.AddSeconds(DropFreeForAllSeconds);
            var reward = new GroundRewardEntity(
                instance.AllocateGroundRewardId(),
                player.CharacterData.CharacterId,
                player.Position,
                new[]
                {
                    new GroundRewardItem(
                        droppedItem.Id,
                        droppedItem.ItemTemplateId,
                        definition.Code,
                        definition.Name,
                        definition.ItemType,
                        definition.Rarity,
                        droppedItem.Quantity,
                        droppedItem.IsBound,
                        definition.Icon,
                        definition.BackgroundIcon)
                },
                utcNow,
                freeAtUtc,
                destroyAtUtc);

            instance.AddGroundReward(reward);

            _network.Send(session.ConnectionId, new DropInventoryItemResultPacket
            {
                Success = true,
                Code = MessageCode.None,
                PlayerItemId = packet.PlayerItemId,
                Quantity = packet.Quantity,
                RewardId = reward.Id
            });
        }
        catch (ArgumentOutOfRangeException)
        {
            _network.Send(session.ConnectionId, new DropInventoryItemResultPacket
            {
                Success = false,
                Code = MessageCode.InventoryItemQuantityInvalid,
                PlayerItemId = packet.PlayerItemId,
                Quantity = packet.Quantity
            });
        }
        catch (InvalidOperationException)
        {
            _network.Send(session.ConnectionId, new DropInventoryItemResultPacket
            {
                Success = false,
                Code = MessageCode.InventoryItemInvalid,
                PlayerItemId = packet.PlayerItemId,
                Quantity = packet.Quantity
            });
        }
    }
}
