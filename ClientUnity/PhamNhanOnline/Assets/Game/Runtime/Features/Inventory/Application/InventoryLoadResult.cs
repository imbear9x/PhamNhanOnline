using GameShared.Messages;
using GameShared.Models;

namespace PhamNhanOnline.Client.Features.Inventory.Application
{
    public struct InventoryLoadResult
    {
        public InventoryLoadResult(
            bool success,
            MessageCode? code,
            InventoryItemModel[] items,
            string message,
            bool servedFromCache)
        {
            Success = success;
            Code = code;
            Items = items ?? System.Array.Empty<InventoryItemModel>();
            Message = message;
            ServedFromCache = servedFromCache;
        }

        public bool Success { get; }
        public MessageCode? Code { get; }
        public InventoryItemModel[] Items { get; }
        public string Message { get; }
        public bool ServedFromCache { get; }
    }
}
