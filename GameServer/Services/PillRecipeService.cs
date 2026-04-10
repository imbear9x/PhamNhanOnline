using GameServer.Entities;
using GameServer.Exceptions;
using GameServer.Repositories;
using GameServer.Runtime;
using GameShared.Messages;

namespace GameServer.Services;

public sealed class PillRecipeService
{
    private readonly AlchemyDefinitionCatalog _definitions;
    private readonly PlayerPillRecipeRepository _playerPillRecipes;
    private readonly PlayerItemRepository _playerItems;
    private readonly ItemService _itemService;

    public PillRecipeService(
        AlchemyDefinitionCatalog definitions,
        PlayerPillRecipeRepository playerPillRecipes,
        PlayerItemRepository playerItems,
        ItemService itemService)
    {
        _definitions = definitions;
        _playerPillRecipes = playerPillRecipes;
        _playerItems = playerItems;
        _itemService = itemService;
    }

    public async Task<LearnedPillRecipeView> LearnRecipeAsync(
        Guid playerId,
        long playerItemId,
        CancellationToken cancellationToken = default)
    {
        var playerItem = await _playerItems.GetByIdAsync(playerItemId, cancellationToken)
                         ?? throw new GameException(MessageCode.InventoryItemInvalid);
        if (playerItem.PlayerId != playerId || playerItem.LocationType != (int)ItemLocationType.Inventory)
            throw new GameException(MessageCode.InventoryItemInvalid);

        if (!_definitions.TryGetPillRecipeByBookItemTemplate(playerItem.ItemTemplateId, out var recipe))
            throw new GameException(MessageCode.ItemUseUnsupported);

        var existing = await _playerPillRecipes.GetByPlayerAndRecipeAsync(playerId, recipe.Id, cancellationToken);
        if (existing is not null)
            throw new GameException(MessageCode.PillRecipeAlreadyLearned);

        var entity = new PlayerPillRecipeEntity
        {
            PlayerId = playerId,
            PillRecipeTemplateId = recipe.Id,
            LearnedAt = DateTime.UtcNow,
            TotalCraftCount = 0,
            CurrentSuccessRateBonus = 0d,
            UpdatedAt = DateTime.UtcNow
        };

        await _playerPillRecipes.CreateAsync(entity, cancellationToken);
        await _itemService.RemovePlayerItemAsync(playerId, playerItemId, cancellationToken);

        return new LearnedPillRecipeView(
            recipe.Id,
            recipe.Code,
            recipe.Name,
            recipe.ResultPillItemTemplateId,
            0,
            0d,
            entity.LearnedAt);
    }

    public async Task<IReadOnlyList<LearnedPillRecipeView>> GetLearnedRecipesAsync(
        Guid playerId,
        CancellationToken cancellationToken = default)
    {
        var playerRecipes = await _playerPillRecipes.ListByPlayerIdAsync(playerId, cancellationToken);
        var result = new List<LearnedPillRecipeView>(playerRecipes.Count);
        foreach (var playerRecipe in playerRecipes)
        {
            if (!_definitions.TryGetPillRecipe(playerRecipe.PillRecipeTemplateId, out var definition))
                continue;

            result.Add(new LearnedPillRecipeView(
                definition.Id,
                definition.Code,
                definition.Name,
                definition.ResultPillItemTemplateId,
                playerRecipe.TotalCraftCount,
                playerRecipe.CurrentSuccessRateBonus,
                playerRecipe.LearnedAt));
        }

        return result;
    }

    public async Task<(PillRecipeTemplateDefinition Definition, PlayerPillRecipeEntity Progress)> GetRecipeDetailAsync(
        Guid playerId,
        int recipeId,
        CancellationToken cancellationToken = default)
    {
        if (!_definitions.TryGetPillRecipe(recipeId, out var definition))
            throw new GameException(MessageCode.PillRecipeInvalid);

        var progress = await _playerPillRecipes.GetByPlayerAndRecipeAsync(playerId, recipeId, cancellationToken)
                       ?? throw new GameException(MessageCode.PillRecipeNotLearned);

        return (definition, progress);
    }
}
