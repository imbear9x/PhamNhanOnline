using UnityEngine;

namespace PhamNhanOnline.Client.UI.Common
{
    public abstract class ViewModelBase : MonoBehaviour
    {
        private bool isInit;

        public bool IsVisible
        {
            get
            {
                var root = ResolveViewRoot();
                return root != null && root.activeSelf;
            }
        }

        protected virtual bool HideOnFirstAwake => false;

        protected virtual void Awake()
        {
            if (HideOnFirstAwake && !isInit)
                SetViewVisible(false, force: true);
        }

        protected virtual GameObject ResolveViewRoot()
        {
            return gameObject;
        }

        protected virtual RectTransform ResolveViewRectTransform()
        {
            return ResolveViewRoot() != null ? ResolveViewRoot().transform as RectTransform : null;
        }

        protected void SetViewVisible(bool visible, bool force = false)
        {
            var root = ResolveViewRoot();
            if (root == null)
                return;

            if (force || root.activeSelf != visible)
                root.SetActive(visible);
        }

        protected void ShowView(bool force = false)
        {
            isInit = true;
            SetViewVisible(true, force);
        }
    }
}
