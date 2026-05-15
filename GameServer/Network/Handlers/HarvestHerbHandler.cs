using GameServer.Exceptions;
using GameServer.Network.Interface;
using GameServer.Services;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class HarvestHerbHandler : IPacketHandler<HarvestHerbPacket>
{
    private readonly HerbService _herbService;
    private readonly INetworkSender _network;

    public HarvestHerbHandler(HerbService herbService, INetworkSender network)
    {
        _herbService = herbService;
        _network = network;
    }

    public async Task HandleAsync(ConnectionSession session, HarvestHerbPacket packet)
    {
        if (session.Player is null)
        {
            _network.Send(session.ConnectionId, new HarvestHerbResultPacket { Success = false, Code = MessageCode.CharacterMustEnterWorld, PlayerHerbId = packet.PlayerHerbId });
            return;
        }

        try
        {
            var expireAt = await _herbService.HarvestAsync(session.Player.CharacterData.CharacterId, packet.PlayerHerbId ?? 0);
            _network.Send(session.ConnectionId, new HarvestHerbResultPacket
            {
                Success = true,
                Code = MessageCode.None,
                PlayerHerbId = packet.PlayerHerbId,
                ExpireAtUnixMs = new DateTimeOffset(expireAt).ToUnixTimeMilliseconds()
            });
        }
        catch (GameException ex)
        {
            _network.Send(session.ConnectionId, new HarvestHerbResultPacket
            {
                Success = false,
                Code = ex.Code,
                PlayerHerbId = packet.PlayerHerbId
            });
        }
    }
}
