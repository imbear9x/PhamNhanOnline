using UnityEngine;

namespace PhamNhanOnline.Client.Features.Character.Presentation
{
    public sealed class KeyboardCharacterActionInputSource : CharacterActionInputSource
    {
        public override CharacterActionInputState ReadInput()
        {
            return new CharacterActionInputState
            {
                Horizontal = Input.GetAxisRaw("Horizontal"),
                Vertical = Input.GetAxisRaw("Vertical"),
                AttackPressed = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.J),
            };
        }
    }
}
