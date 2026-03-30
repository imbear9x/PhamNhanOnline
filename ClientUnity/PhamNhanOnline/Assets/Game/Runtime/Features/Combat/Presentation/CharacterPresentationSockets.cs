using System;
using System.Collections.Generic;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.Combat.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CharacterPresentationSockets : MonoBehaviour
    {
        [Serializable]
        private sealed class SocketEntry
        {
            [SerializeField] private CharacterPresentationSocketType socketType = CharacterPresentationSocketType.None;
            [SerializeField] private Transform anchor;

            public CharacterPresentationSocketType SocketType => socketType;
            public Transform Anchor => anchor;
        }

        [SerializeField] private Transform visualRoot;
        [SerializeField] private List<SocketEntry> sockets = new List<SocketEntry>();

        public bool TryGetSocket(CharacterPresentationSocketType socketType, out Transform anchor)
        {
            AutoWireReferences();

            for (var i = 0; i < sockets.Count; i++)
            {
                var entry = sockets[i];
                if (entry == null || entry.SocketType != socketType || entry.Anchor == null)
                    continue;

                anchor = entry.Anchor;
                return true;
            }

            if (TryResolveFallbackSocket(socketType, out anchor))
                return true;

            anchor = transform;
            return socketType == CharacterPresentationSocketType.Root ||
                   socketType == CharacterPresentationSocketType.None;
        }

        private void AutoWireReferences()
        {
            if (visualRoot == null)
            {
                var child = transform.Find("VisualRoot");
                visualRoot = child != null ? child : transform;
            }
        }

        private bool TryResolveFallbackSocket(CharacterPresentationSocketType socketType, out Transform anchor)
        {
            switch (socketType)
            {
                case CharacterPresentationSocketType.None:
                case CharacterPresentationSocketType.Root:
                    anchor = transform;
                    return true;

                case CharacterPresentationSocketType.VisualRoot:
                    anchor = visualRoot != null ? visualRoot : transform;
                    return true;

                case CharacterPresentationSocketType.Weapon:
                    return TryFindNamedChild(new[] { "WeaponSocket", "Weapon", "SwordSocket" }, out anchor);

                case CharacterPresentationSocketType.HandLeft:
                    return TryFindNamedChild(new[] { "HandL", "LeftHand", "HandLeft", "PalmL" }, out anchor);

                case CharacterPresentationSocketType.HandRight:
                    return TryFindNamedChild(new[] { "HandR", "RightHand", "HandRight", "PalmR" }, out anchor);

                case CharacterPresentationSocketType.Chest:
                    return TryFindNamedChild(new[] { "Chest", "Body", "Spine" }, out anchor);

                case CharacterPresentationSocketType.Head:
                    return TryFindNamedChild(new[] { "Head", "HeadSocket" }, out anchor);

                case CharacterPresentationSocketType.Ground:
                    anchor = transform;
                    return true;

                case CharacterPresentationSocketType.TargetCenter:
                    anchor = transform;
                    return true;
            }

            anchor = null;
            return false;
        }

        private bool TryFindNamedChild(string[] candidateNames, out Transform child)
        {
            for (var i = 0; i < candidateNames.Length; i++)
            {
                var resolved = FindDeepChild(transform, candidateNames[i]);
                if (resolved == null)
                    continue;

                child = resolved;
                return true;
            }

            child = null;
            return false;
        }

        private static Transform FindDeepChild(Transform parent, string childName)
        {
            if (parent == null || string.IsNullOrWhiteSpace(childName))
                return null;

            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (string.Equals(child.name, childName, StringComparison.OrdinalIgnoreCase))
                    return child;

                var nested = FindDeepChild(child, childName);
                if (nested != null)
                    return nested;
            }

            return null;
        }
    }
}
