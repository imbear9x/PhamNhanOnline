using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    [DisallowMultipleComponent]
    public sealed class GroundRewardVisualBindings : MonoBehaviour
    {
        [SerializeField] private Transform scaleRoot;
        [SerializeField] private SpriteRenderer iconRenderer;
        [SerializeField] private SpriteRenderer[] outlineRenderers = System.Array.Empty<SpriteRenderer>();
        [SerializeField] private Renderer[] boundsRenderers = System.Array.Empty<Renderer>();
        [SerializeField] private bool preserveConfiguredIconSize = true;

        private Vector2 configuredIconLocalSize;
        private bool configuredIconLocalSizeCaptured;
        private Vector3 configuredIconLocalScale = Vector3.one;
        private Vector3[] configuredOutlineLocalScales = System.Array.Empty<Vector3>();
        private Vector3[] configuredOutlineLocalPositions = System.Array.Empty<Vector3>();

        public Transform ScaleRoot
        {
            get { return scaleRoot != null ? scaleRoot : transform; }
        }

        public SpriteRenderer IconRenderer
        {
            get { return iconRenderer; }
        }

        public SpriteRenderer[] OutlineRenderers
        {
            get { return outlineRenderers ?? System.Array.Empty<SpriteRenderer>(); }
        }

        public Renderer[] BoundsRenderers
        {
            get { return boundsRenderers ?? System.Array.Empty<Renderer>(); }
        }

        private void Awake()
        {
            CaptureConfiguredIconSize();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            CaptureConfiguredIconSize();
        }
#endif

        public void ApplySprite(Sprite sprite)
        {
            CaptureConfiguredIconSize();

            Vector3 fittedIconScale = Vector3.one;
            if (iconRenderer != null)
            {
                iconRenderer.sprite = sprite;
                fittedIconScale = FitIconRendererToConfiguredSize();
            }

            var renderers = OutlineRenderers;
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null)
                    continue;

                renderer.sprite = sprite;
                FitOutlineRendererToConfiguredSize(renderer, i, fittedIconScale);
            }
        }

        private void CaptureConfiguredIconSize()
        {
            if (!preserveConfiguredIconSize || iconRenderer == null)
                return;

            var sprite = iconRenderer.sprite;
            if (sprite == null)
                return;

            var spriteSize = sprite.bounds.size;
            if (spriteSize.x <= Mathf.Epsilon || spriteSize.y <= Mathf.Epsilon)
                return;

            configuredIconLocalScale = iconRenderer.transform.localScale;
            configuredIconLocalSize = new Vector2(
                Mathf.Abs(spriteSize.x * iconRenderer.transform.localScale.x),
                Mathf.Abs(spriteSize.y * iconRenderer.transform.localScale.y));
            configuredIconLocalSizeCaptured = configuredIconLocalSize.x > Mathf.Epsilon && configuredIconLocalSize.y > Mathf.Epsilon;

            var renderers = OutlineRenderers;
            if (configuredOutlineLocalScales.Length != renderers.Length)
            {
                configuredOutlineLocalScales = new Vector3[renderers.Length];
                configuredOutlineLocalPositions = new Vector3[renderers.Length];
            }

            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                configuredOutlineLocalScales[i] = renderer != null ? renderer.transform.localScale : Vector3.one;
                configuredOutlineLocalPositions[i] = renderer != null ? renderer.transform.localPosition : Vector3.zero;
            }
        }

        private Vector3 FitIconRendererToConfiguredSize()
        {
            if (!preserveConfiguredIconSize || !configuredIconLocalSizeCaptured || iconRenderer == null)
                return iconRenderer != null ? iconRenderer.transform.localScale : Vector3.one;

            var sprite = iconRenderer.sprite;
            if (sprite == null)
                return iconRenderer.transform.localScale;

            var spriteSize = sprite.bounds.size;
            if (spriteSize.x <= Mathf.Epsilon || spriteSize.y <= Mathf.Epsilon)
                return iconRenderer.transform.localScale;

            var fittedScale = new Vector3(
                ComputeSignedScale(configuredIconLocalScale.x, configuredIconLocalSize.x / spriteSize.x),
                ComputeSignedScale(configuredIconLocalScale.y, configuredIconLocalSize.y / spriteSize.y),
                Mathf.Approximately(configuredIconLocalScale.z, 0f) ? 1f : configuredIconLocalScale.z);
            iconRenderer.transform.localScale = fittedScale;
            return fittedScale;
        }

        private void FitOutlineRendererToConfiguredSize(SpriteRenderer renderer, int index, Vector3 fittedIconScale)
        {
            if (!preserveConfiguredIconSize || !configuredIconLocalSizeCaptured || renderer == null)
                return;

            var outlineLocalScale = index >= 0 && index < configuredOutlineLocalScales.Length
                ? configuredOutlineLocalScales[index]
                : renderer.transform.localScale;
            var outlineLocalPosition = index >= 0 && index < configuredOutlineLocalPositions.Length
                ? configuredOutlineLocalPositions[index]
                : renderer.transform.localPosition;

            renderer.transform.localPosition = outlineLocalPosition;

            if (Mathf.Abs(configuredIconLocalScale.x) <= Mathf.Epsilon || Mathf.Abs(configuredIconLocalScale.y) <= Mathf.Epsilon)
            {
                renderer.transform.localScale = outlineLocalScale;
                return;
            }

            renderer.transform.localScale = new Vector3(
                fittedIconScale.x * (outlineLocalScale.x / configuredIconLocalScale.x),
                fittedIconScale.y * (outlineLocalScale.y / configuredIconLocalScale.y),
                Mathf.Approximately(outlineLocalScale.z, 0f) ? 1f : outlineLocalScale.z);
        }

        private static float ComputeSignedScale(float authoredScale, float fittedMagnitude)
        {
            if (Mathf.Approximately(authoredScale, 0f))
                return fittedMagnitude;

            return Mathf.Sign(authoredScale) * fittedMagnitude;
        }
    }
}
