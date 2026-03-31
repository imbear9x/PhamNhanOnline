using System;
using System.Collections.Generic;
using UnityEngine;

namespace PhamNhanOnline.Client.Infrastructure.Pooling
{
    [DisallowMultipleComponent]
    public sealed class ClientPoolService : MonoBehaviour
    {
        [Header("Behavior")]
        [SerializeField] private bool persistentAcrossScenes = true;
        [SerializeField] private int minimumInactiveInstancesPerPool = 1;
        [SerializeField] private float retainedPeakFraction = 0.5f;
        [SerializeField] private float trimCheckIntervalSeconds = 10f;
        [SerializeField] private float trimIdleDelaySeconds = 15f;

        [Header("References")]
        [SerializeField] private Transform poolStorageRoot;

        private readonly Dictionary<int, PrefabPool> poolsByPrefabId = new Dictionary<int, PrefabPool>();
        private float timeSinceLastTrimCheck;

        public static ClientPoolService Instance { get; private set; }

        public static ClientPoolService Ensure(Transform optionalParent = null)
        {
            if (Instance != null)
                return Instance;

            var host = new GameObject("__ClientPoolService");
            if (optionalParent != null)
                host.transform.SetParent(optionalParent, false);

            return host.AddComponent<ClientPoolService>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureStorageRoot();
            if (persistentAcrossScenes)
            {
                if (transform.parent != null)
                    transform.SetParent(null, false);

                DontDestroyOnLoad(gameObject);
            }
        }

        private void Update()
        {
            timeSinceLastTrimCheck += Time.unscaledDeltaTime;
            if (timeSinceLastTrimCheck < trimCheckIntervalSeconds)
                return;

            timeSinceLastTrimCheck = 0f;
            TrimIdlePools();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public T Spawn<T>(T prefab, Transform parent = null, bool worldPositionStays = false)
            where T : Component
        {
            if (prefab == null)
                return null;

            var pool = GetOrCreatePool(prefab.gameObject);
            return pool.Spawn<T>(parent, worldPositionStays);
        }

        public GameObject Spawn(GameObject prefab, Transform parent = null, bool worldPositionStays = false)
        {
            if (prefab == null)
                return null;

            var pool = GetOrCreatePool(prefab);
            var instance = pool.Spawn<GameObject>(parent, worldPositionStays);
            return instance;
        }

        public void Warm(GameObject prefab, int count)
        {
            if (prefab == null || count <= 0)
                return;

            var pool = GetOrCreatePool(prefab);
            pool.Warm(count);
        }

        public void Release(Component component)
        {
            if (component == null)
                return;

            Release(component.gameObject);
        }

        public void Release(GameObject instance)
        {
            if (instance == null)
                return;

            var pooledInstance = instance.GetComponent<PooledInstance>();
            if (pooledInstance != null)
            {
                pooledInstance.Release();
                return;
            }

            Destroy(instance);
        }

        private void TrimIdlePools()
        {
            if (poolsByPrefabId.Count == 0)
                return;

            var idleCutoffTime = Time.unscaledTime - Mathf.Max(0f, trimIdleDelaySeconds);
            foreach (var pair in poolsByPrefabId)
            {
                var pool = pair.Value;
                if (pool.LastUsedAtUnscaledTime > idleCutoffTime)
                    continue;

                var targetInactiveCount = Mathf.Max(
                    minimumInactiveInstancesPerPool,
                    Mathf.CeilToInt(pool.PeakActiveCount * Mathf.Clamp01(retainedPeakFraction)));
                pool.TrimToInactive(targetInactiveCount);
            }
        }

        private PrefabPool GetOrCreatePool(GameObject prefab)
        {
            var prefabId = prefab.GetInstanceID();
            if (poolsByPrefabId.TryGetValue(prefabId, out var existingPool))
                return existingPool;

            EnsureStorageRoot();
            var poolRoot = new GameObject(prefab.name + "_Pool").transform;
            poolRoot.SetParent(poolStorageRoot, false);
            poolRoot.gameObject.SetActive(false);

            var createdPool = new PrefabPool(prefab, poolRoot);
            poolsByPrefabId[prefabId] = createdPool;
            return createdPool;
        }

        private void EnsureStorageRoot()
        {
            if (poolStorageRoot != null)
                return;

            var storage = new GameObject("PoolStorage");
            storage.transform.SetParent(transform, false);
            storage.SetActive(false);
            poolStorageRoot = storage.transform;
        }

        internal sealed class PrefabPool
        {
            private readonly GameObject prefab;
            private readonly Transform storageRoot;
            private readonly Stack<PooledInstance> inactiveInstances = new Stack<PooledInstance>();
            private readonly HashSet<PooledInstance> activeInstances = new HashSet<PooledInstance>();

            public PrefabPool(GameObject prefab, Transform storageRoot)
            {
                this.prefab = prefab;
                this.storageRoot = storageRoot;
            }

            public int PeakActiveCount { get; private set; }
            public float LastUsedAtUnscaledTime { get; private set; }

            public T Spawn<T>(Transform parent, bool worldPositionStays)
            {
                var pooledInstance = inactiveInstances.Count > 0
                    ? inactiveInstances.Pop()
                    : CreateInstance();

                if (pooledInstance == null)
                    return default;

                LastUsedAtUnscaledTime = Time.unscaledTime;
                activeInstances.Add(pooledInstance);
                PeakActiveCount = Mathf.Max(PeakActiveCount, activeInstances.Count);

                var instanceTransform = pooledInstance.transform;
                if (parent != null)
                    instanceTransform.SetParent(parent, worldPositionStays);
                else
                    instanceTransform.SetParent(null, worldPositionStays);

                pooledInstance.gameObject.SetActive(true);

                if (typeof(T) == typeof(GameObject))
                    return (T)(object)pooledInstance.gameObject;

                return pooledInstance.GetComponent<T>();
            }

            public void Warm(int count)
            {
                for (var i = inactiveInstances.Count; i < count; i++)
                {
                    var instance = CreateInstance();
                    if (instance == null)
                        break;

                    Release(instance);
                }
            }

            public void Release(PooledInstance pooledInstance)
            {
                if (pooledInstance == null)
                    return;

                LastUsedAtUnscaledTime = Time.unscaledTime;
                activeInstances.Remove(pooledInstance);
                pooledInstance.transform.SetParent(storageRoot, false);
                pooledInstance.gameObject.SetActive(false);
                inactiveInstances.Push(pooledInstance);
            }

            public void TrimToInactive(int targetInactiveCount)
            {
                targetInactiveCount = Mathf.Max(0, targetInactiveCount);
                while (inactiveInstances.Count > targetInactiveCount)
                {
                    var instance = inactiveInstances.Pop();
                    if (instance != null)
                        UnityEngine.Object.Destroy(instance.gameObject);
                }

                PeakActiveCount = Mathf.Min(PeakActiveCount, activeInstances.Count + inactiveInstances.Count);
            }

            private PooledInstance CreateInstance()
            {
                if (prefab == null)
                    return null;

                var instance = UnityEngine.Object.Instantiate(prefab, storageRoot, false);
                instance.name = prefab.name + "_Pooled";
                var pooledInstance = instance.GetComponent<PooledInstance>();
                if (pooledInstance == null)
                    pooledInstance = instance.AddComponent<PooledInstance>();
                pooledInstance.Bind(this);
                instance.SetActive(false);
                return pooledInstance;
            }
        }
    }
}
