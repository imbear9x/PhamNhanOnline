using GameServer.DTO;
using GameServer.Exceptions;
using GameServer.Network.Interface;
using GameServer.Services;
using GameShared.Logging;
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
        var recipeId = packet.PillRecipeTemplateId!.Value;
        Logger.Info(
            $"[AlchemyRecipeDetail] request conn={session.ConnectionId} " +
            $"characterId={session.Player?.CharacterData.CharacterId.ToString() ?? "<none>"} recipeId={recipeId}.");

        try
        {
            var result = await _alchemyCraftQueryService.GetRecipeDetailAsync(
                session,
                recipeId);

            Logger.Info(
                $"[AlchemyRecipeDetail] response conn={session.ConnectionId} recipeId={recipeId} " +
                $"success={result.Success} code={result.Code} hasRecipe={result.Recipe.HasValue} " +
                $"inputCount={(result.Recipe.HasValue && result.Recipe.Value.Inputs is not null ? result.Recipe.Value.Inputs.Count : 0)}.");

            _network.Send(session.ConnectionId, new GetPillRecipeDetailResultPacket
            {
                Success = result.Success,
                Code = result.Code,
                Recipe = result.Recipe
            });
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"[AlchemyRecipeDetail] unhandled exception conn={session.ConnectionId} recipeId={recipeId}.");
            _network.Send(session.ConnectionId, new GetPillRecipeDetailResultPacket
            {
                Success = false,
                Code = MessageCode.UnknownError,
                FailureReason = ex.Message
            });
        }
    }
}
