using System;
using System.Collections.Generic;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    [CreateAssetMenu(
        fileName = "ClientMapCatalog",
        menuName = "PhamNhanOnline/World/Client Map Catalog")]
    public sealed class ClientMapCatalog : ScriptableObject
    {
        [SerializeField] private List<ClientMapCatalogEntry> entries = new List<ClientMapCatalogEntry>();

        public IReadOnlyList<ClientMapCatalogEntry> Entries
        {
            get { return entries; }
        }

        public bool TryGetEntry(string clientMapKey, out ClientMapCatalogEntry entry)
        {
            if (string.IsNullOrWhiteSpace(clientMapKey))
            {
                entry = null;
                return false;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                var current = entries[i];
                if (current == null)
                    continue;

                if (!string.Equals(current.ClientMapKey, clientMapKey, StringComparison.Ordinal))
                    continue;

                entry = current;
                return true;
            }

            entry = null;
            return false;
        }

        public bool TryGetMapPrefab(string clientMapKey, out GameObject mapPrefab)
        {
            ClientMapCatalogEntry entry;
            if (TryGetEntry(clientMapKey, out entry) && entry.MapPrefab != null)
            {
                mapPrefab = entry.MapPrefab;
                return true;
            }

            mapPrefab = null;
            return false;
        }
    }
}