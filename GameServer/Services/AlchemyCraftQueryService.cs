using GameServer.DTO;
using GameServer.Exceptions;
using GameServer.Network;
using GameServer.Runtime;
using GameShared.Messages;
using GameShared.Models;

namespace GameServer.Services;

public sealed class AlchemyCraftQueryService
{
    private readonly PillRecipeService _pillRecipeService;
    private readonly AlchemyService _alchemyService;
    private readonly ItemService _itemService;
    private readonly AlchemyDefinitionCatalog _alchemyDefinitions;
    private readonly AlchemyModelBuilder _modelBuilder;

    public AlchemyCraftQueryService(
        PillRecipeService pillRecipeService,
        AlchemyService alchemyService,
        ItemService itemService,
        AlchemyDefinitionCatalog alchemyDefinitions,
        AlchemyModelBuilder modelBuilder)
    {
        _pillRecipeService = pillRecipeService;
        _alchemyService = alchemyService;
        _itemService = itemService;
        _alchemyDefinitions = alchemyDefinitions;
        _modelBuilder = modelBuilder;
    }

    public async Task<GetLearnedPillRecipesExecutionResult> GetLearnedRecipesAsync(
        ConnectionSession session,
        CancellationToken cancellationToken = default)
    {
        if (session.Player is null)
            return new GetLearnedPillRecipesExecutionResult(false, MessageCode.CharacterMustEnterWorld, null);

        var recipes = await _pillRecipeService.GetLearnedRecipesAsync(session.Player.CharacterData.CharacterId, cancellationToken);
        var models = new List<LearnedPillRecipeModel>(recipes.Count);
        foreach (var recipe in recipes)
        {
            if (!_alchemyDefinitions.TryGetPillRecipe(recipe.PillRecipeTemplateId, out var definition))
                continue;

            models.Add(_modelBuilder.BuildLearnedRecipeModel(recipe, definition));
        }

        return new GetLearnedPillRecipesExecutionResult(true, MessageCode.None, models);
    }

    public async Task<GetPillRecipeDetailExecutionResult> GetRecipeDetailAsync(
        ConnectionSession session,
        int recipeId,
        CancellationToken cancellationToken = default)
    {
        if (session.Player is null)
            return new GetPillRecipeDetailExecutionResult(false, MessageCode.CharacterMustEnterWorld, null);

        try
        {
            var detail = await _pillRecipeService.GetRecipeDetailAsync(
                session.Player.CharacterData.CharacterId,
                recipeId,
                cancellationToken);
            return new GetPillRecipeDetailExecutionResult(
                true,
                MessageCode.None,
                _modelBuilder.BuildRecipeDetailModel(detail.Definition, detail.Progress));
        }
        catch (GameException ex)
        {
            return new GetPillRecipeDetailExecutionResult(false, ex.Code, null);
        }
    }

    public async Task<PreviewCraftPillExecutionResult> PreviewCraftAsync(
        ConnectionSession session,
        int recipeId,
        int requestedCraftCount,
        IReadOnlyCollection<long>? selectedPlayerItemIds,
        IReadOnlyCollection<AlchemyOptionalInputSelectionModel>? selectedOptionalInputs,
        CancellationToken cancellationToken = default)
    {
        if (session.Player is null)
            return new PreviewCraftPillExecutionResult(false, MessageCode.CharacterMustEnterWorld, null, null);

        var playerId = session.Player.CharacterData.CharacterId;
        try
        {
            await _pillRecipeService.GetRecipeDetailAsync(playerId, recipeId, cancellationToken);

            var validation = await _alchemyService.ValidateCraftPillAsync(
                playerId,
                recipeId,
                requestedCraftCount,
                selectedPlayerItemIds,
                selectedOptionalInputs,
                cancellationToken);
            var inventory = await _itemService.GetInventoryAsync(playerId, cancellationToken);
            var inventoryByPlayerItemId = inventory.ToDictionary(static item => item.PlayerItemId);

            return new PreviewCraftPillExecutionResult(
                true,
                validation.Success ? MessageCode.None : MessageCode.AlchemyInputInvalid,
                validation.FailureReason,
                _modelBuilder.BuildCraftPreviewModel(recipeId, validation, inventoryByPlayerItemId));
        }
        catch (GameException ex)
        {
            return new PreviewCraftPillExecutionResult(false, ex.Code, null, null);
        }
    }
}

public readonly record struct GetLearnedPillRecipesExecutionResult(
    bool Success,
    MessageCode Code,
    IReadOnlyList<LearnedPillRecipeModel>? Recipes);

public readonly record struct GetPillRecipeDetailExecutionResult(
    bool Success,
    MessageCode Code,
    PillRecipeDetailModel? Recipe);

public readonly record struct PreviewCraftPillExecutionResult(
    bool Success,
    MessageCode Code,
    string? FailureReason,
    AlchemyCraftPreviewModel? Preview);
