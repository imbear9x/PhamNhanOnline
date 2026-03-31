using System.Collections.Generic;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class MapSizeRootController : MonoBehaviour
    {
        private const string GeneratedSegmentNamePrefix = "AutoSegment_";

        [Header("Configured Size")]
        [SerializeField] private float configuredWidth = 20f;
        [SerializeField] private float configuredHeight = 8f;

        [Header("Segments")]
        [SerializeField] private Transform segmentRoot;
        [SerializeField] private SpriteRenderer segmentTemplateRenderer;
        [SerializeField] private float preferredSegmentWidth = 20f;

        [Header("Scene Preview")]
        [SerializeField] private Color previewFillColor = new Color(0.2f, 0.8f, 1f, 0.1f);
        [SerializeField] private Color previewOutlineColor = new Color(0.2f, 0.8f, 1f, 0.9f);

        private readonly List<SpriteRenderer> workingSegments = new List<SpriteRenderer>(8);

        public float ConfiguredWidth => configuredWidth;
        public float ConfiguredHeight => configuredHeight;

        private void Reset()
        {
            AutoResolveReferences();
            ApplyConfiguredSize();
        }

        private void Awake()
        {
            AutoResolveReferences();
            ApplyConfiguredSize();
        }

        private void OnEnable()
        {
            AutoResolveReferences();
            ApplyConfiguredSize();
        }

        private void OnValidate()
        {
            AutoResolveReferences();
            ApplyConfiguredSize();
        }

        public void ApplyConfiguredSize()
        {
            AutoResolveReferences();
            if (segmentTemplateRenderer == null || segmentTemplateRenderer.sprite == null)
                return;

            var targetWidth = Mathf.Max(0.01f, configuredWidth);
            var targetHeight = Mathf.Max(0.01f, configuredHeight);
            var segmentCount = Mathf.Max(1, Mathf.CeilToInt(targetWidth / Mathf.Max(0.01f, preferredSegmentWidth)));
            var segmentWidth = targetWidth / segmentCount;

            BuildSegmentList(segmentCount);
            for (var i = 0; i < workingSegments.Count; i++)
            {
                var segment = workingSegments[i];
                if (segment == null)
                    continue;

                SyncSegmentPresentation(segment);
                PositionSegment(segment.transform, i, segmentCount, segmentWidth);
                ResizeSegment(segment, segmentWidth, targetHeight);
            }
        }

        private void OnDrawGizmos()
        {
            var size = new Vector3(
                Mathf.Max(0.01f, configuredWidth),
                Mathf.Max(0.01f, configuredHeight),
                0.01f);
            var matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.matrix = matrix;
            Gizmos.color = previewFillColor;
            Gizmos.DrawCube(Vector3.zero, size);
            Gizmos.color = previewOutlineColor;
            Gizmos.DrawWireCube(Vector3.zero, size);
        }

        private void AutoResolveReferences()
        {
            if (segmentRoot == null)
                segmentRoot = transform;

            if (segmentTemplateRenderer == null)
                segmentTemplateRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }

        private void BuildSegmentList(int requiredSegmentCount)
        {
            workingSegments.Clear();
            if (segmentTemplateRenderer == null)
                return;

            workingSegments.Add(segmentTemplateRenderer);
            var root = segmentRoot != null ? segmentRoot : transform;

            var generatedSegments = new List<SpriteRenderer>();
            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child == segmentTemplateRenderer.transform)
                    continue;

                if (!child.name.StartsWith(GeneratedSegmentNamePrefix, System.StringComparison.Ordinal))
                    continue;

                var childRenderer = child.GetComponent<SpriteRenderer>();
                if (childRenderer != null)
                    generatedSegments.Add(childRenderer);
            }

            generatedSegments.Sort((left, right) => string.CompareOrdinal(left.name, right.name));

            var neededGeneratedCount = Mathf.Max(0, requiredSegmentCount - 1);
            for (var i = generatedSegments.Count; i < neededGeneratedCount; i++)
                generatedSegments.Add(CreateGeneratedSegment(root));

            for (var i = generatedSegments.Count - 1; i >= neededGeneratedCount; i--)
            {
                DestroyObjectSafe(generatedSegments[i].gameObject);
                generatedSegments.RemoveAt(i);
            }

            for (var i = 0; i < generatedSegments.Count; i++)
            {
                generatedSegments[i].name = GeneratedSegmentNamePrefix + (i + 1).ToString("00");
                workingSegments.Add(generatedSegments[i]);
            }
        }

        private SpriteRenderer CreateGeneratedSegment(Transform root)
        {
            var clone = Instantiate(segmentTemplateRenderer.gameObject, root, false);
            clone.name = GeneratedSegmentNamePrefix + "00";
            return clone.GetComponent<SpriteRenderer>();
        }

        private void SyncSegmentPresentation(SpriteRenderer segment)
        {
            if (segment == null || segmentTemplateRenderer == null)
                return;

            segment.sprite = segmentTemplateRenderer.sprite;
            segment.sharedMaterial = segmentTemplateRenderer.sharedMaterial;
            segment.color = segmentTemplateRenderer.color;
            segment.flipX = segmentTemplateRenderer.flipX;
            segment.flipY = segmentTemplateRenderer.flipY;
            segment.drawMode = segmentTemplateRenderer.drawMode;
            segment.maskInteraction = segmentTemplateRenderer.maskInteraction;
            segment.sortingLayerID = segmentTemplateRenderer.sortingLayerID;
            segment.sortingOrder = segmentTemplateRenderer.sortingOrder;

            if (!segment.gameObject.activeSelf)
                segment.gameObject.SetActive(true);
        }

        private static void PositionSegment(Transform segmentTransform, int index, int segmentCount, float segmentWidth)
        {
            if (segmentTransform == null)
                return;

            var localPosition = segmentTransform.localPosition;
            localPosition.x = (-segmentCount * segmentWidth * 0.5f) + (segmentWidth * (index + 0.5f));
            localPosition.y = 0f;
            segmentTransform.localPosition = localPosition;
        }

        private static void DestroyObjectSafe(GameObject target)
        {
            if (target == null)
                return;

            if (UnityEngine.Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }

        private void ResizeSegment(SpriteRenderer segment, float targetWidth, float targetHeight)
        {
            if (segment == null || segment.sprite == null)
                return;

            if (segment.drawMode != SpriteDrawMode.Simple)
            {
                segment.size = new Vector2(targetWidth, targetHeight);
                return;
            }

            var spriteBounds = segment.sprite.bounds.size;
            if (spriteBounds.x <= Mathf.Epsilon || spriteBounds.y <= Mathf.Epsilon)
                return;

            var segmentTransform = segment.transform;
            var parent = segmentTransform.parent;
            var parentLossyScale = parent != null ? parent.lossyScale : Vector3.one;
            var safeParentScaleX = Mathf.Approximately(parentLossyScale.x, 0f) ? 1f : Mathf.Abs(parentLossyScale.x);
            var safeParentScaleY = Mathf.Approximately(parentLossyScale.y, 0f) ? 1f : Mathf.Abs(parentLossyScale.y);

            var scale = segmentTransform.localScale;
            var targetScaleX = targetWidth / (spriteBounds.x * safeParentScaleX);
            var targetScaleY = targetHeight / (spriteBounds.y * safeParentScaleY);
            scale.x = Mathf.Sign(scale.x == 0f ? 1f : scale.x) * targetScaleX;
            scale.y = Mathf.Sign(scale.y == 0f ? 1f : scale.y) * targetScaleY;
            segmentTransform.localScale = scale;
        }
    }
}
