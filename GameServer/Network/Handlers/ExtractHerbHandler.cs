using GameServer.DTO;
using GameServer.Exceptions;
using GameServer.Network.Interface;
using GameServer.Services;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class ExtractHerbHandler : IPacketHandler<ExtractHerbPacket>
{
    private readonly HerbService _herbService;
    private readonly INetworkSender _network;

    public ExtractHerbHandler(HerbService herbService, INetworkSender network)
    {
        _herbService = herbService;
        _network = network;
    }

    public async Task HandleAsync(ConnectionSession session, ExtractHerbPacket packet)
    {
        if (session.Player is null)
        {
            _network.Send(session.ConnectionId, new ExtractHerbResultPacket { Success = false, Code = MessageCode.CharacterMustEnterWorld });
            return;
        }

        try
        {
            var result = await _herbService.ExtractHerbAsync(session.Player.CharacterData.CharacterId, packet.PlayerHerbId ?? 0);
            _network.Send(session.ConnectionId, new ExtractHerbResultPacket
            {
                Success = true,
                Code = MessageCode.None,
                Items = result.Items.Select(x => x.ToModel()).ToList(),
                MamNonReturned = result.MamNonReturned
            });
        }
        catch (GameException ex)
        {
            _network.Send(session.ConnectionId, new ExtractHerbResultPacket
            {
                Success = false,
                Code = ex.Code,
                Items = new List<GameShared.Models.InventoryItemModel>(),
                MamNonReturned = false
            });
        }
    }
}
