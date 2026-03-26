using System;
using System.Collections.Generic;
using GameShared.Models;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.Skills
{
    [CreateAssetMenu(
        fileName = "SkillPresentationCatalog",
        menuName = "PhamNhanOnline/UI/Skill Presentation Catalog")]
    public sealed class SkillPresentationCatalog : ScriptableObject
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
        public sealed class SkillOverrideEntry
        {
            [SerializeField] private int skillId;
            [SerializeField] private Sprite iconSprite;

            public int SkillId => skillId;
            public Sprite IconSprite => iconSprite;
        }

        [Header("Fallbacks")]
        [SerializeField] private Sprite defaultIconSprite;

        [Header("Direct Key Mapping")]
        [SerializeField] private List<SpriteKeyEntry> iconEntries = new List<SpriteKeyEntry>();

        [Header("Skill Overrides")]
        [SerializeField] private List<SkillOverrideEntry> skillOverrides = new List<SkillOverrideEntry>();

        public SkillPresentation Resolve(PlayerSkillModel skill)
        {
            return new SkillPresentation(ResolveIcon(skill));
        }

        private Sprite ResolveIcon(PlayerSkillModel skill)
        {
            SkillOverrideEntry overrideEntry;
            if (TryGetOverride(skill.SkillId, out overrideEntry) && overrideEntry.IconSprite != null)
                return overrideEntry.IconSprite;

            Sprite sprite;
            if (TryResolveByKey(iconEntries, skill.SkillGroupCode, out sprite))
                return sprite;

            if (TryResolveByKey(iconEntries, skill.Code, out sprite))
                return sprite;

            return defaultIconSprite;
        }

        private bool TryGetOverride(int skillId, out SkillOverrideEntry entry)
        {
            for (var i = 0; i < skillOverrides.Count; i++)
            {
                var current = skillOverrides[i];
                if (current == null || current.SkillId != skillId)
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

    public readonly struct SkillPresentation
    {
        public SkillPresentation(Sprite iconSprite)
        {
            IconSprite = iconSprite;
        }

        public Sprite IconSprite { get; }
    }
}
