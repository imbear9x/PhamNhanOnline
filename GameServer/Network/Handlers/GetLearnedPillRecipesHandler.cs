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
    private readonly PillRecipeService _pillRecipeService;
    private readonly AlchemyDefinitionCatalog _alchemyDefinitions;
    private readonly AlchemyModelBuilder _modelBuilder;
    private readonly INetworkSender _network;

    public GetLearnedPillRecipesHandler(
        PillRecipeService pillRecipeService,
        AlchemyDefinitionCatalog alchemyDefinitions,
        AlchemyModelBuilder modelBuilder,
        INetworkSender network)
    {
        _pillRecipeService = pillRecipeService;
        _alchemyDefinitions = alchemyDefinitions;
        _modelBuilder = modelBuilder;
        _network = network;
    }

    public async Task HandleAsync(ConnectionSession session, GetLearnedPillRecipesPacket packet)
    {
        if (session.Player is null)
        {
            _network.Send(session.ConnectionId, new GetLearnedPillRecipesResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterMustEnterWorld
            });
            return;
        }

        var recipes = await _pillRecipeService.GetLearnedRecipesAsync(session.Player.CharacterData.CharacterId);
        var models = new List<LearnedPillRecipeModel>(recipes.Count);
        foreach (var recipe in recipes)
        {
            if (!_alchemyDefinitions.TryGetPillRecipe(recipe.PillRecipeTemplateId, out var definition))
                continue;

            models.Add(_modelBuilder.BuildLearnedRecipeModel(recipe, definition));
        }

        _network.Send(session.ConnectionId, new GetLearnedPillRecipesResultPacket
        {
            Success = true,
            Code = MessageCode.None,
            Recipes = models
        });
    }
}
