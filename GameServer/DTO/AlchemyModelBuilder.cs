using GameServer.Descriptions;
using GameServer.Entities;
using GameServer.Runtime;
using GameShared.Models;
using System.Text.Json;

namespace GameServer.DTO;

public sealed class AlchemyModelBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ItemDefinitionCatalog _itemDefinitions;
    private readonly GameplayDescriptionService _descriptions;

    public AlchemyModelBuilder(
        ItemDefinitionCatalog itemDefinitions,
        GameplayDescriptionService descriptions)
    {
        _itemDefinitions = itemDefinitions;
        _descriptions = descriptions;
    }

    public LearnedPillRecipeModel BuildLearnedRecipeModel(
        LearnedPillRecipeView recipeView,
        PillRecipeTemplateDefinition definition)
    {
        return new LearnedPillRecipeModel
        {
            PillRecipeTemplateId = recipeView.PillRecipeTemplateId,
            Code = recipeView.Code,
            Name = recipeView.Name,
            Description = definition.Description,
            ResultPill = BuildItemTemplateSummary(definition.ResultPillItemTemplateId),
            CraftDurationSeconds = Math.Max(0L, definition.CraftDurationSeconds),
            BaseSuccessRate = NormalizeRate(definition.BaseSuccessRate),
            SuccessRateCap = definition.SuccessRateCap.HasValue ? NormalizeRate(definition.SuccessRateCap.Value) : null,
            MutationRate = NormalizeRate(definition.MutationRate),
            MutationRateCap = NormalizeRate(definition.MutationRateCap),
            TotalCraftCount = recipeView.TotalCraftCount,
            CurrentSuccessRateBonus = NormalizeRate(recipeView.CurrentSuccessRateBonus),
            LearnedUnixMs = ToUnixMs(recipeView.LearnedAt)
        };
    }

    public PillRecipeDetailModel BuildRecipeDetailModel(
        PillRecipeTemplateDefinition definition,
        PlayerPillRecipeEntity progress)
    {
        return new PillRecipeDetailModel
        {
            PillRecipeTemplateId = definition.Id,
            Code = definition.Code,
            Name = definition.Name,
            Description = definition.Description,
            RecipeBookItem = BuildItemTemplateSummary(definition.RecipeBookItemTemplateId),
            ResultPill = BuildItemTemplateSummary(definition.ResultPillItemTemplateId),
            CraftDurationSeconds = Math.Max(0L, definition.CraftDurationSeconds),
            BaseSuccessRate = NormalizeRate(definition.BaseSuccessRate),
            SuccessRateCap = definition.SuccessRateCap.HasValue ? NormalizeRate(definition.SuccessRateCap.Value) : null,
            MutationRate = NormalizeRate(definition.MutationRate),
            MutationRateCap = NormalizeRate(definition.MutationRateCap),
            TotalCraftCount = progress.TotalCraftCount,
            CurrentSuccessRateBonus = NormalizeRate(progress.CurrentSuccessRateBonus),
            LearnedUnixMs = ToUnixMs(progress.LearnedAt),
            Inputs = definition.Inputs.Select(BuildInputModel).ToList(),
            MasteryStages = definition.MasteryStages
                .Select(stage => BuildMasteryStageModel(stage, progress.TotalCraftCount))
                .ToList()
        };
    }

    public AlchemyCraftPreviewModel BuildCraftPreviewModel(
        int recipeId,
        AlchemyValidationResult validation,
        IReadOnlyDictionary<long, InventoryItemView> inventoryByPlayerItemId)
    {
        var consumedItems = BuildConsumedItems(validation, inventoryByPlayerItemId);
        return new AlchemyCraftPreviewModel
        {
            PillRecipeTemplateId = recipeId,
            RequestedCraftCount = Math.Max(1, validation.RequestedCraftCount),
            MaxCraftableCount = Math.Max(0, validation.MaxCraftableCount),
            CanCraft = validation.Success && validation.MaxCraftableCount > 0,
            FailureReason = validation.FailureReason,
            EffectiveSuccessRate = NormalizeRate(validation.EffectiveSuccessRate),
            BoostedSuccessRate = NormalizeRate(validation.BoostedSuccessRate),
            EffectiveMutationRate = NormalizeRate(validation.EffectiveMutationRate),
            BoostedMutationRate = NormalizeRate(validation.BoostedMutationRate),
            BoostedCraftCount = Math.Max(0, validation.BoostedCraftCount),
            AppliedOptionalInputs = validation.AppliedOptionalInputs
                .Select(selection => new AlchemyOptionalInputSelectionModel
                {
                    InputId = selection.Input.Id,
                    Quantity = Math.Max(0, selection.AppliedCount)
                })
                .ToList(),
            ConsumedItems = consumedItems
        };
    }

    public AlchemyPracticeStatusModel BuildAlchemyPracticeStatusModel(
        PlayerPracticeSessionEntity session,
        PillRecipeDetailModel? recipe)
    {
        return new AlchemyPracticeStatusModel
        {
            Session = BuildPracticeSessionModel(session),
            Recipe = recipe,
            ConsumedItems = BuildConsumedItems(session),
            PendingResult = BuildPracticeCompletionResultModel(session)
        };
    }

    public PracticeSessionModel BuildPracticeSessionModel(PlayerPracticeSessionEntity session)
    {
        var payload = string.IsNullOrWhiteSpace(session.RequestPayloadJson)
            ? null
            : JsonSerializer.Deserialize<PracticeSessionPayload>(session.RequestPayloadJson, JsonOptions);
        var utcNow = DateTime.UtcNow;
        var accumulated = Math.Max(0L, session.AccumulatedActiveSeconds);
        if (session.PracticeState == (int)PracticeSessionState.Active && session.LastResumedAtUtc.HasValue)
        {
            var elapsed = utcNow - session.LastResumedAtUtc.Value;
            if (elapsed > TimeSpan.Zero)
                accumulated += (long)Math.Floor(elapsed.TotalSeconds);
        }

        var remaining = Math.Max(0L, Math.Max(0L, session.TotalDurationSeconds) - accumulated);
        var progress = session.TotalDurationSeconds <= 0L
            ? 1d
            : Math.Clamp((double)accumulated / session.TotalDurationSeconds, 0d, 1d);
        return new PracticeSessionModel
        {
            PracticeSessionId = session.Id,
            PracticeType = session.PracticeType,
            PracticeState = session.PracticeState,
            DefinitionId = session.DefinitionId,
            RequestedCraftCount = payload?.RequestedCraftCount ?? 1,
            BoostedCraftCount = payload?.SelectedOptionalInputs?.Sum(static entry => Math.Max(0, entry.AppliedCount)) ?? 0,
            Title = session.Title,
            TotalDurationSeconds = Math.Max(0L, session.TotalDurationSeconds),
            AccumulatedActiveSeconds = accumulated,
            RemainingDurationSeconds = remaining,
            Progress = progress,
            CanPause = session.PracticeState == (int)PracticeSessionState.Active &&
                       progress < Math.Clamp(session.CancelLockedProgress, 0d, 1d),
            CanCancel = session.PracticeState != (int)PracticeSessionState.ResultPendingAcknowledgement &&
                        session.PracticeState != (int)PracticeSessionState.Completed &&
                        session.PracticeState != (int)PracticeSessionState.Cancelled,
            IsPaused = session.PracticeState == (int)PracticeSessionState.Paused,
            StartedUnixMs = ToUnixMs(session.StartedAtUtc),
            LastResumedUnixMs = ToUnixMs(session.LastResumedAtUtc),
            PausedUnixMs = ToUnixMs(session.PausedAtUtc),
            CompletedUnixMs = ToUnixMs(session.CompletedAtUtc)
        };
    }

    public PracticeCompletionResultModel? BuildPracticeCompletionResultModel(PlayerPracticeSessionEntity session)
    {
        if (string.IsNullOrWhiteSpace(session.ResultPayloadJson))
            return null;

        var payload = JsonSerializer.Deserialize<PracticeCompletionPayload>(session.ResultPayloadJson, JsonOptions);
        if (payload is null)
            return null;

        var rewards = (payload.Rewards ?? Array.Empty<PracticeRewardEntry>())
            .Select(reward => new PracticeRewardItemModel
            {
                Item = BuildItemTemplateSummary(reward.ItemTemplateId),
                Quantity = Math.Max(0, reward.Quantity)
            })
            .ToList();

        return new PracticeCompletionResultModel
        {
            PracticeSessionId = session.Id,
            PracticeType = session.PracticeType,
            Success = payload.Success,
            RequestedCraftCount = Math.Max(1, payload.RequestedCraftCount),
            SuccessCount = Math.Max(0, payload.SuccessCount),
            FailedCount = Math.Max(0, payload.FailedCount),
            Title = payload.Title,
            Message = payload.Message,
            DisplayItem = payload.DisplayItemTemplateId.HasValue
                ? BuildItemTemplateSummary(payload.DisplayItemTemplateId.Value)
                : null,
            PrimaryReward = rewards.Count > 0 ? rewards[0] : null,
            Rewards = rewards,
            CompletedUnixMs = ToUnixMs(session.CompletedAtUtc)
        };
    }

    public List<AlchemyConsumedItemModel> BuildConsumedItems(
        AlchemyValidationResult validation,
        IReadOnlyDictionary<long, InventoryItemView> inventoryByPlayerItemId)
    {
        var consumedItems = new List<AlchemyConsumedItemModel>();

        foreach (var playerItemId in validation.ConsumedPlayerItemIds)
        {
            if (!inventoryByPlayerItemId.TryGetValue(playerItemId, out var item))
                continue;

            consumedItems.Add(new AlchemyConsumedItemModel
            {
                PlayerItemId = item.PlayerItemId,
                Item = BuildItemTemplateSummary(item.Definition),
                Quantity = 1
            });
        }

        foreach (var pair in validation.ConsumedStackQuantities.OrderBy(static x => x.Key))
        {
            if (!inventoryByPlayerItemId.TryGetValue(pair.Key, out var item))
                continue;

            consumedItems.Add(new AlchemyConsumedItemModel
            {
                PlayerItemId = item.PlayerItemId,
                Item = BuildItemTemplateSummary(item.Definition),
                Quantity = Math.Max(0, pair.Value)
            });
        }

        return consumedItems;
    }

    public List<AlchemyConsumedItemModel> BuildConsumedItems(PlayerPracticeSessionEntity session)
    {
        if (string.IsNullOrWhiteSpace(session.RequestPayloadJson))
            return new List<AlchemyConsumedItemModel>();

        var payload = JsonSerializer.Deserialize<PracticeSessionPayload>(session.RequestPayloadJson, JsonOptions);
        var consumedEntries = payload?.ConsumedEntries ?? Array.Empty<PracticeConsumedEntry>();
        if (consumedEntries.Count == 0)
            return new List<AlchemyConsumedItemModel>();

        return consumedEntries
            .Select(entry => new AlchemyConsumedItemModel
            {
                PlayerItemId = entry.PlayerItemId,
                Item = BuildItemTemplateSummary(entry.ItemTemplateId),
                Quantity = Math.Max(0, entry.Quantity)
            })
            .ToList();
    }

    private PillRecipeInputModel BuildInputModel(PillRecipeInputDefinition input)
    {
        return new PillRecipeInputModel
        {
            InputId = input.Id,
            RequiredItem = BuildItemTemplateSummary(input.RequiredItemTemplateId),
            RequiredQuantity = input.RequiredQuantity,
            ConsumeMode = (int)input.ConsumeMode,
            IsOptional = input.IsOptional,
            SuccessRateBonus = NormalizeRate(input.SuccessRateBonus),
            MutationBonusRate = NormalizeRate(input.MutationBonusRate),
            RequiredHerbMaturity = (int)input.RequiredHerbMaturity
        };
    }

    private static PillRecipeMasteryStageModel BuildMasteryStageModel(
        PillRecipeMasteryStageDefinition stage,
        int totalCraftCount)
    {
        return new PillRecipeMasteryStageModel
        {
            StageId = stage.Id,
            RequiredTotalCraftCount = stage.RequiredTotalCraftCount,
            SuccessRateBonus = NormalizeRate(stage.SuccessRateBonus),
            IsReached = totalCraftCount >= stage.RequiredTotalCraftCount
        };
    }

    public ItemTemplateSummaryModel BuildItemTemplateSummary(int itemTemplateId)
    {
        if (!_itemDefinitions.TryGetItem(itemTemplateId, out var definition))
            throw new InvalidOperationException($"Item template {itemTemplateId} was not found.");

        return BuildItemTemplateSummary(definition);
    }

    private ItemTemplateSummaryModel BuildItemTemplateSummary(ItemDefinition definition)
    {
        return new ItemTemplateSummaryModel
        {
            ItemTemplateId = definition.Id,
            Code = definition.Code,
            Name = definition.Name,
            ItemType = (int)definition.ItemType,
            Rarity = (int)definition.Rarity,
            Icon = definition.Icon,
            BackgroundIcon = definition.BackgroundIcon,
            Description = _descriptions.BuildItemDescription(definition),
            MaxStack = definition.MaxStack,
            IsStackable = definition.IsStackable
        };
    }

    private static long? ToUnixMs(DateTime? dateTime)
    {
        if (!dateTime.HasValue)
            return null;

        var utc = dateTime.Value.Kind == DateTimeKind.Utc
            ? dateTime.Value
            : DateTime.SpecifyKind(dateTime.Value, DateTimeKind.Utc);

        return new DateTimeOffset(utc).ToUnixTimeMilliseconds();
    }

    private static double NormalizeRate(double rawRate)
    {
        if (rawRate <= 0d)
            return 0d;

        return rawRate > 1d
            ? Math.Clamp(rawRate / 100d, 0d, 1d)
            : Math.Clamp(rawRate, 0d, 1d);
    }
}
