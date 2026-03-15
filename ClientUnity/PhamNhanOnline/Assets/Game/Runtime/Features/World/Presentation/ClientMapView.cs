using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    public sealed class ClientMapView : MonoBehaviour
    {
        [SerializeField] private Collider2D playableBoundsCollider;
        [SerializeField] private Renderer playableBoundsRenderer;

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
    }
}