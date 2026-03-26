using System;
using System.Collections.Generic;
using GameShared.Models;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    [CreateAssetMenu(
        fileName = "EnemyPresentationCatalog",
        menuName = "PhamNhanOnline/World/Enemy Presentation Catalog")]
    public sealed class EnemyPresentationCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class EnemyKeyEntry
        {
            [SerializeField] private string code = string.Empty;
            [SerializeField] private GameObject prefab;

            public string Code { get { return code; } }
            public GameObject Prefab { get { return prefab; } }
        }

        [Serializable]
        public sealed class EnemyTemplateOverrideEntry
        {
            [SerializeField] private int enemyTemplateId;
            [SerializeField] private GameObject prefab;

            public int EnemyTemplateId { get { return enemyTemplateId; } }
            public GameObject Prefab { get { return prefab; } }
        }

        [Header("Fallback")]
        [SerializeField] private GameObject defaultEnemyPrefab;

        [Header("Code Mapping")]
        [SerializeField] private List<EnemyKeyEntry> codeEntries = new List<EnemyKeyEntry>();

        [Header("Template Overrides")]
        [SerializeField] private List<EnemyTemplateOverrideEntry> templateOverrides = new List<EnemyTemplateOverrideEntry>();

        public bool TryResolvePrefab(EnemyRuntimeModel enemy, out GameObject prefab)
        {
            EnemyTemplateOverrideEntry overrideEntry;
            if (TryGetOverride(enemy.EnemyTemplateId, out overrideEntry) && overrideEntry.Prefab != null)
            {
                prefab = overrideEntry.Prefab;
                return true;
            }

            GameObject resolvedPrefab;
            if (TryResolveByCode(enemy.Code, out resolvedPrefab))
            {
                prefab = resolvedPrefab;
                return true;
            }

            prefab = defaultEnemyPrefab;
            return prefab != null;
        }

        private bool TryGetOverride(int enemyTemplateId, out EnemyTemplateOverrideEntry entry)
        {
            for (var i = 0; i < templateOverrides.Count; i++)
            {
                var current = templateOverrides[i];
                if (current == null || current.EnemyTemplateId != enemyTemplateId)
                    continue;

                entry = current;
                return true;
            }

            entry = null;
            return false;
        }

        private bool TryResolveByCode(string code, out GameObject prefab)
        {
            if (!string.IsNullOrWhiteSpace(code))
            {
                for (var i = 0; i < codeEntries.Count; i++)
                {
                    var entry = codeEntries[i];
                    if (entry == null || entry.Prefab == null)
                        continue;

                    if (!string.Equals(entry.Code, code, StringComparison.OrdinalIgnoreCase))
                        continue;

                    prefab = entry.Prefab;
                    return true;
                }
            }

            prefab = null;
            return false;
        }
    }
}
