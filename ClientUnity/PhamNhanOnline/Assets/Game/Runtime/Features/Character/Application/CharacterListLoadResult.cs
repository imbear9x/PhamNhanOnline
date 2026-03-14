using GameShared.Messages;
using GameShared.Models;

namespace PhamNhanOnline.Client.Features.Character.Application
{
    public struct CharacterListLoadResult
    {
        public CharacterListLoadResult(bool success, MessageCode? code, CharacterModel[] characters, string message)
        {
            Success = success;
            Code = code;
            Characters = characters;
            Message = message;
        }

        public bool Success { get; }
        public MessageCode? Code { get; }
        public CharacterModel[] Characters { get; }
        public string Message { get; }
    }
}
