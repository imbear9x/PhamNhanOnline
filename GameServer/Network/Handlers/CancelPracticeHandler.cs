using GameServer.DTO;
using GameServer.Network.Interface;
using GameServer.Services;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class CancelPracticeHandler : IPacketHandler<CancelPracticePacket>
{
    private readonly PracticeService _practiceService;
    private readonly ItemService _itemService;
    private readonly INetworkSender _network;

    public CancelPracticeHandler(PracticeService practiceService, ItemService itemService, INetworkSender network)
    {
        _practiceService = practiceService;
        _itemService = itemService;
        _network = network;
    }

    public async Task HandleAsync(ConnectionSession session, CancelPracticePacket packet)
    {
        var result = await _practiceService.CancelAsync(session, packet.PracticeSessionId);
        _network.Send(session.ConnectionId, new CancelPracticeResultPacket
        {
            Success = result.Success,
            Code = result.Code
        });

        if (result.Success && session.Player is not null)
        {
            var inventoryItems = await _itemService.GetInventoryAsync(session.Player.CharacterData.CharacterId);
            _network.Send(session.ConnectionId, new GetInventoryResultPacket
            {
                Success = true,
                Code = MessageCode.None,
                Items = inventoryItems.Select(static item => item.ToModel()).ToList()
            });
        }
    }
}
