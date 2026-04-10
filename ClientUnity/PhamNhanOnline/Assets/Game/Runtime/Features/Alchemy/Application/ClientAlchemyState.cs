using System;
using System.Collections.Generic;
using System.Linq;
using GameShared.Messages;
using GameShared.Models;

namespace PhamNhanOnline.Client.Features.Alchemy.Application
{
    public sealed class ClientAlchemyState
    {
        private readonly Dictionary<int, PillRecipeDetailModel> detailsByRecipeId =
            new Dictionary<int, PillRecipeDetailModel>();

        public event Action Changed;

        public bool HasLoadedRecipes { get; private set; }
        public bool IsLoadingRecipes { get; private set; }
        public MessageCode? LastResultCode { get; private set; }
        public string LastStatusMessage { get; private set; } = string.Empty;
        public DateTime? LastLoadedAtUtc { get; private set; }
        public LearnedPillRecipeModel[] Recipes { get; private set; } = Array.Empty<LearnedPillRecipeModel>();
        public PillRecipeDetailModel? LastViewedRecipe { get; private set; }
        public AlchemyCraftPreviewModel? LastPreview { get; private set; }
        public AlchemyPracticeStatusModel? LastPracticeStatus { get; private set; }
        public PracticeSessionModel? CurrentPracticeSession { get; private set; }
        public PracticeCompletionResultModel? PendingPracticeResult { get; private set; }

        public void BeginLoadingRecipes()
        {
            if (IsLoadingRecipes)
                return;

            IsLoadingRecipes = true;
            NotifyChanged();
        }

        public void ApplyRecipes(LearnedPillRecipeModel[] recipes, MessageCode? code, string statusMessage)
        {
            HasLoadedRecipes = true;
            IsLoadingRecipes = false;
            LastResultCode = code;
            LastStatusMessage = statusMessage ?? string.Empty;
            LastLoadedAtUtc = DateTime.UtcNow;
            Recipes = recipes ?? Array.Empty<LearnedPillRecipeModel>();
            NotifyChanged();
        }

        public void ApplyRecipeDetail(PillRecipeDetailModel detail, MessageCode? code, string statusMessage)
        {
            LastResultCode = code;
            LastStatusMessage = statusMessage ?? string.Empty;
            LastViewedRecipe = detail;
            detailsByRecipeId[detail.PillRecipeTemplateId] = detail;
            Recipes = UpdateRecipesFromDetail(Recipes, detail);
            NotifyChanged();
        }

        public void ApplyPreview(AlchemyCraftPreviewModel? preview, MessageCode? code, string statusMessage)
        {
            LastResultCode = code;
            LastStatusMessage = statusMessage ?? string.Empty;
            LastPreview = preview;
            NotifyChanged();
        }

        public void ApplyCraftOutcome(PillRecipeDetailModel? recipe, MessageCode? code, string statusMessage)
        {
            LastResultCode = code;
            LastStatusMessage = statusMessage ?? string.Empty;
            LastPreview = null;

            if (recipe.HasValue)
            {
                LastViewedRecipe = recipe;
                detailsByRecipeId[recipe.Value.PillRecipeTemplateId] = recipe.Value;
                Recipes = UpdateRecipesFromDetail(Recipes, recipe.Value);
            }

            NotifyChanged();
        }

        public void ApplyCraftStarted(
            PillRecipeDetailModel? recipe,
            PracticeSessionModel? session,
            AlchemyConsumedItemModel[] consumedItems,
            MessageCode? code,
            string statusMessage)
        {
            LastResultCode = code;
            LastStatusMessage = statusMessage ?? string.Empty;
            LastPreview = null;

            if (recipe.HasValue)
            {
                LastViewedRecipe = recipe;
                detailsByRecipeId[recipe.Value.PillRecipeTemplateId] = recipe.Value;
                Recipes = UpdateRecipesFromDetail(Recipes, recipe.Value);
            }

            CurrentPracticeSession = session;
            LastPracticeStatus = new AlchemyPracticeStatusModel
            {
                Session = session,
                Recipe = recipe,
                ConsumedItems = consumedItems != null ? consumedItems.ToList() : null,
                PendingResult = PendingPracticeResult
            };

            NotifyChanged();
        }

        public void ApplyPracticeStatus(AlchemyPracticeStatusModel? status, MessageCode? code, string statusMessage)
        {
            LastResultCode = code;
            LastStatusMessage = statusMessage ?? string.Empty;
            LastPracticeStatus = status;
            CurrentPracticeSession = status.HasValue ? status.Value.Session : null;
            PendingPracticeResult = status.HasValue ? status.Value.PendingResult : null;
            if (status.HasValue && status.Value.Recipe.HasValue)
            {
                LastViewedRecipe = status.Value.Recipe.Value;
                detailsByRecipeId[status.Value.Recipe.Value.PillRecipeTemplateId] = status.Value.Recipe.Value;
                Recipes = UpdateRecipesFromDetail(Recipes, status.Value.Recipe.Value);
            }

            NotifyChanged();
        }

        public void ApplyPracticeSessionChange(PracticeSessionModel? session, MessageCode? code, string statusMessage)
        {
            LastResultCode = code;
            LastStatusMessage = statusMessage ?? string.Empty;
            CurrentPracticeSession = session;
            if (LastPracticeStatus.HasValue)
            {
                var practiceStatus = LastPracticeStatus.Value;
                practiceStatus.Session = session;
                if (!session.HasValue)
                    practiceStatus.ConsumedItems = null;
                LastPracticeStatus = practiceStatus;
            }

            NotifyChanged();
        }

        public void ApplyPracticeResult(PracticeSessionModel? session, PracticeCompletionResultModel result, MessageCode? code, string statusMessage)
        {
            LastResultCode = code;
            LastStatusMessage = statusMessage ?? string.Empty;
            CurrentPracticeSession = session;
            PendingPracticeResult = result;
            if (LastPracticeStatus.HasValue)
            {
                var practiceStatus = LastPracticeStatus.Value;
                practiceStatus.Session = session;
                practiceStatus.PendingResult = result;
                LastPracticeStatus = practiceStatus;
            }
            else
            {
                LastPracticeStatus = new AlchemyPracticeStatusModel
                {
                    Session = session,
                    PendingResult = result
                };
            }

            NotifyChanged();
        }

        public void ClearPendingPracticeResult(MessageCode? code, string statusMessage)
        {
            LastResultCode = code;
            LastStatusMessage = statusMessage ?? string.Empty;
            PendingPracticeResult = null;
            if (LastPracticeStatus.HasValue)
            {
                var practiceStatus = LastPracticeStatus.Value;
                practiceStatus.PendingResult = null;
                LastPracticeStatus = practiceStatus;
            }

            NotifyChanged();
        }

        public void ApplyFailure(MessageCode? code, string statusMessage)
        {
            IsLoadingRecipes = false;
            LastResultCode = code;
            LastStatusMessage = statusMessage ?? string.Empty;
            NotifyChanged();
        }

        public bool TryGetRecipeDetail(int recipeId, out PillRecipeDetailModel detail)
        {
            return detailsByRecipeId.TryGetValue(recipeId, out detail);
        }

        public void Clear()
        {
            HasLoadedRecipes = false;
            IsLoadingRecipes = false;
            LastResultCode = null;
            LastStatusMessage = string.Empty;
            LastLoadedAtUtc = null;
            Recipes = Array.Empty<LearnedPillRecipeModel>();
            LastViewedRecipe = null;
            LastPreview = null;
            LastPracticeStatus = null;
            CurrentPracticeSession = null;
            PendingPracticeResult = null;
            detailsByRecipeId.Clear();
            NotifyChanged();
        }

        private static LearnedPillRecipeModel[] UpdateRecipesFromDetail(
            LearnedPillRecipeModel[] recipes,
            PillRecipeDetailModel detail)
        {
            if (recipes == null || recipes.Length == 0)
                return Array.Empty<LearnedPillRecipeModel>();

            var updated = recipes.ToArray();
            for (var i = 0; i < updated.Length; i++)
            {
                if (updated[i].PillRecipeTemplateId != detail.PillRecipeTemplateId)
                    continue;

                updated[i].Code = detail.Code;
                updated[i].Name = detail.Name;
                updated[i].Description = detail.Description;
                updated[i].ResultPill = detail.ResultPill;
                updated[i].CraftDurationSeconds = detail.CraftDurationSeconds;
                updated[i].BaseSuccessRate = detail.BaseSuccessRate;
                updated[i].SuccessRateCap = detail.SuccessRateCap;
                updated[i].MutationRate = detail.MutationRate;
                updated[i].MutationRateCap = detail.MutationRateCap;
                updated[i].TotalCraftCount = detail.TotalCraftCount;
                updated[i].CurrentSuccessRateBonus = detail.CurrentSuccessRateBonus;
                updated[i].LearnedUnixMs = detail.LearnedUnixMs;
                break;
            }

            return updated;
        }

        private void NotifyChanged()
        {
            var handler = Changed;
            if (handler != null)
                handler();
        }
    }
}
