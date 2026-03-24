using GameShared.Messages;
using GameShared.Models;

namespace PhamNhanOnline.Client.Features.Inventory.Application
{
    public sealed class InventoryActionResult
    {
        public InventoryActionResult(
            bool success,
            MessageCode? code,
            InventoryItemModel[] items,
            CharacterBaseStatsModel? baseStats,
            CharacterCurrentStateModel? currentState,
            string message)
        {
            Success = success;
            Code = code;
            Items = items ?? System.Array.Empty<InventoryItemModel>();
            BaseStats = baseStats;
            CurrentState = currentState;
            Message = message ?? string.Empty;
        }

        public bool Success { get; }
        public MessageCode? Code { get; }
        public InventoryItemModel[] Items { get; }
        public CharacterBaseStatsModel? BaseStats { get; }
        public CharacterCurrentStateModel? CurrentState { get; }
        public string Message { get; }
    }
}
