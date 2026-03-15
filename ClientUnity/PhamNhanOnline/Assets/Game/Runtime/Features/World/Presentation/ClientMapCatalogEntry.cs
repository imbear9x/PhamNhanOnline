using System;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    [Serializable]
    public sealed class ClientMapCatalogEntry
    {
        [SerializeField] private string clientMapKey = string.Empty;
        [SerializeField] private GameObject mapPrefab;

        public string ClientMapKey
        {
            get { return clientMapKey; }
        }

        public GameObject MapPrefab
        {
            get { return mapPrefab; }
        }
    }
}