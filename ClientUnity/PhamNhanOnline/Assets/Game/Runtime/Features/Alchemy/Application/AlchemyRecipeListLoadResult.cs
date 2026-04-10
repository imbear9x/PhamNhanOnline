using GameShared.Messages;
using GameShared.Models;

namespace PhamNhanOnline.Client.Features.Alchemy.Application
{
    public readonly struct AlchemyRecipeListLoadResult
    {
        public AlchemyRecipeListLoadResult(
            bool success,
            MessageCode? code,
            LearnedPillRecipeModel[] recipes,
            string message,
            bool fromCache)
        {
            Success = success;
            Code = code;
            Recipes = recipes ?? System.Array.Empty<LearnedPillRecipeModel>();
            Message = message ?? string.Empty;
            FromCache = fromCache;
        }

        public bool Success { get; }
        public MessageCode? Code { get; }
        public LearnedPillRecipeModel[] Recipes { get; }
        public string Message { get; }
        public bool FromCache { get; }
    }
}
