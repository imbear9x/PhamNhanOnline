using GameServer.DTO;
using GameServer.Exceptions;
using GameServer.Network.Interface;
using GameServer.Services;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class PreviewCraftPillHandler : IPacketHandler<PreviewCraftPillPacket>
{
    private readonly AlchemyCraftQueryService _alchemyCraftQueryService;
    private readonly INetworkSender _network;

    public PreviewCraftPillHandler(
        AlchemyCraftQueryService alchemyCraftQueryService,
        INetworkSender network)
    {
        _alchemyCraftQueryService = alchemyCraftQueryService;
        _network = network;
    }

    public async Task HandleAsync(ConnectionSession session, PreviewCraftPillPacket packet)
    {
        var result = await _alchemyCraftQueryService.PreviewCraftAsync(
            session,
            packet.PillRecipeTemplateId!.Value,
            packet.RequestedCraftCount ?? 1,
            packet.SelectedPlayerItemIds,
            packet.SelectedOptionalInputs);

        _network.Send(session.ConnectionId, new PreviewCraftPillResultPacket
        {
            Success = result.Success,
            Code = result.Code,
            FailureReason = result.FailureReason,
            Preview = result.Preview
        });
    }
}
