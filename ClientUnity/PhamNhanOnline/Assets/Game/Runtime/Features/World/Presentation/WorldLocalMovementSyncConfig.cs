using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    [CreateAssetMenu(
        fileName = "WorldLocalMovementSyncConfig",
        menuName = "Game/World/Local Movement Sync Config")]
    public sealed class WorldLocalMovementSyncConfig : ScriptableObject
    {
        [Header("Sync Policy")]
        [SerializeField] private float minSyncIntervalSeconds = 0.10f;
        [SerializeField] private float maxSyncIntervalSeconds = 0.20f;
        [SerializeField] private float immediateStateChangeSyncIntervalSeconds = 0.05f;
        [SerializeField] private float syncDistanceThreshold = 10f;
        [SerializeField] private float movingDetectionThreshold = 0.25f;
        [SerializeField] private float finalStopSyncThreshold = 0.5f;

        public float MinSyncIntervalSeconds => minSyncIntervalSeconds;

        public float MaxSyncIntervalSeconds => maxSyncIntervalSeconds;

        public float ImmediateStateChangeSyncIntervalSeconds => immediateStateChangeSyncIntervalSeconds;

        public float SyncDistanceThreshold => syncDistanceThreshold;

        public float MovingDetectionThreshold => movingDetectionThreshold;

        public float FinalStopSyncThreshold => finalStopSyncThreshold;

        public static WorldLocalMovementSyncConfig CreateRuntimeDefaults()
        {
            var config = CreateInstance<WorldLocalMovementSyncConfig>();
            return config;
        }
    }
}
