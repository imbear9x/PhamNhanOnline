using UnityEngine;

namespace PhamNhanOnline.Client.Features.Character.Presentation
{
    public sealed class VirtualCharacterActionInputSource : CharacterActionInputSource
    {
        [SerializeField] private Vector2 moveInput;

        public override CharacterActionInputState ReadInput()
        {
            return new CharacterActionInputState
            {
                Horizontal = Mathf.Clamp(moveInput.x, -1f, 1f),
                Vertical = Mathf.Clamp(moveInput.y, -1f, 1f),
            };
        }

        public void SetMoveInput(Vector2 input)
        {
            moveInput = Vector2.ClampMagnitude(input, 1f);
        }

        public void SetHorizontal(float value)
        {
            moveInput.x = Mathf.Clamp(value, -1f, 1f);
        }

        public void SetVertical(float value)
        {
            moveInput.y = Mathf.Clamp(value, -1f, 1f);
        }
    }
}
