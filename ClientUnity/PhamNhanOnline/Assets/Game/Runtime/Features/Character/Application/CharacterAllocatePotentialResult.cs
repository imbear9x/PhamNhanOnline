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
            string message)
        {
            Success = success;
            Code = code;
            BaseStats = baseStats;
            CurrentState = currentState;
            Message = message;
        }

        public bool Success { get; }
        public MessageCode? Code { get; }
        public CharacterBaseStatsModel? BaseStats { get; }
        public CharacterCurrentStateModel? CurrentState { get; }
        public string Message { get; }
    }
}
