using GameServer.DTO;
using GameServer.Exceptions;
using GameServer.Repositories;
using GameServer.Runtime;
using GameServer.World;
using GameShared.Messages;

namespace GameServer.Services;

public sealed class ItemUseService
{
    private readonly PlayerItemRepository _playerItems;
    private readonly ItemDefinitionCatalog _itemDefinitions;
    private readonly AlchemyDefinitionCatalog _alchemyDefinitions;
    private readonly ItemService _itemService;
    private readonly EquipmentService _equipmentService;
    private readonly CharacterFinalStatService _characterFinalStatService;
    private readonly MartialArtService _martialArtService;
    private readonly PillRecipeService _pillRecipeService;
    private readonly CharacterRuntimeService _characterRuntimeService;
    private readonly CharacterService _characterService;
    private readonly CharacterCultivationService _cultivationService;
    private readonly CharacterRuntimeNotifier _notifier;

    public ItemUseService(
        PlayerItemRepository playerItems,
        ItemDefinitionCatalog itemDefinitions,
        AlchemyDefinitionCatalog alchemyDefinitions,
        ItemService itemService,
        EquipmentService equipmentService,
        CharacterFinalStatService characterFinalStatService,
        MartialArtService martialArtService,
        PillRecipeService pillRecipeService,
        CharacterRuntimeService characterRuntimeService,
        CharacterService characterService,
        CharacterCultivationService cultivationService,
        CharacterRuntimeNotifier notifier)
    {
        _playerItems = playerItems;
        _itemDefinitions = itemDefinitions;
        _alchemyDefinitions = alchemyDefinitions;
        _itemService = itemService;
        _equipmentService = equipmentService;
        _characterFinalStatService = characterFinalStatService;
        _martialArtService = martialArtService;
        _pillRecipeService = pillRecipeService;
        _characterRuntimeService = characterRuntimeService;
        _characterService = characterService;
        _cultivationService = cultivationService;
        _notifier = notifier;
    }

    public async Task<UseItemExecutionResult> UseAsync(
        PlayerSession player,
        long playerItemId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            throw new GameException(MessageCode.InventoryItemQuantityInvalid);

        var playerId = player.CharacterData.CharacterId;
        var playerItem = await _playerItems.GetByIdAsync(playerItemId, cancellationToken);
        if (playerItem is null ||
            playerItem.PlayerId != playerId ||
            playerItem.LocationType != (int)ItemLocationType.Inventory ||
            IsExpired(playerItem.ExpireAt))
        {
            throw new GameException(MessageCode.InventoryItemInvalid);
        }

        if (playerItem.Quantity < quantity)
            throw new GameException(MessageCode.InventoryItemQuantityInvalid);

        if (!_itemDefinitions.TryGetItem(playerItem.ItemTemplateId, out var itemDefinition))
            throw new GameException(MessageCode.InventoryItemInvalid);

        return itemDefinition.ItemType switch
        {
            ItemType.Equipment => await UseEquipmentAsync(player, playerItemId, quantity, itemDefinition, cancellationToken),
            ItemType.MartialArtBook => await UseMartialArtBookAsync(player, playerItemId, quantity, cancellationToken),
            ItemType.PillRecipeBook => await UsePillRecipeBookAsync(playerId, playerItemId, quantity, cancellationToken),
            ItemType.Consumable => await UseConsumableAsync(player, playerItemId, quantity, itemDefinition, cancellationToken),
            _ => throw new GameException(MessageCode.ItemUseUnsupported)
        };
    }

    private async Task<UseItemExecutionResult> UseEquipmentAsync(
        PlayerSession player,
        long playerItemId,
        int quantity,
        ItemDefinition itemDefinition,
        CancellationToken cancellationToken)
    {
        EnsureSingleQuantity(quantity);
        var equipmentDefinition = itemDefinition.Equipment
            ?? throw new GameException(MessageCode.InventoryItemInvalid);

        await _equipmentService.EquipItemAsync(
            player.CharacterData.CharacterId,
            playerItemId,
            equipmentDefinition.SlotType,
            cancellationToken);

        var runtimeSnapshot = await _characterFinalStatService.ApplyAuthoritativeFinalStatsAsync(player, cancellationToken);
        var items = await _itemService.GetInventoryAsync(player.CharacterData.CharacterId, cancellationToken);
        return new UseItemExecutionResult(
            items,
            runtimeSnapshot.BaseStats,
            runtimeSnapshot.CurrentState,
            null,
            null,
            quantity,
            1);
    }

    private async Task<UseItemExecutionResult> UseMartialArtBookAsync(
        PlayerSession player,
        long playerItemId,
        int quantity,
        CancellationToken cancellationToken)
    {
        EnsureSingleQuantity(quantity);

        var result = await _martialArtService.UseMartialArtBookAsync(
            player.CharacterData.CharacterId,
            playerItemId,
            cancellationToken);

        player.RuntimeState.UpdateBaseStats(_ => result.BaseStats);
        _notifier.NotifyBaseStatsChanged(player, result.BaseStats);

        var cultivationPreview = await _cultivationService.BuildCultivationPreviewAsync(result.BaseStats);
        var items = await _itemService.GetInventoryAsync(player.CharacterData.CharacterId, cancellationToken);
        return new UseItemExecutionResult(
            items,
            result.BaseStats,
            null,
            result.LearnedMartialArt,
            cultivationPreview,
            quantity,
            1);
    }

    private async Task<UseItemExecutionResult> UsePillRecipeBookAsync(
        Guid playerId,
        long playerItemId,
        int quantity,
        CancellationToken cancellationToken)
    {
        EnsureSingleQuantity(quantity);

        await _pillRecipeService.LearnRecipeAsync(playerId, playerItemId, cancellationToken);
        var items = await _itemService.GetInventoryAsync(playerId, cancellationToken);
        return new UseItemExecutionResult(
            items,
            null,
            null,
            null,
            null,
            quantity,
            1);
    }

    private async Task<UseItemExecutionResult> UseConsumableAsync(
        PlayerSession player,
        long playerItemId,
        int quantity,
        ItemDefinition itemDefinition,
        CancellationToken cancellationToken)
    {
        if (!_alchemyDefinitions.TryGetPillTemplate(itemDefinition.Id, out var pillDefinition) ||
            pillDefinition.UsageType != PillUsageType.ConsumeDirectly)
        {
            throw new GameException(MessageCode.ItemUseUnsupported);
        }

        var currentSnapshot = player.RuntimeState.CaptureSnapshot();
        var hpDelta = 0;
        var mpDelta = 0;
        var staminaDelta = 0;

        foreach (var effect in pillDefinition.Effects.OrderBy(x => x.OrderIndex))
        {
            switch (effect.EffectType)
            {
                case PillEffectType.RecoverHp:
                    hpDelta = checked(hpDelta + ResolveResourceDelta(effect, currentSnapshot.BaseStats.GetEffectiveHp(), quantity));
                    break;
                case PillEffectType.RecoverMp:
                    mpDelta = checked(mpDelta + ResolveResourceDelta(effect, currentSnapshot.BaseStats.GetEffectiveMp(), quantity));
                    break;
                default:
                    throw new GameException(MessageCode.ItemUseUnsupported);
            }
        }

        await _itemService.ConsumePlayerItemAsync(player.CharacterData.CharacterId, playerItemId, quantity, cancellationToken);
        var updatedSnapshot = _characterRuntimeService.ApplyResourceDelta(player, hpDelta, mpDelta, staminaDelta);
        await _characterService.UpdateCharacterCurrentStateAsync(updatedSnapshot.CurrentState, cancellationToken);

        var items = await _itemService.GetInventoryAsync(player.CharacterData.CharacterId, cancellationToken);
        return new UseItemExecutionResult(
            items,
            updatedSnapshot.BaseStats,
            updatedSnapshot.CurrentState,
            null,
            null,
            quantity,
            quantity);
    }

    private static int ResolveResourceDelta(PillEffectDefinition effect, int maxResource, int quantity)
    {
        decimal totalPerUse = 0m;

        if (effect.BaseValue.HasValue)
        {
            if (effect.ValueType == CombatValueType.Percent || effect.ValueType == CombatValueType.Ratio)
                totalPerUse += maxResource * NormalizeRatio(effect.BaseValue.Value);
            else
                totalPerUse += effect.BaseValue.Value;
        }

        if (effect.RatioValue.HasValue)
            totalPerUse += maxResource * NormalizeRatio(effect.RatioValue.Value);

        var total = totalPerUse * quantity;
        return decimal.ToInt32(decimal.Truncate(total));
    }

    private static decimal NormalizeRatio(decimal rawValue)
    {
        if (rawValue <= 0m)
            return 0m;

        var normalized = rawValue > 1m ? rawValue / 100m : rawValue;
        return Math.Clamp(normalized, 0m, 1m);
    }

    private static void EnsureSingleQuantity(int quantity)
    {
        if (quantity != 1)
            throw new GameException(MessageCode.InventoryItemQuantityInvalid);
    }

    private static bool IsExpired(DateTime? expireAtUtc)
    {
        return expireAtUtc.HasValue && expireAtUtc.Value <= DateTime.UtcNow;
    }
}

public readonly record struct UseItemExecutionResult(
    IReadOnlyList<InventoryItemView> Items,
    CharacterBaseStatsDto? BaseStats,
    CharacterCurrentStateDto? CurrentState,
    PlayerMartialArtDto? LearnedMartialArt,
    CultivationPreviewDto? CultivationPreview,
    int RequestedQuantity,
    int AppliedQuantity);
