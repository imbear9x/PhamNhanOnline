using System;
using System.Collections.Generic;
using GameShared.Models;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
            [SerializeField] private string skillGroupName = string.Empty;
            [SerializeField] private SkillPresentationArchetype archetype = SkillPresentationArchetype.None;
            [SerializeField] private string castStateName = string.Empty;
            [SerializeField] private string releaseStateName = string.Empty;
            [SerializeField] private string targetImpactStateName = string.Empty;
            [SerializeField] private CharacterPresentationSocketType sourceSocket = CharacterPresentationSocketType.Root;
            [SerializeField] private CharacterPresentationSocketType impactSocket = CharacterPresentationSocketType.TargetCenter;

            [Header("FX")]
            [SerializeField] private GameObject castFxPrefab;
            [SerializeField] private GameObject releaseFxPrefab;
            [SerializeField] private GameObject impactFxPrefab;
            [SerializeField] private float fxLifetimeSeconds = 1.5f;

            [Header("Behavior")]
            [SerializeField] private bool faceTargetOnCast = true;

            [Header("UI")]
            [SerializeField] private Sprite iconSprite;

            public string SkillGroupCode => skillGroupCode;
            public string SkillGroupName => skillGroupName;
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
            public Sprite IconSprite => iconSprite;

#if UNITY_EDITOR
            internal string EditorSkillGroupCode
            {
                get => skillGroupCode;
                set => skillGroupCode = value ?? string.Empty;
            }

            internal string EditorSkillGroupName
            {
                get => skillGroupName;
                set => skillGroupName = value ?? string.Empty;
            }

            internal Sprite EditorIconSprite
            {
                get => iconSprite;
                set => iconSprite = value;
            }
#endif
        }

        [Header("Fallback")]
        [SerializeField] private SkillPresentationArchetype defaultArchetype = SkillPresentationArchetype.MeleeWeaponSwing;

        [Header("UI")]
        [SerializeField] private Sprite defaultIconSprite;

        [Header("Skill Group Presets")]
        [SerializeField] private List<SkillGroupPresetEntry> skillGroupPresets =
            new List<SkillGroupPresetEntry>();

        [Header("Skill Overrides")]
        [SerializeField] private List<SkillWorldPresentationDefinition> skillOverrides =
            new List<SkillWorldPresentationDefinition>();

        public SkillWorldPresentationDefinition Resolve(SkillPresentationLookupContext context)
        {
            SkillGroupPresetEntry preset;
            if (TryResolvePreset(context.SkillGroupCode, out preset))
            {
                SkillWorldPresentationDefinition resolved;
                if (TryResolveSkillOverride(context.SkillId, context.SkillCode, out resolved))
                    return resolved;

                return BuildDefinitionFromPreset(context, preset);
            }

            SkillWorldPresentationDefinition fallbackOverride;
            if (TryResolveSkillOverride(context.SkillId, context.SkillCode, out fallbackOverride))
                return fallbackOverride;

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
                null,
                null,
                null,
                1.5f,
                true);
        }

        private bool TryResolveSkillOverride(
            int skillId,
            string skillCode,
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
            }

            definition = null;
            return false;
        }

        private static SkillWorldPresentationDefinition BuildDefinitionFromPreset(
            SkillPresentationLookupContext context,
            SkillGroupPresetEntry preset)
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
                preset.CastFxPrefab,
                preset.ReleaseFxPrefab,
                preset.ImpactFxPrefab,
                preset.FxLifetimeSeconds,
                preset.FaceTargetOnCast);
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

        public Sprite ResolveIcon(PlayerSkillModel skill)
        {
            SkillWorldPresentationDefinition overrideDefinition;
            if (TryResolveSkillOverride(skill.SkillId, skill.Code, out overrideDefinition))
            {
                if (overrideDefinition.IconSprite != null)
                    return overrideDefinition.IconSprite;
            }

            SkillGroupPresetEntry preset;
            if (TryResolvePreset(skill.SkillGroupCode, out preset) && preset.IconSprite != null)
                return preset.IconSprite;

            return defaultIconSprite;
        }

#if UNITY_EDITOR
        public EditorSyncSummary ApplyDatabaseSync(IReadOnlyList<EditorDatabaseSkillRecord> databaseSkills)
        {
            if (databaseSkills == null)
                throw new ArgumentNullException(nameof(databaseSkills));

            var validSkillsById = new Dictionary<int, EditorDatabaseSkillRecord>();
            var validSkillsByCode =
                new Dictionary<string, EditorDatabaseSkillRecord>(StringComparer.OrdinalIgnoreCase);
            var orderedGroups =
                new SortedDictionary<string, EditorDatabaseSkillGroupRecord>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < databaseSkills.Count; i++)
            {
                var entry = databaseSkills[i];
                if (entry == null)
                    continue;

                var normalizedGroupCode = NormalizeKey(entry.SkillGroupCode);
                var normalizedSkillCode = NormalizeKey(entry.SkillCode);
                if (string.IsNullOrEmpty(normalizedGroupCode) || string.IsNullOrEmpty(normalizedSkillCode))
                    continue;

                validSkillsById[entry.SkillId] = entry;
                validSkillsByCode[normalizedSkillCode] = entry;

                if (!orderedGroups.ContainsKey(normalizedGroupCode))
                {
                    orderedGroups.Add(
                        normalizedGroupCode,
                        new EditorDatabaseSkillGroupRecord(
                            entry.SkillGroupCode.Trim(),
                            entry.Name.Trim()));
                }
            }

            Undo.RecordObject(this, "Sync Skill World Presentation Catalog");

            var summary = new EditorSyncSummary
            {
                DatabaseSkillCount = validSkillsById.Count,
                DatabaseSkillGroupCount = orderedGroups.Count
            };

            summary.AddedGroupCount = RebuildSkillGroupPresets(orderedGroups, out var removedGroupCount);
            summary.RemovedGroupCount = removedGroupCount;
            summary.RemovedOverrideCount = RebuildSkillOverrides(validSkillsById, validSkillsByCode, out var normalizedOverrideCount);
            summary.NormalizedOverrideCount = normalizedOverrideCount;

            EditorUtility.SetDirty(this);
            return summary;
        }

        private int RebuildSkillGroupPresets(
            SortedDictionary<string, EditorDatabaseSkillGroupRecord> orderedGroups,
            out int removedGroupCount)
        {
            var existingByGroup = new Dictionary<string, SkillGroupPresetEntry>(StringComparer.OrdinalIgnoreCase);
            removedGroupCount = 0;
            for (var i = 0; i < skillGroupPresets.Count; i++)
            {
                var entry = skillGroupPresets[i];
                if (entry == null)
                {
                    removedGroupCount++;
                    continue;
                }

                var normalizedKey = NormalizeKey(entry.EditorSkillGroupCode);
                if (string.IsNullOrEmpty(normalizedKey))
                {
                    removedGroupCount++;
                    continue;
                }

                if (existingByGroup.ContainsKey(normalizedKey))
                {
                    removedGroupCount++;
                    continue;
                }

                existingByGroup.Add(normalizedKey, entry);
            }

            var rebuilt = new List<SkillGroupPresetEntry>(orderedGroups.Count);
            var addedCount = 0;
            foreach (var pair in orderedGroups)
            {
                if (existingByGroup.TryGetValue(pair.Key, out var existing))
                {
                    existing.EditorSkillGroupCode = pair.Value.SkillGroupCode;
                    existing.EditorSkillGroupName = pair.Value.SkillGroupName;
                    rebuilt.Add(existing);
                    continue;
                }

                rebuilt.Add(new SkillGroupPresetEntry
                {
                    EditorSkillGroupCode = pair.Value.SkillGroupCode,
                    EditorSkillGroupName = pair.Value.SkillGroupName
                });
                addedCount++;
            }

            removedGroupCount += Math.Max(0, existingByGroup.Count - rebuilt.Count + addedCount);
            skillGroupPresets = rebuilt;
            return addedCount;
        }

        private int RebuildSkillOverrides(
            IReadOnlyDictionary<int, EditorDatabaseSkillRecord> validSkillsById,
            IReadOnlyDictionary<string, EditorDatabaseSkillRecord> validSkillsByCode,
            out int normalizedOverrideCount)
        {
            normalizedOverrideCount = 0;

            var existingValidOverrides = new List<(SkillWorldPresentationDefinition Definition, EditorDatabaseSkillRecord Record)>();
            var seenSkillIds = new HashSet<int>();
            var removedOverrideCount = 0;

            for (var i = 0; i < skillOverrides.Count; i++)
            {
                var definition = skillOverrides[i];
                if (definition == null)
                {
                    removedOverrideCount++;
                    continue;
                }

                if (!TryMatchDatabaseSkill(definition, validSkillsById, validSkillsByCode, out var matchedRecord))
                {
                    removedOverrideCount++;
                    continue;
                }

                if (!seenSkillIds.Add(matchedRecord.SkillId))
                {
                    removedOverrideCount++;
                    continue;
                }

                if (NormalizeOverride(definition, matchedRecord))
                    normalizedOverrideCount++;

                existingValidOverrides.Add((definition, matchedRecord));
            }

            existingValidOverrides.Sort(static (left, right) =>
            {
                var groupComparison = string.Compare(
                    left.Record.SkillGroupCode,
                    right.Record.SkillGroupCode,
                    StringComparison.OrdinalIgnoreCase);
                if (groupComparison != 0)
                    return groupComparison;

                var levelComparison = left.Record.SkillLevel.CompareTo(right.Record.SkillLevel);
                if (levelComparison != 0)
                    return levelComparison;

                return left.Record.SkillId.CompareTo(right.Record.SkillId);
            });

            skillOverrides = new List<SkillWorldPresentationDefinition>(existingValidOverrides.Count);
            for (var i = 0; i < existingValidOverrides.Count; i++)
                skillOverrides.Add(existingValidOverrides[i].Definition);

            return removedOverrideCount;
        }

        private static bool TryMatchDatabaseSkill(
            SkillWorldPresentationDefinition definition,
            IReadOnlyDictionary<int, EditorDatabaseSkillRecord> validSkillsById,
            IReadOnlyDictionary<string, EditorDatabaseSkillRecord> validSkillsByCode,
            out EditorDatabaseSkillRecord record)
        {
            if (definition.EditorSkillId > 0 && validSkillsById.TryGetValue(definition.EditorSkillId, out record))
                return true;

            var normalizedSkillCode = NormalizeKey(definition.EditorSkillCode);
            if (!string.IsNullOrEmpty(normalizedSkillCode) && validSkillsByCode.TryGetValue(normalizedSkillCode, out record))
                return true;

            record = null;
            return false;
        }

        private static bool NormalizeOverride(
            SkillWorldPresentationDefinition definition,
            EditorDatabaseSkillRecord matchedRecord)
        {
            var changed = false;
            if (definition.EditorSkillId != matchedRecord.SkillId)
            {
                definition.EditorSkillId = matchedRecord.SkillId;
                changed = true;
            }

            var canonicalSkillCode = matchedRecord.SkillCode.Trim();
            if (!string.Equals(definition.EditorSkillCode, canonicalSkillCode, StringComparison.Ordinal))
            {
                definition.EditorSkillCode = canonicalSkillCode;
                changed = true;
            }

            var canonicalGroupCode = matchedRecord.SkillGroupCode.Trim();
            if (!string.Equals(definition.EditorSkillGroupCode, canonicalGroupCode, StringComparison.Ordinal))
            {
                definition.EditorSkillGroupCode = canonicalGroupCode;
                changed = true;
            }

            return changed;
        }

        private static string NormalizeKey(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant();
        }

        [Serializable]
        public sealed class EditorDatabaseSkillRecord
        {
            [SerializeField] private int skillId;
            [SerializeField] private string skillCode = string.Empty;
            [SerializeField] private string skillGroupCode = string.Empty;
            [SerializeField] private int skillLevel;
            [SerializeField] private string name = string.Empty;

            public int SkillId => skillId;
            public string SkillCode => skillCode;
            public string SkillGroupCode => skillGroupCode;
            public int SkillLevel => skillLevel;
            public string Name => name;
        }

        public struct EditorSyncSummary
        {
            public int DatabaseSkillCount;
            public int DatabaseSkillGroupCount;
            public int AddedGroupCount;
            public int RemovedGroupCount;
            public int RemovedOverrideCount;
            public int NormalizedOverrideCount;
        }

        private readonly struct EditorDatabaseSkillGroupRecord
        {
            public EditorDatabaseSkillGroupRecord(string skillGroupCode, string skillGroupName)
            {
                SkillGroupCode = skillGroupCode ?? string.Empty;
                SkillGroupName = skillGroupName ?? string.Empty;
            }

            public string SkillGroupCode { get; }

            public string SkillGroupName { get; }
        }
#endif
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

        [Header("UI")]
        [SerializeField] private Sprite iconSprite;

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
        public Sprite IconSprite => iconSprite;

#if UNITY_EDITOR
        internal int EditorSkillId
        {
            get => skillId;
            set => skillId = value;
        }

        internal string EditorSkillCode
        {
            get => skillCode;
            set => skillCode = value ?? string.Empty;
        }

        internal string EditorSkillGroupCode
        {
            get => skillGroupCode;
            set => skillGroupCode = value ?? string.Empty;
        }

        internal Sprite EditorIconSprite
        {
            get => iconSprite;
            set => iconSprite = value;
        }
#endif

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
            GameObject castFxPrefab,
            GameObject releaseFxPrefab,
            GameObject impactFxPrefab,
            float fxLifetimeSeconds,
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
                castFxPrefab = castFxPrefab,
                releaseFxPrefab = releaseFxPrefab,
                impactFxPrefab = impactFxPrefab,
                fxLifetimeSeconds = fxLifetimeSeconds,
                faceTargetOnCast = faceTargetOnCast,
                iconSprite = null
            };
        }
    }
}
