using GameServer.Descriptions;
using GameServer.DTO;
using GameServer.Entities;
using GameServer.Exceptions;
using GameServer.Repositories;
using GameServer.Runtime;
using GameShared.Messages;

namespace GameServer.Services;

public sealed class MartialArtService
{
    private readonly ItemService _itemService;
    private readonly ItemDefinitionCatalog _itemDefinitions;
    private readonly CombatDefinitionCatalog _combatDefinitions;
    private readonly PlayerItemRepository _playerItems;
    private readonly PlayerMartialArtRepository _playerMartialArts;
    private readonly CharacterService _characterService;
    private readonly GameplayDescriptionService _descriptions;

    public MartialArtService(
        ItemService itemService,
        ItemDefinitionCatalog itemDefinitions,
        CombatDefinitionCatalog combatDefinitions,
        PlayerItemRepository playerItems,
        PlayerMartialArtRepository playerMartialArts,
        CharacterService characterService,
        GameplayDescriptionService descriptions)
    {
        _itemService = itemService;
        _itemDefinitions = itemDefinitions;
        _combatDefinitions = combatDefinitions;
        _playerItems = playerItems;
        _playerMartialArts = playerMartialArts;
        _characterService = characterService;
        _descriptions = descriptions;
    }

    public async Task<IReadOnlyList<PlayerMartialArtDto>> GetOwnedMartialArtsAsync(Guid playerId, int? activeMartialArtId, CancellationToken cancellationToken = default)
    {
        var owned = await _playerMartialArts.ListByPlayerIdAsync(playerId, cancellationToken);
        return BuildOwnedMartialArts(owned, activeMartialArtId);
    }

    public async Task<UseMartialArtBookResult> UseMartialArtBookAsync(Guid playerId, long playerItemId, CancellationToken cancellationToken = default)
    {
        var playerItem = await _playerItems.GetByIdAsync(playerItemId, cancellationToken);
        if (playerItem is null || playerItem.PlayerId != playerId)
            throw new GameException(MessageCode.MartialArtBookItemInvalid);

        if (!_itemDefinitions.TryGetItem(playerItem.ItemTemplateId, out var itemDefinition) ||
            itemDefinition.MartialArtBook is null)
        {
            throw new GameException(MessageCode.MartialArtBookItemInvalid);
        }

        var martialArtId = itemDefinition.MartialArtBook.MartialArtId;
        if (!_combatDefinitions.TryGetMartialArt(martialArtId, out var martialArtDefinition))
            throw new InvalidOperationException($"Martial art {martialArtId} referenced by book item template {itemDefinition.Id} was not found.");

        var existing = await _playerMartialArts.GetByPlayerAndMartialArtAsync(playerId, martialArtId, cancellationToken);
        if (existing is not null)
            throw new GameException(MessageCode.MartialArtAlreadyLearned);

        var learned = new PlayerMartialArtEntity
        {
            PlayerId = playerId,
            MartialArtId = martialArtId,
            CurrentStage = 1,
            CurrentExp = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        learned.Id = await _playerMartialArts.CreateAsync(learned, cancellationToken);
        await _itemService.ConsumePlayerItemAsync(playerId, playerItemId, 1, cancellationToken);

        var baseStats = await _characterService.InitializeCharacterBaseStatsAsync(playerId, cancellationToken);
        return new UseMartialArtBookResult(
            baseStats,
            new PlayerMartialArtDto(
                martialArtDefinition.Id,
                martialArtDefinition.Code,
                martialArtDefinition.Name,
                martialArtDefinition.Icon,
                martialArtDefinition.Quality,
                martialArtDefinition.Category,
                _descriptions.BuildMartialArtDescription(martialArtDefinition),
                learned.CurrentStage,
                learned.CurrentExp,
                martialArtDefinition.MaxStage,
                martialArtDefinition.QiAbsorptionRate,
                baseStats.ActiveMartialArtId == martialArtDefinition.Id));
    }

    public async Task<CharacterBaseStatsDto> SetActiveMartialArtAsync(Guid playerId, int martialArtId, CancellationToken cancellationToken = default)
    {
        var baseStats = await _characterService.InitializeCharacterBaseStatsAsync(playerId, cancellationToken);
        if (martialArtId <= 0)
        {
            var cleared = baseStats with
            {
                ActiveMartialArtId = null
            };

            return await _characterService.UpdateCharacterBaseStatsAsync(cleared, cancellationToken);
        }

        if (!_combatDefinitions.TryGetMartialArt(martialArtId, out _))
            throw new GameException(MessageCode.ActiveMartialArtInvalid);

        var owned = await _playerMartialArts.GetByPlayerAndMartialArtAsync(playerId, martialArtId, cancellationToken);
        if (owned is null)
            throw new GameException(MessageCode.MartialArtNotLearned);

        var updated = baseStats with
        {
            ActiveMartialArtId = martialArtId
        };
        return await _characterService.UpdateCharacterBaseStatsAsync(updated, cancellationToken);
    }

    private IReadOnlyList<PlayerMartialArtDto> BuildOwnedMartialArts(
        IReadOnlyList<PlayerMartialArtEntity> owned,
        int? activeMartialArtId)
    {
        return owned
            .OrderBy(x => x.MartialArtId)
            .Select(progress =>
            {
                if (!_combatDefinitions.TryGetMartialArt(progress.MartialArtId, out var definition))
                    throw new InvalidOperationException($"Player martial art {progress.MartialArtId} was not found in combat definitions.");

                return new PlayerMartialArtDto(
                    definition.Id,
                    definition.Code,
                    definition.Name,
                    definition.Icon,
                    definition.Quality,
                    definition.Category,
                    _descriptions.BuildMartialArtDescription(definition),
                    progress.CurrentStage,
                    progress.CurrentExp,
                    definition.MaxStage,
                    definition.QiAbsorptionRate,
                    activeMartialArtId == definition.Id);
            })
            .ToArray();
    }
}

public readonly record struct UseMartialArtBookResult(
    CharacterBaseStatsDto BaseStats,
    PlayerMartialArtDto LearnedMartialArt);
