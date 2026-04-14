using UnityEngine;

namespace PhamNhanOnline.Client.UI.Common
{
    public sealed class LoopVerticalListView : LoopScrollViewBase
    {
        public enum ChildWidthMode
        {
            KeepTemplateWidth = 0,
            StretchToViewport = 1,
        }

        [Header("Layout")]
        [SerializeField, Min(0f)] private float paddingTop;
        [SerializeField, Min(0f)] private float paddingBottom;
        [SerializeField, Min(0f)] private float paddingLeft;
        [SerializeField, Min(0f)] private float paddingRight;
        [SerializeField, Min(0f)] private float itemSpacing;
        [SerializeField, Min(0)] private int extraVisibleItemCount = 1;
        [SerializeField] private ChildWidthMode childWidthMode = ChildWidthMode.StretchToViewport;
        [SerializeField] private TextAnchor childAlignment = TextAnchor.UpperLeft;

        public delegate LoopScrollViewItem OnGetItemByIndexDelegate(LoopVerticalListView listView, int itemIndex);

        private OnGetItemByIndexDelegate itemProvider;

        public void InitListView(int itemCount, OnGetItemByIndexDelegate onGetItemByIndex)
        {
            itemProvider = onGetItemByIndex;
            InitInternal(itemCount, ResolveItemByIndex);
        }

        public void SetListItemCount(int itemCount, bool keepPosition = false)
        {
            SetListItemCountInternal(itemCount, keepPosition);
        }

        protected override int GetVisibleItemStartIndex(float scrollOffset)
        {
            var stride = GetItemStride();
            if (stride <= 0f)
                return 0;

            var start = Mathf.FloorToInt(Mathf.Max(0f, scrollOffset - paddingTop) / stride) - extraVisibleItemCount;
            return Mathf.Max(0, start);
        }

        protected override int GetVisibleItemEndIndex(float scrollOffset, int itemCount)
        {
            if (itemCount <= 0)
                return -1;

            var stride = GetItemStride();
            if (stride <= 0f)
                return itemCount - 1;

            var viewportHeight = Mathf.Max(0f, ViewportSize.y);
            var end = Mathf.CeilToInt(Mathf.Max(0f, scrollOffset + viewportHeight - paddingTop) / stride) + extraVisibleItemCount;
            return Mathf.Min(itemCount - 1, end);
        }

        protected override Vector2 CalculateContentSize(int itemCount)
        {
            var templateSize = GetPrimaryTemplateSize();
            var layoutHeight = CalculateLayoutHeight(itemCount, templateSize.y);
            var layoutWidth = CalculateOccupiedWidth(templateSize.x);
            var width = Mathf.Max(ViewportSize.x, layoutWidth);
            var height = Mathf.Max(ViewportSize.y, layoutHeight);

            return new Vector2(width, height);
        }

        protected override Vector2 CalculateItemAnchoredPosition(int itemIndex)
        {
            var templateSize = GetPrimaryTemplateSize();
            var layoutHeight = CalculateLayoutHeight(ItemTotalCount, templateSize.y);
            var layoutWidth = CalculateOccupiedWidth(templateSize.x);
            var extraWidth = Mathf.Max(0f, ContentWidth - layoutWidth);
            var extraHeight = Mathf.Max(0f, ContentHeight - layoutHeight);
            var x = paddingLeft + ResolveHorizontalAlignmentOffset(extraWidth);
            var y = -(paddingTop + ResolveVerticalAlignmentOffset(extraHeight) + itemIndex * GetItemStride());
            return new Vector2(x, y);
        }

        protected override void PrepareItemRect(RectTransform itemRect)
        {
            if (itemRect == null)
                return;

            var templateSize = GetPrimaryTemplateSize();
            itemRect.anchorMin = new Vector2(0f, 1f);
            itemRect.anchorMax = new Vector2(0f, 1f);
            itemRect.pivot = new Vector2(0f, 1f);

            var width = childWidthMode == ChildWidthMode.StretchToViewport
                ? Mathf.Max(0f, ContentWidth - paddingLeft - paddingRight)
                : templateSize.x;
            itemRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            itemRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, templateSize.y);
        }

        protected override float GetScrollOffset()
        {
            return ContentRect != null ? Mathf.Max(0f, ContentRect.anchoredPosition.y) : 0f;
        }

        protected override void SetScrollOffset(float value)
        {
            if (ContentRect == null)
                return;

            var anchoredPosition = ContentRect.anchoredPosition;
            anchoredPosition.y = Mathf.Clamp(value, 0f, GetMaxScrollOffset());
            ContentRect.anchoredPosition = anchoredPosition;
        }

        protected override float GetMaxScrollOffset()
        {
            return Mathf.Max(0f, ContentHeight - ViewportSize.y);
        }

        protected override float CalculateScrollOffsetForItem(int itemIndex, float offset)
        {
            return paddingTop + itemIndex * GetItemStride() + offset;
        }

        private float GetItemStride()
        {
            var templateSize = GetPrimaryTemplateSize();
            return templateSize.y + itemSpacing;
        }

        private float CalculateLayoutHeight(int itemCount, float itemHeight)
        {
            var height = paddingTop + paddingBottom;
            if (itemCount > 0)
                height += itemCount * itemHeight + Mathf.Max(0, itemCount - 1) * itemSpacing;

            return height;
        }

        private float CalculateOccupiedWidth(float templateWidth)
        {
            var childWidth = childWidthMode == ChildWidthMode.StretchToViewport
                ? Mathf.Max(0f, ViewportSize.x - paddingLeft - paddingRight)
                : templateWidth;
            return paddingLeft + paddingRight + childWidth;
        }

        private float ResolveHorizontalAlignmentOffset(float extraWidth)
        {
            switch (childAlignment)
            {
                case TextAnchor.UpperCenter:
                case TextAnchor.MiddleCenter:
                case TextAnchor.LowerCenter:
                    return extraWidth * 0.5f;
                case TextAnchor.UpperRight:
                case TextAnchor.MiddleRight:
                case TextAnchor.LowerRight:
                    return extraWidth;
                default:
                    return 0f;
            }
        }

        private float ResolveVerticalAlignmentOffset(float extraHeight)
        {
            switch (childAlignment)
            {
                case TextAnchor.MiddleLeft:
                case TextAnchor.MiddleCenter:
                case TextAnchor.MiddleRight:
                    return extraHeight * 0.5f;
                case TextAnchor.LowerLeft:
                case TextAnchor.LowerCenter:
                case TextAnchor.LowerRight:
                    return extraHeight;
                default:
                    return 0f;
            }
        }

        private LoopScrollViewItem ResolveItemByIndex(int itemIndex)
        {
            return itemProvider != null ? itemProvider(this, itemIndex) : null;
        }
    }
}
