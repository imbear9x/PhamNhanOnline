using GameServer.DTO;
using GameServer.Network.Interface;
using GameServer.Services;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class GetInventoryHandler : IPacketHandler<GetInventoryPacket>
{
    private readonly ItemService _itemService;
    private readonly INetworkSender _network;

    public GetInventoryHandler(ItemService itemService, INetworkSender network)
    {
        _itemService = itemService;
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
            Items = items.Select(x => x.ToModel()).ToList()
        });
    }
}
