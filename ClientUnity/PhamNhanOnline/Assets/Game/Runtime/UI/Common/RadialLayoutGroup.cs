using UnityEngine;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Common
{
    [AddComponentMenu("Layout/Radial Layout Group")]
    [DisallowMultipleComponent]
    public sealed class RadialLayoutGroup : LayoutGroup
    {
        public enum RadialAlignment
        {
            Right = 0,
            TopRight = 1,
            Top = 2,
            TopLeft = 3,
            Left = 4,
            BottomLeft = 5,
            Bottom = 6,
            BottomRight = 7
        }

        [Header("Radial Layout")]
        [SerializeField] private RectTransform centerPoint;
        [SerializeField] private RadialAlignment radialAlignment = RadialAlignment.Top;
        [SerializeField] private float radius = 180f;
        [SerializeField] private float itemSpacing = 64f;
        [SerializeField] private float angleOffsetDegrees;
        [SerializeField] private bool controlChildRotation;
        [SerializeField] private bool rotateChildrenOutward;

        public RectTransform CenterPoint
        {
            get => centerPoint;
            set => SetProperty(ref centerPoint, value);
        }

        public RadialAlignment Alignment
        {
            get => radialAlignment;
            set => SetProperty(ref radialAlignment, value);
        }

        public float Radius
        {
            get => radius;
            set => SetProperty(ref radius, Mathf.Max(0f, value));
        }

        public float ItemSpacing
        {
            get => itemSpacing;
            set => SetProperty(ref itemSpacing, Mathf.Max(0f, value));
        }

        public float AngleOffsetDegrees
        {
            get => angleOffsetDegrees;
            set => SetProperty(ref angleOffsetDegrees, value);
        }

        public bool ControlChildRotation
        {
            get => controlChildRotation;
            set => SetProperty(ref controlChildRotation, value);
        }

        public bool RotateChildrenOutward
        {
            get => rotateChildrenOutward;
            set => SetProperty(ref rotateChildrenOutward, value);
        }

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();

            var requiredWidth = Mathf.Max(0f, (radius * 2f) + padding.horizontal);
            SetLayoutInputForAxis(requiredWidth, requiredWidth, -1f, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            var requiredHeight = Mathf.Max(0f, (radius * 2f) + padding.vertical);
            SetLayoutInputForAxis(requiredHeight, requiredHeight, -1f, 1);
        }

        public override void SetLayoutHorizontal()
        {
            ArrangeChildren();
        }

        public override void SetLayoutVertical()
        {
            ArrangeChildren();
        }

        protected override void OnTransformChildrenChanged()
        {
            base.OnTransformChildrenChanged();
            SetDirty();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            SetDirty();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            radius = Mathf.Max(0f, radius);
            itemSpacing = Mathf.Max(0f, itemSpacing);
            SetDirty();
        }

        private void ArrangeChildren()
        {
            m_Tracker.Clear();

            if (rectChildren == null || rectChildren.Count == 0)
                return;

            var layoutRect = rectTransform.rect;
            layoutRect.xMin += padding.left;
            layoutRect.xMax -= padding.right;
            layoutRect.yMin += padding.bottom;
            layoutRect.yMax -= padding.top;

            var center = ResolveCenterPosition(layoutRect);
            var childCount = rectChildren.Count;
            var angleStepDegrees = ComputeAngleStepDegrees(radius, itemSpacing);
            var centerAngleDegrees = ResolveCenterAngleDegrees(radialAlignment) + angleOffsetDegrees;
            var startAngleDegrees = centerAngleDegrees - ((childCount - 1) * angleStepDegrees * 0.5f);

            for (var i = 0; i < childCount; i++)
            {
                var child = rectChildren[i];
                if (child == null)
                    continue;

                m_Tracker.Add(
                    this,
                    child,
                    DrivenTransformProperties.Anchors |
                    DrivenTransformProperties.AnchoredPosition |
                    (controlChildRotation ? DrivenTransformProperties.Rotation : 0));

                child.anchorMin = new Vector2(0.5f, 0.5f);
                child.anchorMax = new Vector2(0.5f, 0.5f);

                var angleDegrees = startAngleDegrees + (i * angleStepDegrees);
                var angleRadians = angleDegrees * Mathf.Deg2Rad;
                var position = center + new Vector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians)) * radius;
                child.anchoredPosition = position;

                if (controlChildRotation)
                {
                    var zRotation = rotateChildrenOutward
                        ? angleDegrees - 90f
                        : 0f;
                    child.localRotation = Quaternion.Euler(0f, 0f, zRotation);
                }
            }
        }

        private static float ComputeAngleStepDegrees(float currentRadius, float spacing)
        {
            if (currentRadius <= 0.001f || spacing <= 0f)
                return 0f;

            return (spacing / currentRadius) * Mathf.Rad2Deg;
        }

        private Vector2 ResolveCenterPosition(Rect layoutRect)
        {
            if (centerPoint == null || centerPoint == rectTransform)
                return layoutRect.center;

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, centerPoint.position);
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, null, out localPoint))
                return localPoint;

            return layoutRect.center;
        }

        private static float ResolveCenterAngleDegrees(RadialAlignment alignment)
        {
            switch (alignment)
            {
                case RadialAlignment.Right:
                    return 0f;
                case RadialAlignment.TopRight:
                    return 45f;
                case RadialAlignment.Top:
                    return 90f;
                case RadialAlignment.TopLeft:
                    return 135f;
                case RadialAlignment.Left:
                    return 180f;
                case RadialAlignment.BottomLeft:
                    return 225f;
                case RadialAlignment.Bottom:
                    return 270f;
                case RadialAlignment.BottomRight:
                    return 315f;
                default:
                    return 90f;
            }
        }

        protected override void Reset()
        {
            base.Reset();
            childAlignment = TextAnchor.MiddleCenter;
        }

        protected override void OnDisable()
        {
            m_Tracker.Clear();
            base.OnDisable();
        }
    }
}
