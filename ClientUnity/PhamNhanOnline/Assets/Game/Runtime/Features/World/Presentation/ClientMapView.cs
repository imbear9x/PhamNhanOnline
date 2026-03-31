using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    public sealed class ClientMapView : MonoBehaviour
    {
        [SerializeField] private Collider2D playableBoundsCollider;
        [SerializeField] private Renderer playableBoundsRenderer;
        [SerializeField] private Collider2D cameraClampBoundsCollider;
        [SerializeField] private Renderer cameraClampBoundsRenderer;

        public bool TryGetPlayableBounds(out Bounds bounds)
        {
            if (playableBoundsCollider != null)
            {
                bounds = playableBoundsCollider.bounds;
                return true;
            }

            if (playableBoundsRenderer != null)
            {
                bounds = playableBoundsRenderer.bounds;
                return true;
            }

            bounds = default;
            return false;
        }

        public bool TryGetCameraClampBounds(out Bounds bounds)
        {
            if (cameraClampBoundsCollider != null)
            {
                bounds = cameraClampBoundsCollider.bounds;
                return true;
            }

            if (cameraClampBoundsRenderer != null)
            {
                bounds = cameraClampBoundsRenderer.bounds;
                return true;
            }

            return TryGetPlayableBounds(out bounds);
        }

        public string DescribePlayableBoundsSources()
        {
            var colliderState = playableBoundsCollider != null
                ? $"{playableBoundsCollider.name} ({playableBoundsCollider.GetType().Name}, enabled={playableBoundsCollider.enabled})"
                : "null";
            var rendererState = playableBoundsRenderer != null
                ? $"{playableBoundsRenderer.name} ({playableBoundsRenderer.GetType().Name}, enabled={playableBoundsRenderer.enabled})"
                : "null";
            return $"playableBoundsCollider={colliderState}, playableBoundsRenderer={rendererState}";
        }
    }
}
