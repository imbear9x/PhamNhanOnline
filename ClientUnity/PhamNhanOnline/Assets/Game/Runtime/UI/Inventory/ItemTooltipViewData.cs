using UnityEngine;

namespace PhamNhanOnline.Client.UI.Inventory
{
    public readonly struct ItemTooltipViewData
    {
        public ItemTooltipViewData(string title, string description, Sprite iconSprite, Color titleColor)
        {
            Title = title;
            Description = description;
            IconSprite = iconSprite;
            TitleColor = titleColor;
        }

        public string Title { get; }
        public string Description { get; }
        public Sprite IconSprite { get; }
        public Color TitleColor { get; }
    }
}
