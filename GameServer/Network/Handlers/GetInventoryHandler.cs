using GameServer.DTO;
using GameServer.Network.Interface;
using GameServer.Services;
using GameServer.Config;
using GameShared.Messages;
using GameShared.Models;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class GetInventoryHandler : IPacketHandler<GetInventoryPacket>
{
    private readonly ItemService _itemService;
    private readonly BagService _bagService;
    private readonly GameConfigValues _gameConfig;
    private readonly INetworkSender _network;

    public GetInventoryHandler(ItemService itemService, BagService bagService, GameConfigValues gameConfig, INetworkSender network)
    {
        _itemService = itemService;
        _bagService = bagService;
        _gameConfig = gameConfig;
        _network = network;
    }

    public async Task HandleAsync(ConnectionSession session, GetInventoryPacket packet)
    {
        if (session.Player is null)
        {
            _network.Send(session.ConnectionId, new GetInventoryResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterMustEnterWorld
            });
            return;
        }

        var characterId = session.Player.CharacterData.CharacterId;
        var items = await _itemService.GetInventoryAsync(characterId);
        var bag = await _bagService.GetBagStateAsync(characterId);
        _network.Send(session.ConnectionId, new GetInventoryResultPacket
        {
            Success = true,
            Code = MessageCode.None,
            EquipmentSlotCount = _gameConfig.CharacterEquipmentSlotCount,
            BagState = new BagStateModel
            {
                Grade = bag.Grade,
                UsedSlots = bag.UsedSlots,
                TotalSlots = bag.TotalSlots,
                DisplayName = bag.DisplayName
            },
            Items = items.Select(x => x.ToModel()).ToList()
        });
    }
}
