using GameServer.Network.Interface;
using GameServer.Services;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class ConfirmPermanentCharacterDeletionHandler : IPacketHandler<ConfirmPermanentCharacterDeletionPacket>
{
    private readonly PermanentCharacterDeletionService _deletionService;
    private readonly INetworkSender _network;

    public ConfirmPermanentCharacterDeletionHandler(
        PermanentCharacterDeletionService deletionService,
        INetworkSender network)
    {
        _deletionService = deletionService;
        _network = network;
    }

    public async Task HandleAsync(ConnectionSession session, ConfirmPermanentCharacterDeletionPacket packet)
    {
        try
        {
            var result = await _deletionService.ConfirmAsync(session.PlayerId, packet.CharacterId!.Value);
            _network.Send(session.ConnectionId, new ConfirmPermanentCharacterDeletionResultPacket
            {
                Success = result.Success,
                Code = result.Code,
                CharacterId = result.CharacterId
            });
        }
        catch (Exception)
        {
            _network.Send(session.ConnectionId, new ConfirmPermanentCharacterDeletionResultPacket
            {
                Success = false,
                Code = MessageCode.UnknownError,
                CharacterId = packet.CharacterId
            });
            throw;
        }
    }
}
