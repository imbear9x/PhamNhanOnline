using UnityEngine;
using PhamNhanOnline.Client.Core.Application;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    [RequireComponent(typeof(Camera))]
    public sealed class WorldCameraFollowController : WorldSceneBehaviour
    {
        [SerializeField] private WorldLocalPlayerPresenter localPlayerPresenter;
        [SerializeField] private WorldMapPresenter worldMapPresenter;
        [SerializeField] private Vector3 followOffset = new Vector3(0f, 0f, -10f);
        [SerializeField] private bool smoothFollow = false;
        [SerializeField] private float smoothSpeed = 8f;
        [SerializeField] private bool clampToMapBounds = true;

        private Camera cachedCamera;
        private Bounds cachedClampBounds;
        private bool hasCachedClampBounds;

        private void Awake()
        {
            cachedCamera = GetComponent<Camera>();
            AutoWireReferences();
        }

        private void Start()
        {
            AutoWireReferences();
            LogMissingCriticalWorldSceneDependenciesIfNeeded();
            ActivateWorldSceneReadiness();
            TryRefreshCachedClampBoundsIfReady();
        }

        private void OnEnable()
        {
            AutoWireReferences();
            ActivateWorldSceneReadiness();
            TryRefreshCachedClampBoundsIfReady();
        }

        private void OnDisable()
        {
            DeactivateWorldSceneReadiness();
        }

        private void OnDestroy()
        {
            DeactivateWorldSceneReadiness();
        }

        private void LateUpdate()
        {
            if (localPlayerPresenter == null)
                return;

            var target = localPlayerPresenter.CurrentPlayerTransform;
            if (target == null)
                return;

            var desiredPosition = target.position + followOffset;
            desiredPosition = ClampCameraPosition(desiredPosition);

            if (!smoothFollow)
            {
                transform.position = desiredPosition;
                return;
            }

            var t = Mathf.Clamp01(smoothSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, t);
        }

        private Vector3 ClampCameraPosition(Vector3 desiredPosition)
        {
            if (!clampToMapBounds || worldMapPresenter == null || cachedCamera == null || !cachedCamera.orthographic)
                return desiredPosition;

            if (!hasCachedClampBounds)
                return desiredPosition;

            var halfHeight = cachedCamera.orthographicSize;
            var halfWidth = halfHeight * cachedCamera.aspect;

            var minX = cachedClampBounds.min.x + halfWidth;
            var maxX = cachedClampBounds.max.x - halfWidth;
            var minY = cachedClampBounds.min.y + halfHeight;
            var maxY = cachedClampBounds.max.y - halfHeight;

            if (minX > maxX)
                desiredPosition.x = cachedClampBounds.center.x;
            else
                desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);

            if (minY > maxY)
                desiredPosition.y = cachedClampBounds.center.y;
            else
                desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);

            return desiredPosition;
        }

        private bool TryGetCameraClampBounds(out Bounds bounds)
        {
            bounds = default;
            var currentMapTransform = worldMapPresenter.CurrentMapTransform;
            if (currentMapTransform == null)
                return false;

            var mapView = currentMapTransform.GetComponent<ClientMapView>();
            if (mapView != null && mapView.TryGetCameraClampBounds(out bounds))
                return true;

            return worldMapPresenter.TryGetPlayableBounds(out bounds);
        }

        private void RefreshCachedClampBounds()
        {
            hasCachedClampBounds = TryGetCameraClampBounds(out cachedClampBounds);
        }

        private void ClearCachedClampBounds()
        {
            hasCachedClampBounds = false;
            cachedClampBounds = default;
        }

        private void TryRefreshCachedClampBoundsIfReady()
        {
            if (!IsReady(WorldSceneReadyKey.MapVisual))
            {
                ClearCachedClampBounds();
                return;
            }

            RefreshCachedClampBounds();
        }

        protected override void ConfigureReadyWaits()
        {
            WaitFor(WorldSceneReadyKey.MapVisual, TryRefreshCachedClampBoundsIfReady);
        }

        protected override void OnWorldLoadCycleStarted(int loadVersion, string mapKey)
        {
            ClearCachedClampBounds();
        }

        private void AutoWireReferences()
        {
            InitializeWorldSceneBehaviour(ref worldMapPresenter);

            if (localPlayerPresenter == null)
                localPlayerPresenter = SceneController != null ? SceneController.WorldLocalPlayerPresenter : GetComponent<WorldLocalPlayerPresenter>();
        }
    }
}

