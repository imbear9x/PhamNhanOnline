using GameServer.Entities;
using GameServer.Randomness;
using GameServer.Repositories;
using GameServer.Runtime;
using GameServer.World;

namespace GameServer.Services;

public sealed class HerbService
{
    private const int DefaultHomeGardenPlotCount = 8;

    private readonly GameDb _db;
    private readonly MapCatalog _mapCatalog;
    private readonly AlchemyDefinitionCatalog _definitions;
    private readonly ItemDefinitionCatalog _itemDefinitions;
    private readonly PlayerCaveRepository _playerCaves;
    private readonly PlayerGardenPlotRepository _playerGardenPlots;
    private readonly PlayerSoilRepository _playerSoils;
    private readonly PlayerHerbRepository _playerHerbs;
    private readonly PlayerItemRepository _playerItems;
    private readonly ItemService _itemService;
    private readonly IGameRandomService _randomService;

    public HerbService(
        GameDb db,
        MapCatalog mapCatalog,
        AlchemyDefinitionCatalog definitions,
        ItemDefinitionCatalog itemDefinitions,
        PlayerCaveRepository playerCaves,
        PlayerGardenPlotRepository playerGardenPlots,
        PlayerSoilRepository playerSoils,
        PlayerHerbRepository playerHerbs,
        PlayerItemRepository playerItems,
        ItemService itemService,
        IGameRandomService randomService)
    {
        _db = db;
        _mapCatalog = mapCatalog;
        _definitions = definitions;
        _itemDefinitions = itemDefinitions;
        _playerCaves = playerCaves;
        _playerGardenPlots = playerGardenPlots;
        _playerSoils = playerSoils;
        _playerHerbs = playerHerbs;
        _playerItems = playerItems;
        _itemService = itemService;
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
        for (var plotIndex = 1; plotIndex <= DefaultHomeGardenPlotCount; plotIndex++)
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

    public async Task InsertSoilAsync(
        Guid playerId,
        long soilPlayerItemId,
        long caveId,
        int plotIndex,
        CancellationToken cancellationToken = default)
    {
        var plot = await RequireOwnedPlotAsync(playerId, caveId, plotIndex, cancellationToken);
        var soilItem = await _playerItems.GetByIdAsync(soilPlayerItemId, cancellationToken)
                       ?? throw new InvalidOperationException($"Soil player item {soilPlayerItemId} was not found.");
        if (soilItem.PlayerId != playerId)
            throw new InvalidOperationException($"Soil player item {soilPlayerItemId} does not belong to player {playerId}.");

        if (!_itemDefinitions.TryGetItem(soilItem.ItemTemplateId, out var itemDefinition) || itemDefinition.ItemType != ItemType.Soil)
            throw new InvalidOperationException($"Player item {soilPlayerItemId} is not a soil item.");

        if (!_definitions.TryGetSoil(soilItem.ItemTemplateId, out _))
            throw new InvalidOperationException($"Soil template for item template {soilItem.ItemTemplateId} was not found.");

        var playerSoil = await _playerSoils.GetByPlayerItemIdAsync(soilPlayerItemId, cancellationToken)
                         ?? throw new InvalidOperationException($"Player soil record for item {soilPlayerItemId} was not found.");

        if (playerSoil.State == (int)PlayerSoilState.Inserted)
            throw new InvalidOperationException($"Soil player item {soilPlayerItemId} is already inserted into another plot.");

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
            throw new InvalidOperationException($"Plot {plotIndex} in cave {caveId} already has a soil inserted.");

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
            throw new InvalidOperationException("Plot chua co linh tho, khong the trong cay.");
        if (plot.CurrentPlayerHerbId.HasValue)
            throw new InvalidOperationException("Plot da co linh duoc dang trong.");

        var seedItem = await _playerItems.GetByIdAsync(seedPlayerItemId, cancellationToken)
                      ?? throw new InvalidOperationException($"Seed player item {seedPlayerItemId} was not found.");
        if (seedItem.PlayerId != playerId)
            throw new InvalidOperationException($"Seed player item {seedPlayerItemId} does not belong to player {playerId}.");

        if (!_itemDefinitions.TryGetItem(seedItem.ItemTemplateId, out var seedDefinition) || seedDefinition.ItemType != ItemType.HerbSeed)
            throw new InvalidOperationException($"Player item {seedPlayerItemId} is not a herb seed.");

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
            CurrentAgeYears = 0,
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
            throw new InvalidOperationException("Plot chua co linh tho, khong the trong cay.");
        if (plot.CurrentPlayerHerbId.HasValue)
            throw new InvalidOperationException("Plot da co linh duoc dang trong.");

        var herb = await _playerHerbs.GetByIdAsync(playerHerbId, cancellationToken)
                   ?? throw new InvalidOperationException($"Player herb {playerHerbId} was not found.");
        if (herb.PlayerId != playerId)
            throw new InvalidOperationException($"Player herb {playerHerbId} does not belong to player {playerId}.");
        if (herb.State != (int)PlayerHerbState.InInventory)
            throw new InvalidOperationException("Chi co the trong lai linh duoc dang o trong tui.");

        herb.State = (int)PlayerHerbState.Planting;
        herb.CurrentPlotId = plot.Id;
        herb.PlantedAt = DateTime.UtcNow;
        herb.UpdatedAt = DateTime.UtcNow;

        plot.CurrentPlayerHerbId = herb.Id;
        plot.UpdatedAt = DateTime.UtcNow;

        await _playerHerbs.UpdateAsync(herb, cancellationToken);
        await _playerGardenPlots.UpdateAsync(plot, cancellationToken);
    }

    public async Task MoveHerbToInventoryAsync(Guid playerId, long playerHerbId, CancellationToken cancellationToken = default)
    {
        var herb = await RequireOwnedHerbAsync(playerId, playerHerbId, cancellationToken);
        herb = await MaterializeHerbProgressAsync(herb, cancellationToken);

        await using var tx = await _db.BeginTransactionAsync(cancellationToken);
        var plot = herb.CurrentPlotId.HasValue
            ? await _playerGardenPlots.GetByIdAsync(herb.CurrentPlotId.Value, cancellationToken)
            : null;

        if (plot is not null)
        {
            if (plot.CurrentSoilPlayerItemId.HasValue)
            {
                var soil = await _playerSoils.GetByPlayerItemIdAsync(plot.CurrentSoilPlayerItemId.Value, cancellationToken);
                if (soil is not null)
                {
                    soil.State = soil.State == (int)PlayerSoilState.Depleted
                        ? (int)PlayerSoilState.Depleted
                        : (int)PlayerSoilState.InInventory;
                    soil.InsertedPlotId = null;
                    soil.UpdatedAt = DateTime.UtcNow;
                    await _playerSoils.UpdateAsync(soil, cancellationToken);
                }
            }

            plot.CurrentSoilPlayerItemId = null;
            plot.CurrentPlayerHerbId = null;
            plot.UpdatedAt = DateTime.UtcNow;
            await _playerGardenPlots.UpdateAsync(plot, cancellationToken);
        }

        herb.State = (int)PlayerHerbState.InInventory;
        herb.CurrentPlotId = null;
        herb.PlantedAt = null;
        herb.UpdatedAt = DateTime.UtcNow;
        await _playerHerbs.UpdateAsync(herb, cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    public async Task<HerbRuntimeState> GetHerbRuntimeStateAsync(long playerHerbId, CancellationToken cancellationToken = default)
    {
        var herb = await _playerHerbs.GetByIdAsync(playerHerbId, cancellationToken)
                   ?? throw new InvalidOperationException($"Player herb {playerHerbId} was not found.");
        herb = await MaterializeHerbProgressAsync(herb, cancellationToken);
        return await BuildRuntimeStateAsync(herb, cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryItemView>> HarvestHerbAsync(
        Guid playerId,
        long playerHerbId,
        CancellationToken cancellationToken = default)
    {
        var herb = await RequireOwnedHerbAsync(playerId, playerHerbId, cancellationToken);
        herb = await MaterializeHerbProgressAsync(herb, cancellationToken);

        if (!_definitions.TryGetHerb(herb.HerbTemplateId, out var herbDefinition))
            throw new InvalidOperationException($"Herb template {herb.HerbTemplateId} was not found.");

        var outputs = ResolveHarvestOutputs(herbDefinition, (HerbGrowthStage)herb.CurrentStage);
        if (outputs.Count == 0)
            throw new InvalidOperationException($"Herb template {herb.HerbTemplateId} does not have harvest output for stage {(HerbGrowthStage)herb.CurrentStage}.");

        await using var tx = await _db.BeginTransactionAsync(cancellationToken);
        var created = new List<PlayerItemEntity>();
        foreach (var output in outputs)
        {
            if (!_randomService.CheckChance(ToPartsPerMillion(output.OutputChance)).Success)
                continue;

            var createdItems = await _itemService.AddItemAsync(
                playerId,
                output.ResultItemTemplateId,
                output.ResultQuantity,
                false,
                null,
                cancellationToken);
            created.AddRange(createdItems);
        }

        if (herb.CurrentPlotId.HasValue)
        {
            var plot = await _playerGardenPlots.GetByIdAsync(herb.CurrentPlotId.Value, cancellationToken);
            if (plot is not null)
            {
                plot.CurrentPlayerHerbId = null;
                plot.UpdatedAt = DateTime.UtcNow;
                await _playerGardenPlots.UpdateAsync(plot, cancellationToken);
            }
        }

        await _playerHerbs.DeleteAsync(herb.Id, cancellationToken);
        await tx.CommitAsync(cancellationToken);

        var inventory = await _itemService.GetInventoryAsync(playerId, cancellationToken);
        return inventory.Where(x => created.Any(createdItem => createdItem.Id == x.PlayerItemId)).ToArray();
    }

    private async Task<PlayerCaveEntity> RequireOwnedCaveAsync(Guid playerId, long caveId, CancellationToken cancellationToken)
    {
        var cave = await _playerCaves.GetByIdAsync(caveId, cancellationToken)
                   ?? throw new InvalidOperationException($"Player cave {caveId} was not found.");
        if (cave.OwnerCharacterId != playerId)
            throw new InvalidOperationException($"Player cave {caveId} does not belong to player {playerId}.");

        return cave;
    }

    private async Task<PlayerGardenPlotEntity> RequireOwnedPlotAsync(Guid playerId, long caveId, int plotIndex, CancellationToken cancellationToken)
    {
        await RequireOwnedCaveAsync(playerId, caveId, cancellationToken);
        return await _playerGardenPlots.GetByCaveAndPlotIndexAsync(caveId, plotIndex, cancellationToken)
               ?? throw new InvalidOperationException($"Garden plot {plotIndex} in cave {caveId} was not found.");
    }

    private async Task<PlayerHerbEntity> RequireOwnedHerbAsync(Guid playerId, long playerHerbId, CancellationToken cancellationToken)
    {
        var herb = await _playerHerbs.GetByIdAsync(playerHerbId, cancellationToken)
                   ?? throw new InvalidOperationException($"Player herb {playerHerbId} was not found.");
        if (herb.PlayerId != playerId)
            throw new InvalidOperationException($"Player herb {playerHerbId} does not belong to player {playerId}.");

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
            herb.CurrentAgeYears,
            herb.State == (int)PlayerHerbState.Planting && herb.CurrentPlotId.HasValue && soilRemainingSeconds > 0,
            herb.CurrentPlotId,
            soilPlayerItemId,
            soilRemainingSeconds);
    }

    private async Task ConsumeSpecificPlayerItemUnitAsync(PlayerItemEntity playerItem, CancellationToken cancellationToken)
    {
        if (playerItem.Quantity <= 0)
            throw new InvalidOperationException($"Player item {playerItem.Id} has invalid quantity {playerItem.Quantity}.");

        if (playerItem.Quantity == 1)
        {
            await _itemService.RemovePlayerItemAsync(playerItem.PlayerId, playerItem.Id, cancellationToken);
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
            herb.CurrentAgeYears = 0;
            return;
        }

        herb.CurrentStage = (int)stage.Stage;
        herb.CurrentAgeYears = stage.AgeYears;
    }

    private static int ToPartsPerMillion(double rawRate)
    {
        var normalized = rawRate <= 1d ? rawRate : rawRate / 100d;
        normalized = Math.Clamp(normalized, 0d, 1d);
        return (int)Math.Round(normalized * 1_000_000d, MidpointRounding.AwayFromZero);
    }
}
