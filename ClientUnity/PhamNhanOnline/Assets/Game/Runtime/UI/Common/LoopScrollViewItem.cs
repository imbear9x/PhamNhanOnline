using UnityEngine;

namespace PhamNhanOnline.Client.UI.Common
{
    [DisallowMultipleComponent]
    public class LoopScrollViewItem : MonoBehaviour
    {
        [SerializeField] private string itemPrefabNameOverride;

        private RectTransform cachedRectTransform;

        public int ItemIndex { get; internal set; } = -1;

        public string ItemPrefabName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(itemPrefabNameOverride))
                    return itemPrefabNameOverride.Trim();

                return gameObject.name.Trim();
            }
        }

        public RectTransform RectTransform
        {
            get
            {
                if (cachedRectTransform == null)
                    cachedRectTransform = transform as RectTransform;

                return cachedRectTransform;
            }
        }

        public virtual void OnItemVisible(int itemIndex)
        {
        }

        public virtual void OnItemRecycled()
        {
        }

        private void Awake()
        {
            if (cachedRectTransform == null)
                cachedRectTransform = transform as RectTransform;
        }
    }
}
