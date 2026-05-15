using GameServer.Config;
using GameServer.DTO;
using GameServer.Entities;
using GameServer.Exceptions;
using GameServer.Randomness;
using GameServer.Repositories;
using GameServer.Runtime;
using GameServer.World;
using GameShared.Messages;

namespace GameServer.Services;

public sealed class HerbService
{
    private readonly GameDb _db;
    private readonly GameConfigValues _gameConfig;
    private readonly MapCatalog _mapCatalog;
    private readonly AlchemyDefinitionCatalog _definitions;
    private readonly ItemDefinitionCatalog _itemDefinitions;
    private readonly PlayerCaveRepository _playerCaves;
    private readonly PlayerGardenPlotRepository _playerGardenPlots;
    private readonly PlayerSoilRepository _playerSoils;
    private readonly PlayerHerbRepository _playerHerbs;
    private readonly PlayerItemRepository _playerItems;
    private readonly ItemService _itemService;
    private readonly BagService _bagService;
    private readonly PlayerInventoryTransactionService _inventoryTransactions;
    private readonly IGameRandomService _randomService;

    public HerbService(
        GameDb db,
        GameConfigValues gameConfig,
        MapCatalog mapCatalog,
        AlchemyDefinitionCatalog definitions,
        ItemDefinitionCatalog itemDefinitions,
        PlayerCaveRepository playerCaves,
        PlayerGardenPlotRepository playerGardenPlots,
        PlayerSoilRepository playerSoils,
        PlayerHerbRepository playerHerbs,
        PlayerItemRepository playerItems,
        ItemService itemService,
        BagService bagService,
        PlayerInventoryTransactionService inventoryTransactions,
        IGameRandomService randomService)
    {
        _db = db;
        _gameConfig = gameConfig;
        _mapCatalog = mapCatalog;
        _definitions = definitions;
        _itemDefinitions = itemDefinitions;
        _playerCaves = playerCaves;
        _playerGardenPlots = playerGardenPlots;
        _playerSoils = playerSoils;
        _playerHerbs = playerHerbs;
        _playerItems = playerItems;
        _itemService = itemService;
        _bagService = bagService;
        _inventoryTransactions = inventoryTransactions;
        _randomService = randomService;
    }

    public async Task<PlayerCaveEntity> EnsureHomeCaveAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        var existing = await _playerCaves.GetHomeByOwnerAsync(playerId, cancellationToken);
        if (existing is not null)
            return existing;

        var homeDefinition = _mapCatalog.ResolveHomeDefinition();
        await using var tx = await _db.BeginTransactionAsync(cancellationToken);
        var cave = new PlayerCaveEntity
        {
            OwnerCharacterId = playerId,
            MapTemplateId = homeDefinition.MapId,
            ZoneIndex = homeDefinition.DefaultZoneIndex,
            IsHome = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        cave.Id = await _playerCaves.CreateAsync(cave, cancellationToken);
        for (var plotIndex = 1; plotIndex <= _gameConfig.CharacterHomeGardenPlotCount; plotIndex++)
        {
            await _playerGardenPlots.CreateAsync(new PlayerGardenPlotEntity
            {
                PlayerId = playerId,
                CaveId = cave.Id,
                PlotIndex = plotIndex,
                CurrentSoilPlayerItemId = null,
                CurrentPlayerHerbId = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        await tx.CommitAsync(cancellationToken);
        return cave;
    }

    public async Task<IReadOnlyList<PlayerGardenPlotEntity>> GetGardenPlotsAsync(Guid playerId, long caveId, CancellationToken cancellationToken = default)
    {
        var cave = await RequireOwnedCaveAsync(playerId, caveId, cancellationToken);
        return await _playerGardenPlots.ListByCaveIdAsync(cave.Id, cancellationToken);
    }

    public async Task<HerbRuntimeState?> GetGardenPlotHerbStateAsync(Guid playerId, long caveId, int plotIndex, CancellationToken cancellationToken = default)
    {
        var plot = await RequireOwnedPlotAsync(playerId, caveId, plotIndex, cancellationToken);
        return plot.CurrentPlayerHerbId.HasValue
            ? await GetHerbRuntimeStateAsync(plot.CurrentPlayerHerbId.Value, cancellationToken)
            : null;
    }

    public async Task<long> GetNextStageRemainingSecondsAsync(long playerHerbId, CancellationToken cancellationToken = default)
    {
        var herb = await _playerHerbs.GetByIdAsync(playerHerbId, cancellationToken)
                   ?? throw new GameException(MessageCode.GardenPlotNoHerb);
        herb = await MaterializeHerbProgressAsync(herb, cancellationToken);
        if (!_definitions.TryGetHerb(herb.HerbTemplateId, out var herbDefinition))
            throw new InvalidOperationException($"Herb template {herb.HerbTemplateId} was not found.");

        var nextStage = herbDefinition.GrowthStages
            .Where(x => x.RequiredGrowthSeconds > herb.AccumulatedGrowthSeconds)
            .OrderBy(x => x.RequiredGrowthSeconds)
            .FirstOrDefault();

        return nextStage is null
            ? 0
            : Math.Max(0L, nextStage.RequiredGrowthSeconds - herb.AccumulatedGrowthSeconds);
    }

    public async Task InsertSoilAsync(
        Guid playerId,
        long soilPlayerItemId,
        long caveId,
        int plotIndex,
        CancellationToken cancellationToken = default)
    {
        var plot = await RequireOwnedPlotAsync(playerId, caveId, plotIndex, cancellationToken);
        var soilItem = await _playerItems.GetByIdAsync(soilPlayerItemId, cancellationToken)
                       ?? throw new GameException(MessageCode.InventoryItemInvalid);
        if (soilItem.PlayerId != playerId)
            throw new GameException(MessageCode.InventoryItemInvalid);

        if (!_itemDefinitions.TryGetItem(soilItem.ItemTemplateId, out var itemDefinition) || itemDefinition.ItemType != ItemType.Soil)
            throw new GameException(MessageCode.InventoryItemInvalid);

        if (!_definitions.TryGetSoil(soilItem.ItemTemplateId, out _))
            throw new InvalidOperationException($"Soil template for item template {soilItem.ItemTemplateId} was not found.");

        var playerSoil = await _playerSoils.GetByPlayerItemIdAsync(soilPlayerItemId, cancellationToken)
                         ?? throw new GameException(MessageCode.InventoryItemInvalid);

        if (playerSoil.State == (int)PlayerSoilState.Inserted)
            throw new GameException(MessageCode.GardenPlotAlreadyHasSoil);

        if (plot.CurrentSoilPlayerItemId.HasValue)
        {
            var existingSoil = await _playerSoils.GetByPlayerItemIdAsync(plot.CurrentSoilPlayerItemId.Value, cancellationToken);
            if (existingSoil is not null && existingSoil.State == (int)PlayerSoilState.Depleted)
            {
                existingSoil.State = (int)PlayerSoilState.InInventory;
                existingSoil.InsertedPlotId = null;
                existingSoil.UpdatedAt = DateTime.UtcNow;
                await _playerSoils.UpdateAsync(existingSoil, cancellationToken);
                plot.CurrentSoilPlayerItemId = null;
            }
        }

        if (plot.CurrentSoilPlayerItemId.HasValue)
            throw new GameException(MessageCode.GardenPlotAlreadyHasSoil);

        playerSoil.State = (int)PlayerSoilState.Inserted;
        playerSoil.InsertedPlotId = plot.Id;
        playerSoil.UpdatedAt = DateTime.UtcNow;
        plot.CurrentSoilPlayerItemId = soilPlayerItemId;
        plot.UpdatedAt = DateTime.UtcNow;

        await _playerSoils.UpdateAsync(playerSoil, cancellationToken);
        await _playerGardenPlots.UpdateAsync(plot, cancellationToken);
    }

    public async Task<long> PlantSeedAsync(
        Guid playerId,
        long seedPlayerItemId,
        long caveId,
        int plotIndex,
        CancellationToken cancellationToken = default)
    {
        var plot = await RequireOwnedPlotAsync(playerId, caveId, plotIndex, cancellationToken);
        if (!plot.CurrentSoilPlayerItemId.HasValue)
            throw new GameException(MessageCode.GardenPlotNoSoil);
        if (plot.CurrentPlayerHerbId.HasValue)
            throw new GameException(MessageCode.GardenPlotAlreadyHasHerb);

        var seedItem = await _playerItems.GetByIdAsync(seedPlayerItemId, cancellationToken)
                      ?? throw new GameException(MessageCode.InventoryItemInvalid);
        if (seedItem.PlayerId != playerId)
            throw new GameException(MessageCode.InventoryItemInvalid);

        if (!_itemDefinitions.TryGetItem(seedItem.ItemTemplateId, out var seedDefinition) || seedDefinition.ItemType != ItemType.HerbSeed)
            throw new GameException(MessageCode.InventoryItemInvalid);

        if (!_definitions.TryGetHerbBySeedItemTemplate(seedItem.ItemTemplateId, out var herbDefinition))
            throw new InvalidOperationException($"No herb template is bound to seed item template {seedItem.ItemTemplateId}.");

        await using var tx = await _db.BeginTransactionAsync(cancellationToken);
        await ConsumeSpecificPlayerItemUnitAsync(seedItem, cancellationToken);

        var herb = new PlayerHerbEntity
        {
            PlayerId = playerId,
            HerbTemplateId = herbDefinition.Id,
            CurrentStage = (int)HerbGrowthStage.Seedling,
            PlantedAt = DateTime.UtcNow,
            AccumulatedGrowthSeconds = 0,
            State = (int)PlayerHerbState.Planting,
            CurrentPlotId = plot.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        herb.Id = await _playerHerbs.CreateAsync(herb, cancellationToken);

        plot.CurrentPlayerHerbId = herb.Id;
        plot.UpdatedAt = DateTime.UtcNow;
        await _playerGardenPlots.UpdateAsync(plot, cancellationToken);

        await tx.CommitAsync(cancellationToken);
        return herb.Id;
    }

    public async Task PlantExistingHerbAsync(
        Guid playerId,
        long playerHerbId,
        long caveId,
        int plotIndex,
        CancellationToken cancellationToken = default)
    {
        var plot = await RequireOwnedPlotAsync(playerId, caveId, plotIndex, cancellationToken);
        if (!plot.CurrentSoilPlayerItemId.HasValue)
            throw new GameException(MessageCode.GardenPlotNoSoil);
        if (plot.CurrentPlayerHerbId.HasValue)
            throw new GameException(MessageCode.GardenPlotAlreadyHasHerb);

        var herb = await _playerHerbs.GetByIdAsync(playerHerbId, cancellationToken)
                   ?? throw new GameException(MessageCode.GardenHerbNotOwned);
        if (herb.PlayerId != playerId)
            throw new GameException(MessageCode.GardenHerbNotOwned);
        if (herb.State != (int)PlayerHerbState.InInventory)
            throw new GameException(MessageCode.GardenHerbNotInInventory);

        herb.State = (int)PlayerHerbState.Planting;
        herb.CurrentPlotId = plot.Id;
        herb.PlantedAt = DateTime.UtcNow;
        herb.UpdatedAt = DateTime.UtcNow;

        plot.CurrentPlayerHerbId = herb.Id;
        plot.UpdatedAt = DateTime.UtcNow;

        await _playerHerbs.UpdateAsync(herb, cancellationToken);
        await _playerGardenPlots.UpdateAsync(plot, cancellationToken);
    }

    public async Task<DateTime> HarvestAsync(Guid playerId, long playerHerbId, CancellationToken cancellationToken = default)
    {
        var herb = await RequireOwnedHerbAsync(playerId, playerHerbId, cancellationToken);
        if (herb.State != (int)PlayerHerbState.Planting)
            throw new GameException(MessageCode.GardenPlotNoHerb);

        herb = await MaterializeHerbProgressAsync(herb, cancellationToken);
        var currentStage = (HerbGrowthStage)herb.CurrentStage;
        if (currentStage is not HerbGrowthStage.Mature and not HerbGrowthStage.ThousandYear)
            throw new GameException(MessageCode.GardenHerbNotHarvestable);

        var expireAt = DateTime.UtcNow.Add(_gameConfig.HerbInventoryExpiry);

        await using var tx = await _db.BeginTransactionAsync(cancellationToken);
        var plot = herb.CurrentPlotId.HasValue
            ? await _playerGardenPlots.GetByIdAsync(herb.CurrentPlotId.Value, cancellationToken)
            : null;

        if (plot is not null)
        {
            plot.CurrentPlayerHerbId = null;
            plot.UpdatedAt = DateTime.UtcNow;
            await _playerGardenPlots.UpdateAsync(plot, cancellationToken);
        }

        herb.State = (int)PlayerHerbState.InInventory;
        herb.CurrentPlotId = null;
        herb.PlantedAt = null;
        herb.ExpireAt = expireAt;
        herb.UpdatedAt = DateTime.UtcNow;
        await _playerHerbs.UpdateAsync(herb, cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return expireAt;
    }

    public async Task<HerbRuntimeState> GetHerbRuntimeStateAsync(long playerHerbId, CancellationToken cancellationToken = default)
    {
        var herb = await _playerHerbs.GetByIdAsync(playerHerbId, cancellationToken)
                   ?? throw new InvalidOperationException($"Player herb {playerHerbId} was not found.");
        herb = await MaterializeHerbProgressAsync(herb, cancellationToken);
        return await BuildRuntimeStateAsync(herb, cancellationToken);
    }

    public async Task<HerbExtractionResult> ExtractHerbAsync(
        Guid playerId,
        long playerHerbId,
        CancellationToken cancellationToken = default)
    {
        var herb = await RequireOwnedHerbAsync(playerId, playerHerbId, cancellationToken);
        if (herb.State != (int)PlayerHerbState.InInventory)
            throw new GameException(MessageCode.GardenHerbNotInInventory);

        if (IsHerbExpired(herb, DateTime.UtcNow))
        {
            await _playerHerbs.DeleteAsync(herb.Id, cancellationToken);
            throw new GameException(MessageCode.GardenHerbExpired);
        }

        if (!_definitions.TryGetHerb(herb.HerbTemplateId, out var herbDefinition))
            throw new InvalidOperationException($"Herb template {herb.HerbTemplateId} was not found.");

        var outputs = ResolveHarvestOutputs(herbDefinition, (HerbGrowthStage)herb.CurrentStage);
        if (outputs.Count == 0)
            throw new InvalidOperationException($"Herb template {herb.HerbTemplateId} does not have harvest output for stage {(HerbGrowthStage)herb.CurrentStage}.");

        var grants = outputs
            .Where(output => _randomService.CheckChance(ToPartsPerMillion(output.OutputChance)).Success)
            .Select(output => new ItemGrantRequest(output.ResultItemTemplateId, output.ResultQuantity, false, null))
            .ToList();

        var mamNonReturned = false;
        if (herbDefinition.ReplantItemTemplateId.HasValue)
        {
            mamNonReturned = _randomService.CheckChance(ToPartsPerMillion(herbDefinition.ReplantReturnChance)).Success;
            if (mamNonReturned)
                grants.Add(new ItemGrantRequest(herbDefinition.ReplantItemTemplateId.Value, 1, false, null));
        }

        var created = new List<PlayerItemEntity>();
        await _inventoryTransactions.ExecuteAsync(
            playerId,
            async ct =>
            {
                var lockedHerb = await RequireOwnedHerbAsync(playerId, playerHerbId, ct);
                if (lockedHerb.State != (int)PlayerHerbState.InInventory)
                    throw new GameException(MessageCode.GardenHerbNotInInventory);

                if (IsHerbExpired(lockedHerb, DateTime.UtcNow))
                {
                    await _playerHerbs.DeleteAsync(lockedHerb.Id, ct);
                    throw new GameException(MessageCode.GardenHerbExpired);
                }

                if (grants.Count > 0)
                {
                    var capacityCheck = await _bagService.CheckCapacityForAsync(playerId, grants, ct);
                    if (!capacityCheck.CanFit)
                        throw new GameException(MessageCode.GardenInventoryFull);
                }

                foreach (var grant in grants)
                {
                    var createdItems = await _itemService.AddItemAsync(
                        playerId,
                        grant.ItemTemplateId,
                        grant.Quantity,
                        grant.IsBound,
                        grant.ExpireAtUtc,
                        ct);
                    created.AddRange(createdItems);
                }

                await _playerHerbs.DeleteAsync(lockedHerb.Id, ct);
            },
            cancellationToken);

        var inventory = await _itemService.GetInventoryAsync(playerId, cancellationToken);
        var createdViews = inventory.Where(x => created.Any(createdItem => createdItem.Id == x.PlayerItemId)).ToArray();
        return new HerbExtractionResult(createdViews, mamNonReturned);
    }

    private async Task<PlayerCaveEntity> RequireOwnedCaveAsync(Guid playerId, long caveId, CancellationToken cancellationToken)
    {
        var cave = await _playerCaves.GetByIdAsync(caveId, cancellationToken)
                   ?? throw new GameException(MessageCode.GardenCaveNotFound);
        if (cave.OwnerCharacterId != playerId)
            throw new GameException(MessageCode.GardenPlotNotOwned);

        return cave;
    }

    private async Task<PlayerGardenPlotEntity> RequireOwnedPlotAsync(Guid playerId, long caveId, int plotIndex, CancellationToken cancellationToken)
    {
        await RequireOwnedCaveAsync(playerId, caveId, cancellationToken);
        return await _playerGardenPlots.GetByCaveAndPlotIndexAsync(caveId, plotIndex, cancellationToken)
               ?? throw new GameException(MessageCode.GardenPlotNotFound);
    }

    private async Task<PlayerHerbEntity> RequireOwnedHerbAsync(Guid playerId, long playerHerbId, CancellationToken cancellationToken)
    {
        var herb = await _playerHerbs.GetByIdAsync(playerHerbId, cancellationToken)
                   ?? throw new GameException(MessageCode.GardenHerbNotOwned);
        if (herb.PlayerId != playerId)
            throw new GameException(MessageCode.GardenHerbNotOwned);

        return herb;
    }

    private async Task<PlayerHerbEntity> MaterializeHerbProgressAsync(PlayerHerbEntity herb, CancellationToken cancellationToken)
    {
        if (!_definitions.TryGetHerb(herb.HerbTemplateId, out var herbDefinition))
            throw new InvalidOperationException($"Herb template {herb.HerbTemplateId} was not found.");

        if (herb.State != (int)PlayerHerbState.Planting || !herb.CurrentPlotId.HasValue)
        {
            ApplyStageProgress(herb, herbDefinition, herb.AccumulatedGrowthSeconds);
            return herb;
        }

        var plot = await _playerGardenPlots.GetByIdAsync(herb.CurrentPlotId.Value, cancellationToken);
        var now = DateTime.UtcNow;
        var elapsedSeconds = herb.PlantedAt.HasValue
            ? Math.Max(0L, (long)(now - herb.PlantedAt.Value).TotalSeconds)
            : 0L;

        var rawGrowthSeconds = 0L;
        if (plot?.CurrentSoilPlayerItemId is { } soilPlayerItemId)
        {
            var playerSoil = await _playerSoils.GetByPlayerItemIdAsync(soilPlayerItemId, cancellationToken);
            if (playerSoil is not null &&
                playerSoil.State == (int)PlayerSoilState.Inserted &&
                _definitions.TryGetSoil((await _playerItems.GetByIdAsync(soilPlayerItemId, cancellationToken))!.ItemTemplateId, out var soilDefinition))
            {
                var remainingSeconds = Math.Max(0L, soilDefinition.MaxActiveSeconds - playerSoil.TotalUsedSeconds);
                rawGrowthSeconds = Math.Min(elapsedSeconds, remainingSeconds);
                playerSoil.TotalUsedSeconds += rawGrowthSeconds;
                if (playerSoil.TotalUsedSeconds >= soilDefinition.MaxActiveSeconds)
                    playerSoil.State = (int)PlayerSoilState.Depleted;

                playerSoil.UpdatedAt = now;
                await _playerSoils.UpdateAsync(playerSoil, cancellationToken);

                var effectiveGrowthSeconds = decimal.ToInt64(decimal.Truncate(rawGrowthSeconds * soilDefinition.GrowthSpeedRate));
                herb.AccumulatedGrowthSeconds += effectiveGrowthSeconds;
            }
        }

        herb.PlantedAt = now;
        herb.UpdatedAt = now;
        ApplyStageProgress(herb, herbDefinition, herb.AccumulatedGrowthSeconds);
        await _playerHerbs.UpdateAsync(herb, cancellationToken);
        return herb;
    }

    private async Task<HerbRuntimeState> BuildRuntimeStateAsync(PlayerHerbEntity herb, CancellationToken cancellationToken)
    {
        long? soilPlayerItemId = null;
        long soilRemainingSeconds = 0;
        if (herb.CurrentPlotId.HasValue)
        {
            var plot = await _playerGardenPlots.GetByIdAsync(herb.CurrentPlotId.Value, cancellationToken);
            if (plot?.CurrentSoilPlayerItemId is { } currentSoilPlayerItemId)
            {
                soilPlayerItemId = currentSoilPlayerItemId;
                var playerSoil = await _playerSoils.GetByPlayerItemIdAsync(currentSoilPlayerItemId, cancellationToken);
                var soilItem = await _playerItems.GetByIdAsync(currentSoilPlayerItemId, cancellationToken);
                if (playerSoil is not null &&
                    soilItem is not null &&
                    _definitions.TryGetSoil(soilItem.ItemTemplateId, out var soilDefinition))
                {
                    soilRemainingSeconds = Math.Max(0L, soilDefinition.MaxActiveSeconds - playerSoil.TotalUsedSeconds);
                }
            }
        }

        return new HerbRuntimeState(
            herb.Id,
            herb.HerbTemplateId,
            (HerbGrowthStage)herb.CurrentStage,
            herb.AccumulatedGrowthSeconds,
            herb.State == (int)PlayerHerbState.Planting && herb.CurrentPlotId.HasValue && soilRemainingSeconds > 0,
            herb.CurrentPlotId,
            soilPlayerItemId,
            soilRemainingSeconds,
            herb.ExpireAt);
    }

    private async Task ConsumeSpecificPlayerItemUnitAsync(PlayerItemEntity playerItem, CancellationToken cancellationToken)
    {
        if (playerItem.Quantity <= 0)
            throw new InvalidOperationException($"Player item {playerItem.Id} has invalid quantity {playerItem.Quantity}.");

        if (playerItem.Quantity == 1)
        {
            if (!playerItem.PlayerId.HasValue)
                throw new InvalidOperationException($"Player item {playerItem.Id} does not have an owner.");

            await _itemService.RemovePlayerItemAsync(playerItem.PlayerId.Value, playerItem.Id, cancellationToken);
            return;
        }

        playerItem.Quantity -= 1;
        playerItem.UpdatedAt = DateTime.UtcNow;
        await _playerItems.UpdateAsync(playerItem, cancellationToken);
    }

    private static IReadOnlyList<HerbHarvestOutputDefinition> ResolveHarvestOutputs(HerbTemplateDefinition herbDefinition, HerbGrowthStage currentStage)
    {
        var exact = herbDefinition.HarvestOutputs.Where(x => x.RequiredStage == currentStage).ToArray();
        if (exact.Length > 0)
            return exact;

        var fallbackStage = herbDefinition.HarvestOutputs
            .Where(x => x.RequiredStage <= currentStage)
            .OrderByDescending(x => x.RequiredStage)
            .Select(x => x.RequiredStage)
            .FirstOrDefault();

        return fallbackStage == default
            ? Array.Empty<HerbHarvestOutputDefinition>()
            : herbDefinition.HarvestOutputs.Where(x => x.RequiredStage == fallbackStage).ToArray();
    }

    private static void ApplyStageProgress(PlayerHerbEntity herb, HerbTemplateDefinition herbDefinition, long accumulatedGrowthSeconds)
    {
        var stage = herbDefinition.GrowthStages
            .Where(x => accumulatedGrowthSeconds >= x.RequiredGrowthSeconds)
            .OrderByDescending(x => x.RequiredGrowthSeconds)
            .FirstOrDefault();

        if (stage is null)
        {
            herb.CurrentStage = (int)HerbGrowthStage.Seedling;
            return;
        }

        herb.CurrentStage = (int)stage.Stage;
    }

    private static bool IsHerbExpired(PlayerHerbEntity herb, DateTime utcNow)
    {
        return herb.ExpireAt.HasValue && herb.ExpireAt.Value <= utcNow;
    }

    private static int ToPartsPerMillion(double rawRate)
    {
        var normalized = rawRate <= 1d ? rawRate : rawRate / 100d;
        normalized = Math.Clamp(normalized, 0d, 1d);
        return (int)Math.Round(normalized * 1_000_000d, MidpointRounding.AwayFromZero);
    }
}

public sealed record HerbExtractionResult(
    IReadOnlyList<InventoryItemView> Items,
    bool MamNonReturned);
