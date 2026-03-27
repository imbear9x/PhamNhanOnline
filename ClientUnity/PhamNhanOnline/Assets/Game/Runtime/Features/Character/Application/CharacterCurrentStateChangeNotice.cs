using GameShared.Models;

namespace PhamNhanOnline.Client.Features.Character.Application
{
    public readonly struct CharacterCurrentStateChangeNotice
    {
        public CharacterCurrentStateChangeNotice(
            CharacterCurrentStateModel? previousState,
            CharacterCurrentStateModel? currentState)
        {
            PreviousState = previousState;
            CurrentState = currentState;
        }

        public CharacterCurrentStateModel? PreviousState { get; }
        public CharacterCurrentStateModel? CurrentState { get; }
    }
}
