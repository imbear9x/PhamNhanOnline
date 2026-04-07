using GameServer.DTO;
using GameServer.Exceptions;
using GameServer.Network.Interface;
using GameServer.Services;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class UseMartialArtBookHandler : IPacketHandler<UseMartialArtBookPacket>
{
    private readonly ItemUseService _itemUseService;
    private readonly INetworkSender _network;

    public UseMartialArtBookHandler(
        ItemUseService itemUseService,
        INetworkSender network)
    {
        _itemUseService = itemUseService;
        _network = network;
    }

    public async Task HandleAsync(ConnectionSession session, UseMartialArtBookPacket packet)
    {
        if (session.Player is null)
        {
            _network.Send(session.ConnectionId, new UseMartialArtBookResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterMustEnterWorld
            });
            return;
        }

        try
        {
            var result = await _itemUseService.UseAsync(session.Player, packet.PlayerItemId!.Value, 1);
            if (result.LearnedMartialArt is null)
                throw new InvalidOperationException("Generic item use did not return a learned martial art for a martial art book request.");

            _network.Send(session.ConnectionId, new UseMartialArtBookResultPacket
            {
                Success = true,
                Code = MessageCode.None,
                BaseStats = result.BaseStats?.ToModel(),
                LearnedMartialArt = result.LearnedMartialArt.ToModel(),
                CultivationPreview = result.CultivationPreview?.ToModel()
            });
        }
        catch (GameException ex)
        {
            _network.Send(session.ConnectionId, new UseMartialArtBookResultPacket
            {
                Success = false,
                Code = ex.Code
            });
        }
    }
}
