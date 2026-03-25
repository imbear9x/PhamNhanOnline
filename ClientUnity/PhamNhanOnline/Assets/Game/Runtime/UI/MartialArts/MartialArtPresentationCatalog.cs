using System;
using System.Collections.Generic;
using GameShared.Models;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.MartialArts
{
    [CreateAssetMenu(
        fileName = "MartialArtPresentationCatalog",
        menuName = "PhamNhanOnline/UI/Martial Art Presentation Catalog")]
    public sealed class MartialArtPresentationCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class SpriteKeyEntry
        {
            [SerializeField] private string key = string.Empty;
            [SerializeField] private Sprite sprite;

            public string Key => key;
            public Sprite Sprite => sprite;
        }

        [Serializable]
        public sealed class MartialArtOverrideEntry
        {
            [SerializeField] private int martialArtId;
            [SerializeField] private Sprite iconSprite;

            public int MartialArtId => martialArtId;
            public Sprite IconSprite => iconSprite;
        }

        [Header("Fallbacks")]
        [SerializeField] private Sprite defaultIconSprite;

        [Header("Direct Key Mapping")]
        [SerializeField] private List<SpriteKeyEntry> iconEntries = new List<SpriteKeyEntry>();

        [Header("Martial Art Overrides")]
        [SerializeField] private List<MartialArtOverrideEntry> martialArtOverrides = new List<MartialArtOverrideEntry>();

        public MartialArtPresentation Resolve(PlayerMartialArtModel martialArt)
        {
            return new MartialArtPresentation(ResolveIcon(martialArt));
        }

        private Sprite ResolveIcon(PlayerMartialArtModel martialArt)
        {
            MartialArtOverrideEntry overrideEntry;
            if (TryGetOverride(martialArt.MartialArtId, out overrideEntry) && overrideEntry.IconSprite != null)
                return overrideEntry.IconSprite;

            Sprite sprite;
            if (TryResolveByKey(iconEntries, martialArt.Icon, out sprite))
                return sprite;

            if (TryResolveByKey(iconEntries, martialArt.Code, out sprite))
                return sprite;

            return defaultIconSprite;
        }

        private bool TryGetOverride(int martialArtId, out MartialArtOverrideEntry entry)
        {
            for (var i = 0; i < martialArtOverrides.Count; i++)
            {
                var current = martialArtOverrides[i];
                if (current == null || current.MartialArtId != martialArtId)
                    continue;

                entry = current;
                return true;
            }

            entry = null;
            return false;
        }

        private static bool TryResolveByKey(IReadOnlyList<SpriteKeyEntry> entries, string key, out Sprite sprite)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                for (var i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    if (entry == null || entry.Sprite == null)
                        continue;

                    if (!string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase))
                        continue;

                    sprite = entry.Sprite;
                    return true;
                }
            }

            sprite = null;
            return false;
        }
    }

    public readonly struct MartialArtPresentation
    {
        public MartialArtPresentation(Sprite iconSprite)
        {
            IconSprite = iconSprite;
        }

        public Sprite IconSprite { get; }
    }
}
