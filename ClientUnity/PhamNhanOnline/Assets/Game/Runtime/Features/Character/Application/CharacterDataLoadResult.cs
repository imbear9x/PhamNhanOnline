using GameShared.Messages;
using GameShared.Models;

namespace PhamNhanOnline.Client.Features.Character.Application
{
    public struct CharacterDataLoadResult
    {
        public CharacterDataLoadResult(
            bool success,
            MessageCode? code,
            CharacterModel? character,
            CharacterBaseStatsModel? baseStats,
            CharacterCurrentStateModel? currentState,
            string message)
        {
            Success = success;
            Code = code;
            Character = character;
            BaseStats = baseStats;
            CurrentState = currentState;
            Message = message;
        }

        public bool Success { get; }
        public MessageCode? Code { get; }
        public CharacterModel? Character { get; }
        public CharacterBaseStatsModel? BaseStats { get; }
        public CharacterCurrentStateModel? CurrentState { get; }
        public string Message { get; }
    }
}
