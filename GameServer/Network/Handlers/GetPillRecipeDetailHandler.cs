using GameServer.DTO;
using GameServer.Exceptions;
using GameServer.Network.Interface;
using GameServer.Services;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class GetPillRecipeDetailHandler : IPacketHandler<GetPillRecipeDetailPacket>
{
    private readonly PillRecipeService _pillRecipeService;
    private readonly AlchemyModelBuilder _modelBuilder;
    private readonly INetworkSender _network;

    public GetPillRecipeDetailHandler(
        PillRecipeService pillRecipeService,
        AlchemyModelBuilder modelBuilder,
        INetworkSender network)
    {
        _pillRecipeService = pillRecipeService;
        _modelBuilder = modelBuilder;
        _network = network;
    }

    public async Task HandleAsync(ConnectionSession session, GetPillRecipeDetailPacket packet)
    {
        if (session.Player is null)
        {
            _network.Send(session.ConnectionId, new GetPillRecipeDetailResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterMustEnterWorld
            });
            return;
        }

        try
        {
            var detail = await _pillRecipeService.GetRecipeDetailAsync(
                session.Player.CharacterData.CharacterId,
                packet.PillRecipeTemplateId!.Value);

            _network.Send(session.ConnectionId, new GetPillRecipeDetailResultPacket
            {
                Success = true,
                Code = MessageCode.None,
                Recipe = _modelBuilder.BuildRecipeDetailModel(detail.Definition, detail.Progress)
            });
        }
        catch (GameException ex)
        {
            _network.Send(session.ConnectionId, new GetPillRecipeDetailResultPacket
            {
                Success = false,
                Code = ex.Code
            });
        }
    }
}
