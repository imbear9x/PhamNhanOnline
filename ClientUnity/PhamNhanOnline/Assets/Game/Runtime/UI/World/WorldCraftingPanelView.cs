using PhamNhanOnline.Client.UI.Common;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.World
{
    [DisallowMultipleComponent]
    public sealed class WorldCraftingPanelView : ViewModelBase
    {
        public void Show()
        {
            ShowView();
        }

        public void Hide(bool force = false)
        {
            SetViewVisible(false, force);
        }
    }
}
