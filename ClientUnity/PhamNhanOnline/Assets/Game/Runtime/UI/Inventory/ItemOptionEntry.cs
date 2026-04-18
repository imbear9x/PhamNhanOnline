using System;

namespace PhamNhanOnline.Client.UI.Inventory
{
    public readonly struct ItemOptionEntry
    {
        public ItemOptionEntry(string label, Action onClick, bool interactable = true)
        {
            Label = label;
            OnClick = onClick;
            Interactable = interactable;
        }

        public string Label { get; }
        public Action OnClick { get; }
        public bool Interactable { get; }
    }
}
