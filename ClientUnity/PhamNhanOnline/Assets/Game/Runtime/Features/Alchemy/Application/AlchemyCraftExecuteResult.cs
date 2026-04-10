using GameShared.Messages;
using GameShared.Models;

namespace PhamNhanOnline.Client.Features.Alchemy.Application
{
    public readonly struct AlchemyCraftExecuteResult
    {
        public AlchemyCraftExecuteResult(
            bool success,
            MessageCode? code,
            PillRecipeDetailModel? recipe,
            PracticeSessionModel? session,
            InventoryItemModel[] inventoryItems,
            AlchemyConsumedItemModel[] consumedItems,
            string failureReason,
            string message)
        {
            Success = success;
            Code = code;
            Recipe = recipe;
            Session = session;
            InventoryItems = inventoryItems ?? System.Array.Empty<InventoryItemModel>();
            ConsumedItems = consumedItems ?? System.Array.Empty<AlchemyConsumedItemModel>();
            FailureReason = failureReason ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public bool Success { get; }
        public MessageCode? Code { get; }
        public PillRecipeDetailModel? Recipe { get; }
        public PracticeSessionModel? Session { get; }
        public InventoryItemModel[] InventoryItems { get; }
        public AlchemyConsumedItemModel[] ConsumedItems { get; }
        public string FailureReason { get; }
        public string Message { get; }
    }
}
