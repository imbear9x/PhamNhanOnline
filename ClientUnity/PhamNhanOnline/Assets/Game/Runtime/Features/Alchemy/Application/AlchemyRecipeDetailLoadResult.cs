using GameShared.Messages;
using GameShared.Models;

namespace PhamNhanOnline.Client.Features.Alchemy.Application
{
    public readonly struct AlchemyRecipeDetailLoadResult
    {
        public AlchemyRecipeDetailLoadResult(
            bool success,
            MessageCode? code,
            PillRecipeDetailModel? recipe,
            string failureReason,
            string message,
            bool fromCache)
        {
            Success = success;
            Code = code;
            Recipe = recipe;
            FailureReason = failureReason ?? string.Empty;
            Message = message ?? string.Empty;
            FromCache = fromCache;
        }

        public bool Success { get; }
        public MessageCode? Code { get; }
        public PillRecipeDetailModel? Recipe { get; }
        public string FailureReason { get; }
        public string Message { get; }
        public bool FromCache { get; }
    }
}
