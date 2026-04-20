using GameServer.DTO;
using GameServer.Network.Interface;
using GameServer.Runtime;
using GameServer.Services;
using GameShared.Messages;
using GameShared.Models;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class GetLearnedPillRecipesHandler : IPacketHandler<GetLearnedPillRecipesPacket>
{
    private readonly AlchemyCraftQueryService _alchemyCraftQueryService;
    private readonly INetworkSender _network;

    public GetLearnedPillRecipesHandler(
        AlchemyCraftQueryService alchemyCraftQueryService,
        INetworkSender network)
    {
        _alchemyCraftQueryService = alchemyCraftQueryService;
        _network = network;
    }

    public async Task HandleAsync(ConnectionSession session, GetLearnedPillRecipesPacket packet)
    {
        var result = await _alchemyCraftQueryService.GetLearnedRecipesAsync(session);

        _network.Send(session.ConnectionId, new GetLearnedPillRecipesResultPacket
        {
            Success = result.Success,
            Code = result.Code,
            Recipes = result.Recipes?.ToList()
        });
    }
}
