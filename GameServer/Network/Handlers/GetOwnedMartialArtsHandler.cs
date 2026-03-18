using GameServer.DTO;
using GameServer.Network.Interface;
using GameServer.Services;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class GetOwnedMartialArtsHandler : IPacketHandler<GetOwnedMartialArtsPacket>
{
    private readonly MartialArtService _martialArtService;
    private readonly INetworkSender _network;

    public GetOwnedMartialArtsHandler(
        MartialArtService martialArtService,
        INetworkSender network)
    {
        _martialArtService = martialArtService;
        _network = network;
    }

    public async Task HandleAsync(ConnectionSession session, GetOwnedMartialArtsPacket packet)
    {
        if (session.Player is null)
        {
            _network.Send(session.ConnectionId, new GetOwnedMartialArtsResultPacket
            {
                Success = false,
                Code = GameShared.Messages.MessageCode.CharacterMustEnterWorld
            });
            return;
        }

        var snapshot = session.Player.RuntimeState.CaptureSnapshot();
        var martialArts = await _martialArtService.GetOwnedMartialArtsAsync(
            session.Player.CharacterData.CharacterId,
            snapshot.BaseStats.ActiveMartialArtId);

        _network.Send(session.ConnectionId, new GetOwnedMartialArtsResultPacket
        {
            Success = true,
            Code = GameShared.Messages.MessageCode.None,
            ActiveMartialArtId = snapshot.BaseStats.ActiveMartialArtId,
            MartialArts = martialArts.Select(static x => x.ToModel()).ToList()
        });
    }
}
