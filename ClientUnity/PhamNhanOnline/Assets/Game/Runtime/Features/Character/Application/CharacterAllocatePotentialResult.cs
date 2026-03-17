using GameShared.Messages;
using GameShared.Models;

namespace PhamNhanOnline.Client.Features.Character.Application
{
    public struct CharacterAllocatePotentialResult
    {
        public CharacterAllocatePotentialResult(
            bool success,
            MessageCode? code,
            CharacterBaseStatsModel? baseStats,
            CharacterCurrentStateModel? currentState,
            int requestedPotentialAmount,
            int spentPotentialAmount,
            int appliedUpgradeCount,
            string message)
        {
            Success = success;
            Code = code;
            BaseStats = baseStats;
            CurrentState = currentState;
            RequestedPotentialAmount = requestedPotentialAmount;
            SpentPotentialAmount = spentPotentialAmount;
            AppliedUpgradeCount = appliedUpgradeCount;
            Message = message;
        }

        public bool Success { get; }
        public MessageCode? Code { get; }
        public CharacterBaseStatsModel? BaseStats { get; }
        public CharacterCurrentStateModel? CurrentState { get; }
        public int RequestedPotentialAmount { get; }
        public int SpentPotentialAmount { get; }
        public int AppliedUpgradeCount { get; }
        public string Message { get; }
    }
}
