using System;
using System.Collections.Generic;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.Common
{
    public sealed class UIScreenService
    {
        private readonly Dictionary<string, GameObject> screens = new(StringComparer.Ordinal);

        public void Register(string screenId, GameObject root)
        {
            if (string.IsNullOrWhiteSpace(screenId))
                throw new ArgumentException("Screen id is required.", nameof(screenId));
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            screens[screenId] = root;
        }

        public void Unregister(string screenId, GameObject root)
        {
            if (string.IsNullOrWhiteSpace(screenId))
                return;

            if (!screens.TryGetValue(screenId, out var registeredRoot))
                return;

            if (registeredRoot == root)
                screens.Remove(screenId);
        }

        public bool SetVisible(string screenId, bool isVisible)
        {
            if (!screens.TryGetValue(screenId, out var root) || root == null)
                return false;

            root.SetActive(isVisible);
            return true;
        }

        public bool Show(string screenId) => SetVisible(screenId, true);

        public bool Hide(string screenId) => SetVisible(screenId, false);

        public void ShowOnly(params string[] visibleScreenIds)
        {
            var visible = new HashSet<string>(visibleScreenIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            foreach (var pair in screens)
            {
                if (pair.Value == null)
                    continue;

                pair.Value.SetActive(visible.Contains(pair.Key));
            }
        }

        public bool IsRegistered(string screenId) => screens.ContainsKey(screenId);
    }
}
