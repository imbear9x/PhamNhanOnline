using System;
using GameShared.Models;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.Potential
{
    [CreateAssetMenu(
        fileName = "PotentialStatPresentationCatalog",
        menuName = "PhamNhanOnline/UI/Potential Stat Presentation Catalog")]
    public sealed class PotentialStatPresentationCatalog : ScriptableObject
    {
        [Serializable]
        private struct Entry
        {
            [SerializeField] private PotentialAllocationTarget target;
            [SerializeField] private string displayName;
            [SerializeField] private Sprite iconSprite;
            [SerializeField] private string valueFormat;
            [SerializeField] private string gainFormat;

            public PotentialAllocationTarget Target => target;
            public string DisplayName => displayName;
            public Sprite IconSprite => iconSprite;
            public string ValueFormat => string.IsNullOrWhiteSpace(valueFormat) ? "0" : valueFormat.Trim();
            public string GainFormat => string.IsNullOrWhiteSpace(gainFormat) ? "0.##" : gainFormat.Trim();
        }

        [Header("Entries")]
        [SerializeField] private Entry[] entries = Array.Empty<Entry>();

        public PotentialStatPresentation Resolve(PotentialAllocationTarget target)
        {
            for (var i = 0; i < entries.Length; i++)
            {
                if (entries[i].Target != target)
                    continue;

                return new PotentialStatPresentation(
                    target,
                    string.IsNullOrWhiteSpace(entries[i].DisplayName) ? GetFallbackDisplayName(target) : entries[i].DisplayName.Trim(),
                    entries[i].IconSprite,
                    entries[i].ValueFormat,
                    entries[i].GainFormat);
            }

            return new PotentialStatPresentation(
                target,
                GetFallbackDisplayName(target),
                null,
                target == PotentialAllocationTarget.BaseFortune ? "0.##" : "0",
                "0.##");
        }

        public static string GetFallbackDisplayName(PotentialAllocationTarget target)
        {
            return target switch
            {
                PotentialAllocationTarget.BaseHp => "HP",
                PotentialAllocationTarget.BaseMp => "MP",
                PotentialAllocationTarget.BaseAttack => "Cong kich",
                PotentialAllocationTarget.BaseSpeed => "Toc do",
                PotentialAllocationTarget.BaseFortune => "Co duyen",
                PotentialAllocationTarget.BaseSpiritualSense => "Than thuc",
                _ => "Khong ro"
            };
        }
    }

    public readonly struct PotentialStatPresentation
    {
        public PotentialStatPresentation(
            PotentialAllocationTarget target,
            string displayName,
            Sprite iconSprite,
            string valueFormat,
            string gainFormat)
        {
            Target = target;
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? PotentialStatPresentationCatalog.GetFallbackDisplayName(target)
                : displayName.Trim();
            IconSprite = iconSprite;
            ValueFormat = string.IsNullOrWhiteSpace(valueFormat) ? "0" : valueFormat.Trim();
            GainFormat = string.IsNullOrWhiteSpace(gainFormat) ? "0.##" : gainFormat.Trim();
        }

        public PotentialAllocationTarget Target { get; }
        public string DisplayName { get; }
        public Sprite IconSprite { get; }
        public string ValueFormat { get; }
        public string GainFormat { get; }
    }
}
