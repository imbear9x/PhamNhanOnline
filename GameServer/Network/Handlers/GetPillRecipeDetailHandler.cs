using GameServer.DTO;
using GameServer.Exceptions;
using GameServer.Network.Interface;
using GameServer.Services;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class GetPillRecipeDetailHandler : IPacketHandler<GetPillRecipeDetailPacket>
{
    private readonly AlchemyCraftQueryService _alchemyCraftQueryService;
    private readonly INetworkSender _network;

    public GetPillRecipeDetailHandler(
        AlchemyCraftQueryService alchemyCraftQueryService,
        INetworkSender network)
    {
        _alchemyCraftQueryService = alchemyCraftQueryService;
        _network = network;
    }

    public async Task HandleAsync(ConnectionSession session, GetPillRecipeDetailPacket packet)
    {
        var result = await _alchemyCraftQueryService.GetRecipeDetailAsync(
            session,
            packet.PillRecipeTemplateId!.Value);

        _network.Send(session.ConnectionId, new GetPillRecipeDetailResultPacket
        {
            Success = result.Success,
            Code = result.Code,
            Recipe = result.Recipe
        });
    }
}
