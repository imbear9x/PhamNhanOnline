using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    [RequireComponent(typeof(Camera))]
    public sealed class WorldCameraFollowController : MonoBehaviour
    {
        [SerializeField] private WorldLocalPlayerPresenter localPlayerPresenter;
        [SerializeField] private WorldMapPresenter worldMapPresenter;
        [SerializeField] private Vector3 followOffset = new Vector3(0f, 0f, -10f);
        [SerializeField] private bool smoothFollow = false;
        [SerializeField] private float smoothSpeed = 8f;
        [SerializeField] private bool clampToMapBounds = true;

        private Camera cachedCamera;

        private void Awake()
        {
            cachedCamera = GetComponent<Camera>();
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

            Bounds playableBounds;
            if (!worldMapPresenter.TryGetPlayableBounds(out playableBounds))
                return desiredPosition;

            var halfHeight = cachedCamera.orthographicSize;
            var halfWidth = halfHeight * cachedCamera.aspect;

            var minX = playableBounds.min.x + halfWidth;
            var maxX = playableBounds.max.x - halfWidth;
            var minY = playableBounds.min.y + halfHeight;
            var maxY = playableBounds.max.y - halfHeight;

            if (minX > maxX)
                desiredPosition.x = playableBounds.center.x;
            else
                desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);

            if (minY > maxY)
                desiredPosition.y = playableBounds.center.y;
            else
                desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);

            return desiredPosition;
        }
    }
}