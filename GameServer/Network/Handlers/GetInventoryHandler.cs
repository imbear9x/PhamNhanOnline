using GameServer.DTO;
using GameServer.Network.Interface;
using GameServer.Services;
using GameServer.Config;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class GetInventoryHandler : IPacketHandler<GetInventoryPacket>
{
    private readonly ItemService _itemService;
    private readonly GameConfigValues _gameConfig;
    private readonly INetworkSender _network;

    public GetInventoryHandler(ItemService itemService, GameConfigValues gameConfig, INetworkSender network)
    {
        _itemService = itemService;
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

        var items = await _itemService.GetInventoryAsync(session.Player.CharacterData.CharacterId);
        _network.Send(session.ConnectionId, new GetInventoryResultPacket
        {
            Success = true,
            Code = MessageCode.None,
            EquipmentSlotCount = _gameConfig.CharacterEquipmentSlotCount,
            Items = items.Select(x => x.ToModel()).ToList()
        });
    }
}
