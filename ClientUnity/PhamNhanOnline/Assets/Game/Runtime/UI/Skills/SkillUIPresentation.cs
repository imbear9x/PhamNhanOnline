using UnityEngine;

namespace PhamNhanOnline.Client.UI.Skills
{
    public readonly struct SkillUIPresentation
    {
        public SkillUIPresentation(Sprite iconSprite)
        {
            IconSprite = iconSprite;
        }

        public Sprite IconSprite { get; }
    }
}
