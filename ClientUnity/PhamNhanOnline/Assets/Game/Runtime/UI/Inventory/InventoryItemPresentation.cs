using UnityEngine;

namespace PhamNhanOnline.Client.UI.Inventory
{
    public readonly struct InventoryItemPresentation
    {
        public InventoryItemPresentation(Sprite iconSprite, Sprite backgroundSprite, Color nameColor)
        {
            IconSprite = iconSprite;
            BackgroundSprite = backgroundSprite;
            NameColor = nameColor;
        }

        public Sprite IconSprite { get; }
        public Sprite BackgroundSprite { get; }
        public Color NameColor { get; }
    }
}
