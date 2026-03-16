using UnityEngine;

namespace PhamNhanOnline.Client.Features.Character.Presentation
{
    public abstract class CharacterActionInputSource : MonoBehaviour
    {
        public abstract CharacterActionInputState ReadInput();
    }
}
