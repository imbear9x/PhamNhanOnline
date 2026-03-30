using UnityEngine;
using UnityEngine.Serialization;

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

        [Tooltip("Quang duong toi thieu theo don vi map/server ma nhan vat phai di duoc truoc khi client gui mot ban cap nhat vi tri moi. Vi du map rong 1000 thi gia tri 10 nghia la di duoc 1% be ngang map logic moi gui tiep.")]
        [FormerlySerializedAs("syncDistanceThreshold")]
        [SerializeField] private float syncDistanceThresholdMapUnits = 10f;

        [Tooltip("Nguong quang duong toi thieu theo don vi map/server de client coi nhan vat la dang di chuyen. Dung de tach giua dao dong nho va di chuyen that trong he quy chieu map logic.")]
        [FormerlySerializedAs("movingDetectionThreshold")]
        [SerializeField] private float movingDetectionThresholdMapUnits = 0.25f;

        [Tooltip("Cua so thoi gian ngan de cong don quang duong di chuyen khi xac dinh nhan vat co dang di chuyen hay khong. Vi du 0.2 giay nghia la client se nhin quang duong tich luy trong 0.2 giay gan nhat.")]
        [SerializeField] private float movingDetectionWindowSeconds = 0.20f;

        [Tooltip("Nguong quang duong toi thieu theo don vi map/server de gui ban cap nhat cuoi cung khi nhan vat vua dung lai. Dung de server chot vi tri cuoi cho on dinh trong he quy chieu map.")]
        [FormerlySerializedAs("finalStopSyncThreshold")]
        [SerializeField] private float finalStopSyncThresholdMapUnits = 0.5f;

        public float MinSyncIntervalSeconds => minSyncIntervalSeconds;

        public float MaxSyncIntervalSeconds => maxSyncIntervalSeconds;

        public float ImmediateStateChangeSyncIntervalSeconds => immediateStateChangeSyncIntervalSeconds;

        public float SyncDistanceThresholdMapUnits => syncDistanceThresholdMapUnits;

        public float MovingDetectionThresholdMapUnits => movingDetectionThresholdMapUnits;

        public float MovingDetectionWindowSeconds => movingDetectionWindowSeconds;

        public float FinalStopSyncThresholdMapUnits => finalStopSyncThresholdMapUnits;

        public static WorldLocalMovementSyncConfig CreateRuntimeDefaults()
        {
            var config = CreateInstance<WorldLocalMovementSyncConfig>();
            return config;
        }
    }
}
