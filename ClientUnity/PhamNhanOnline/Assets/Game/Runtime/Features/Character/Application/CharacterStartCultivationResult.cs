using GameShared.Messages;
using GameShared.Models;

namespace PhamNhanOnline.Client.Features.Character.Application
{
    public struct CharacterStartCultivationResult
    {
        public CharacterStartCultivationResult(bool success, MessageCode? code, CharacterCurrentStateModel? currentState, string message)
        {
            Success = success;
            Code = code;
            CurrentState = currentState;
            Message = message;
        }

        public bool Success { get; }
        public MessageCode? Code { get; }
        public CharacterCurrentStateModel? CurrentState { get; }
        public string Message { get; }
    }
}
