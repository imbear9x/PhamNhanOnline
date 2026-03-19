using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    [CreateAssetMenu(
        fileName = "WorldLocalMovementSyncConfig",
        menuName = "Game/World/Local Movement Sync Config")]
    public sealed class WorldLocalMovementSyncConfig : ScriptableObject
    {
        [Header("Sync Policy")]
        [Tooltip("Khoang thoi gian ngan nhat giua 2 lan gui vi tri len server khi nhan vat dang di chuyen binh thuong. Giam gia tri nay thi dong bo muot hon nhung tang tan suat gui packet.")]
        [SerializeField] private float minSyncIntervalSeconds = 0.10f;

        [Tooltip("Khoang thoi gian toi da duoc phep im lang truoc khi client bat buoc gui lai vi tri trong luc dang di chuyen. Dung de tranh dang chay ma lau khong gui cap nhat.")]
        [SerializeField] private float maxSyncIntervalSeconds = 0.20f;

        [Tooltip("Khoang thoi gian toi thieu de gui ngay mot ban dong bo khi co thay doi trang thai quan trong nhu bat dau di, dung lai, doi huong hoac doi phase movement.")]
        [SerializeField] private float immediateStateChangeSyncIntervalSeconds = 0.05f;

        [Tooltip("Quang duong toi thieu trong he toa do server ma nhan vat phai di duoc truoc khi client gui mot ban cap nhat vi tri moi. Tang gia tri nay thi giam packet, giam gia tri nay thi vi tri tren server sat hon.")]
        [SerializeField] private float syncDistanceThreshold = 10f;

        [Tooltip("Nguong quang duong toi thieu de client coi nhan vat la dang di chuyen. Dung de tach giua rung nhe/dao dong va di chuyen that.")]
        [SerializeField] private float movingDetectionThreshold = 0.25f;

        [Tooltip("Nguong quang duong toi thieu de gui ban cap nhat cuoi cung khi nhan vat vua dung lai. Dung de server chot vi tri cuoi cho on dinh, tranh lech nho luc tha phim.")]
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
