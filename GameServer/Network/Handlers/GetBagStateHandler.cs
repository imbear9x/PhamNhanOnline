using GameServer.Network.Interface;
using GameServer.Services;
using GameShared.Messages;
using GameShared.Models;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class GetBagStateHandler : IPacketHandler<GetBagStatePacket>
{
    private readonly BagService _bagService;
    private readonly INetworkSender _network;

    public GetBagStateHandler(BagService bagService, INetworkSender network)
    {
        _bagService = bagService;
        _network = network;
    }

    public async Task HandleAsync(ConnectionSession session, GetBagStatePacket packet)
    {
        if (session.Player is null)
        {
            _network.Send(session.ConnectionId, new GetBagStateResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterMustEnterWorld
            });
            return;
        }

        var bag = await _bagService.GetBagStateAsync(session.Player.CharacterData.CharacterId);
        _network.Send(session.ConnectionId, new GetBagStateResultPacket
        {
            Success = true,
            Code = MessageCode.None,
            BagState = new BagStateModel
            {
                Grade = bag.Grade,
                UsedSlots = bag.UsedSlots,
                TotalSlots = bag.TotalSlots,
                DisplayName = bag.DisplayName
            }
        });
    }
}