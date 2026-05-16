using GameServer.Exceptions;
using GameServer.Repositories;
using GameShared.Messages;

namespace GameServer.Services;

public sealed class PermanentCharacterDeletionService
{
    private readonly CharacterService _characterService;
    private readonly PlayerInventoryTransactionService _inventoryTransactions;
    private readonly BreakthroughAttemptRepository _breakthroughAttempts;
    private readonly CharacterBaseStatRepository _baseStats;
    private readonly CharacterCurrentStateRepository _currentStates;
    private readonly CharacterRepository _characters;
    private readonly PlayerMartialArtRepository _playerMartialArts;
    private readonly PlayerSkillGrantSourceRepository _playerSkillGrantSources;
    private readonly PlayerSkillLoadoutRepository _playerSkillLoadouts;
    private readonly PlayerSkillRepository _playerSkills;
    private readonly PlayerPillRecipeRepository _playerPillRecipes;
    private readonly PlayerPracticeSessionRepository _playerPracticeSessions;
    private readonly PlayerNotificationRepository _playerNotifications;
    private readonly PlayerHerbRepository _playerHerbs;
    private readonly PlayerGardenPlotRepository _playerGardenPlots;
    private readonly PlayerCaveRepository _playerCaves;
    private readonly ItemService _itemService;

    public PermanentCharacterDeletionService(
        CharacterService characterService,
        PlayerInventoryTransactionService inventoryTransactions,
        BreakthroughAttemptRepository breakthroughAttempts,
        CharacterBaseStatRepository baseStats,
        CharacterCurrentStateRepository currentStates,
        CharacterRepository characters,
        PlayerMartialArtRepository playerMartialArts,
        PlayerSkillGrantSourceRepository playerSkillGrantSources,
        PlayerSkillLoadoutRepository playerSkillLoadouts,
        PlayerSkillRepository playerSkills,
        PlayerPillRecipeRepository playerPillRecipes,
        PlayerPracticeSessionRepository playerPracticeSessions,
        PlayerNotificationRepository playerNotifications,
        PlayerHerbRepository playerHerbs,
        PlayerGardenPlotRepository playerGardenPlots,
        PlayerCaveRepository playerCaves,
        ItemService itemService)
    {
        _characterService = characterService;
        _inventoryTransactions = inventoryTransactions;
        _breakthroughAttempts = breakthroughAttempts;
        _baseStats = baseStats;
        _currentStates = currentStates;
        _characters = characters;
        _playerMartialArts = playerMartialArts;
        _playerSkillGrantSources = playerSkillGrantSources;
        _playerSkillLoadouts = playerSkillLoadouts;
        _playerSkills = playerSkills;
        _playerPillRecipes = playerPillRecipes;
        _playerPracticeSessions = playerPracticeSessions;
        _playerNotifications = playerNotifications;
        _playerHerbs = playerHerbs;
        _playerGardenPlots = playerGardenPlots;
        _playerCaves = playerCaves;
        _itemService = itemService;
    }

    public async Task<PermanentCharacterDeletionResult> ConfirmAsync(
        Guid accountId,
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _characterService.LoadCharacterSnapshotByAccountAsync(accountId, characterId, cancellationToken);
        if (snapshot is null)
            return PermanentCharacterDeletionResult.Failed(MessageCode.CharacterNotFound, characterId);

        if (!_characterService.IsPendingPermanentDeletion(snapshot))
            return PermanentCharacterDeletionResult.Failed(MessageCode.CharacterPendingPermanentDeletion, characterId);

        await _inventoryTransactions.ExecuteAsync(
            characterId,
            async ct =>
            {
                await _itemService.PurgeAllItemsForPlayerAsync(characterId, ct);
                await _playerSkillGrantSources.DeleteByPlayerIdAsync(characterId, ct);
                await _playerSkillLoadouts.DeleteByPlayerIdAsync(characterId, ct);
                await _playerSkills.DeleteByPlayerIdAsync(characterId, ct);
                await _playerMartialArts.DeleteByPlayerIdAsync(characterId, ct);
                await _playerPillRecipes.DeleteByPlayerIdAsync(characterId, ct);
                await _playerPracticeSessions.DeleteByPlayerIdAsync(characterId, ct);
                await _playerNotifications.DeleteByPlayerIdAsync(characterId, ct);
                await _playerHerbs.DeleteByPlayerIdAsync(characterId, ct);
                await _playerGardenPlots.DeleteByPlayerIdAsync(characterId, ct);
                await _playerCaves.DeleteByOwnerAsync(characterId, ct);
                await _breakthroughAttempts.DeleteByCharacterIdAsync(characterId, ct);
                await _currentStates.DeleteAsync(characterId, ct);
                await _baseStats.DeleteAsync(characterId, ct);
                await _characters.DeleteAsync(characterId, ct);
            },
            cancellationToken);

        return PermanentCharacterDeletionResult.Succeeded(characterId);
    }
}
