using System;
using System.Collections.Generic;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.Combat.Presentation
{
    [CreateAssetMenu(
        fileName = "SkillWorldPresentationCatalog",
        menuName = "PhamNhanOnline/Combat/Skill World Presentation Catalog")]
    public sealed class SkillWorldPresentationCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class SkillGroupPresetEntry
        {
            [SerializeField] private string skillGroupCode = string.Empty;
            [SerializeField] private SkillPresentationArchetype archetype = SkillPresentationArchetype.None;
            [SerializeField] private string castStateName = string.Empty;
            [SerializeField] private string releaseStateName = string.Empty;
            [SerializeField] private string targetImpactStateName = string.Empty;
            [SerializeField] private CharacterPresentationSocketType sourceSocket = CharacterPresentationSocketType.Root;
            [SerializeField] private CharacterPresentationSocketType impactSocket = CharacterPresentationSocketType.TargetCenter;
            [SerializeField] private bool faceTargetOnCast = true;

            public string SkillGroupCode => skillGroupCode;
            public SkillPresentationArchetype Archetype => archetype;
            public string CastStateName => castStateName;
            public string ReleaseStateName => releaseStateName;
            public string TargetImpactStateName => targetImpactStateName;
            public CharacterPresentationSocketType SourceSocket => sourceSocket;
            public CharacterPresentationSocketType ImpactSocket => impactSocket;
            public bool FaceTargetOnCast => faceTargetOnCast;
        }

        [Header("Fallback")]
        [SerializeField] private SkillPresentationArchetype defaultArchetype = SkillPresentationArchetype.MeleeWeaponSwing;

        [Header("Skill Overrides")]
        [SerializeField] private List<SkillWorldPresentationDefinition> skillOverrides =
            new List<SkillWorldPresentationDefinition>();

        [Header("Skill Group Presets")]
        [SerializeField] private List<SkillGroupPresetEntry> skillGroupPresets =
            new List<SkillGroupPresetEntry>();

        public SkillWorldPresentationDefinition Resolve(SkillPresentationLookupContext context)
        {
            SkillWorldPresentationDefinition resolved;
            if (TryResolveOverride(context.SkillId, context.SkillCode, context.SkillGroupCode, out resolved))
                return resolved;

            SkillGroupPresetEntry preset;
            if (TryResolvePreset(context.SkillGroupCode, out preset))
            {
                return SkillWorldPresentationDefinition.BuildSynthetic(
                    context.SkillId,
                    context.SkillCode,
                    context.SkillGroupCode,
                    preset.Archetype,
                    preset.CastStateName,
                    preset.ReleaseStateName,
                    preset.TargetImpactStateName,
                    preset.SourceSocket,
                    preset.ImpactSocket,
                    preset.FaceTargetOnCast);
            }

            return SkillWorldPresentationDefinition.BuildSynthetic(
                context.SkillId,
                context.SkillCode,
                context.SkillGroupCode,
                GuessArchetype(context),
                string.Empty,
                string.Empty,
                string.Empty,
                CharacterPresentationSocketType.Root,
                CharacterPresentationSocketType.TargetCenter,
                true);
        }

        private bool TryResolveOverride(
            int skillId,
            string skillCode,
            string skillGroupCode,
            out SkillWorldPresentationDefinition definition)
        {
            for (var i = 0; i < skillOverrides.Count; i++)
            {
                var entry = skillOverrides[i];
                if (entry == null)
                    continue;

                if (entry.SkillId > 0 && entry.SkillId == skillId)
                {
                    definition = entry;
                    return true;
                }

                if (!string.IsNullOrWhiteSpace(skillCode) &&
                    !string.IsNullOrWhiteSpace(entry.SkillCode) &&
                    string.Equals(entry.SkillCode, skillCode, StringComparison.OrdinalIgnoreCase))
                {
                    definition = entry;
                    return true;
                }

                if (!string.IsNullOrWhiteSpace(skillGroupCode) &&
                    !string.IsNullOrWhiteSpace(entry.SkillGroupCode) &&
                    string.Equals(entry.SkillGroupCode, skillGroupCode, StringComparison.OrdinalIgnoreCase))
                {
                    definition = entry;
                    return true;
                }
            }

            definition = null;
            return false;
        }

        private bool TryResolvePreset(string skillGroupCode, out SkillGroupPresetEntry preset)
        {
            if (!string.IsNullOrWhiteSpace(skillGroupCode))
            {
                for (var i = 0; i < skillGroupPresets.Count; i++)
                {
                    var entry = skillGroupPresets[i];
                    if (entry == null)
                        continue;

                    if (!string.Equals(entry.SkillGroupCode, skillGroupCode, StringComparison.OrdinalIgnoreCase))
                        continue;

                    preset = entry;
                    return true;
                }
            }

            preset = null;
            return false;
        }

        private SkillPresentationArchetype GuessArchetype(SkillPresentationLookupContext context)
        {
            var key = !string.IsNullOrWhiteSpace(context.SkillGroupCode)
                ? context.SkillGroupCode
                : context.SkillCode;
            if (string.IsNullOrWhiteSpace(key))
                return defaultArchetype;

            var normalized = key.Trim().ToLowerInvariant();
            if (normalized.Contains("summon") || normalized.Contains("trieu") || normalized.Contains("call"))
                return SkillPresentationArchetype.SummonStrike;
            if (normalized.Contains("buff") || normalized.Contains("self") || normalized.Contains("ho_the"))
                return SkillPresentationArchetype.SelfBuff;
            if (normalized.Contains("projectile") || normalized.Contains("arrow") || normalized.Contains("dan"))
                return SkillPresentationArchetype.WeaponProjectile;
            if (normalized.Contains("chuong") || normalized.Contains("thrust") || normalized.Contains("blast"))
                return SkillPresentationArchetype.HandProjectile;

            return defaultArchetype;
        }
    }

    [Serializable]
    public sealed class SkillWorldPresentationDefinition
    {
        [SerializeField] private int skillId;
        [SerializeField] private string skillCode = string.Empty;
        [SerializeField] private string skillGroupCode = string.Empty;
        [SerializeField] private SkillPresentationArchetype archetype = SkillPresentationArchetype.None;

        [Header("Animation")]
        [SerializeField] private string castStateName = string.Empty;
        [SerializeField] private string releaseStateName = string.Empty;
        [SerializeField] private string targetImpactStateName = string.Empty;

        [Header("Sockets")]
        [SerializeField] private CharacterPresentationSocketType sourceSocket = CharacterPresentationSocketType.Root;
        [SerializeField] private CharacterPresentationSocketType impactSocket = CharacterPresentationSocketType.TargetCenter;

        [Header("FX")]
        [SerializeField] private GameObject castFxPrefab;
        [SerializeField] private GameObject releaseFxPrefab;
        [SerializeField] private GameObject impactFxPrefab;
        [SerializeField] private float fxLifetimeSeconds = 1.5f;

        [Header("Behavior")]
        [SerializeField] private bool faceTargetOnCast = true;

        public int SkillId => skillId;
        public string SkillCode => skillCode;
        public string SkillGroupCode => skillGroupCode;
        public SkillPresentationArchetype Archetype => archetype;
        public string CastStateName => castStateName;
        public string ReleaseStateName => releaseStateName;
        public string TargetImpactStateName => targetImpactStateName;
        public CharacterPresentationSocketType SourceSocket => sourceSocket;
        public CharacterPresentationSocketType ImpactSocket => impactSocket;
        public GameObject CastFxPrefab => castFxPrefab;
        public GameObject ReleaseFxPrefab => releaseFxPrefab;
        public GameObject ImpactFxPrefab => impactFxPrefab;
        public float FxLifetimeSeconds => fxLifetimeSeconds;
        public bool FaceTargetOnCast => faceTargetOnCast;

        public static SkillWorldPresentationDefinition BuildSynthetic(
            int skillId,
            string skillCode,
            string skillGroupCode,
            SkillPresentationArchetype archetype,
            string castStateName,
            string releaseStateName,
            string targetImpactStateName,
            CharacterPresentationSocketType sourceSocket,
            CharacterPresentationSocketType impactSocket,
            bool faceTargetOnCast)
        {
            return new SkillWorldPresentationDefinition
            {
                skillId = skillId,
                skillCode = skillCode ?? string.Empty,
                skillGroupCode = skillGroupCode ?? string.Empty,
                archetype = archetype,
                castStateName = castStateName ?? string.Empty,
                releaseStateName = releaseStateName ?? string.Empty,
                targetImpactStateName = targetImpactStateName ?? string.Empty,
                sourceSocket = sourceSocket,
                impactSocket = impactSocket,
                faceTargetOnCast = faceTargetOnCast
            };
        }
    }
}
