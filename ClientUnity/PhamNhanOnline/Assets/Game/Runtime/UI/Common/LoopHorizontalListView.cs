using UnityEngine;

namespace PhamNhanOnline.Client.UI.Common
{
    public sealed class LoopHorizontalListView : LoopScrollViewBase
    {
        public enum ChildHeightMode
        {
            KeepTemplateHeight = 0,
            StretchToViewport = 1,
        }

        [Header("Layout")]
        [SerializeField, Min(0f)] private float paddingTop;
        [SerializeField, Min(0f)] private float paddingBottom;
        [SerializeField, Min(0f)] private float paddingLeft;
        [SerializeField, Min(0f)] private float paddingRight;
        [SerializeField, Min(0f)] private float itemSpacing;
        [SerializeField, Min(0)] private int extraVisibleItemCount = 1;
        [SerializeField] private ChildHeightMode childHeightMode = ChildHeightMode.StretchToViewport;
        [SerializeField] private TextAnchor childAlignment = TextAnchor.UpperLeft;

        public delegate LoopScrollViewItem OnGetItemByIndexDelegate(LoopHorizontalListView listView, int itemIndex);

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

            var start = Mathf.FloorToInt(Mathf.Max(0f, scrollOffset - paddingLeft) / stride) - extraVisibleItemCount;
            return Mathf.Max(0, start);
        }

        protected override int GetVisibleItemEndIndex(float scrollOffset, int itemCount)
        {
            if (itemCount <= 0)
                return -1;

            var stride = GetItemStride();
            if (stride <= 0f)
                return itemCount - 1;

            var viewportWidth = Mathf.Max(0f, ViewportSize.x);
            var end = Mathf.CeilToInt(Mathf.Max(0f, scrollOffset + viewportWidth - paddingLeft) / stride) + extraVisibleItemCount;
            return Mathf.Min(itemCount - 1, end);
        }

        protected override Vector2 CalculateContentSize(int itemCount)
        {
            var templateSize = GetPrimaryTemplateSize();
            var layoutWidth = CalculateLayoutWidth(itemCount, templateSize.x);
            var layoutHeight = CalculateOccupiedHeight(templateSize.y);
            var width = Mathf.Max(ViewportSize.x, layoutWidth);
            var height = Mathf.Max(ViewportSize.y, layoutHeight);
            return new Vector2(width, height);
        }

        protected override Vector2 CalculateItemAnchoredPosition(int itemIndex)
        {
            var templateSize = GetPrimaryTemplateSize();
            var layoutWidth = CalculateLayoutWidth(ItemTotalCount, templateSize.x);
            var layoutHeight = CalculateOccupiedHeight(templateSize.y);
            var extraWidth = Mathf.Max(0f, ContentWidth - layoutWidth);
            var extraHeight = Mathf.Max(0f, ContentHeight - layoutHeight);
            var x = paddingLeft + ResolveHorizontalAlignmentOffset(extraWidth) + itemIndex * GetItemStride();
            var y = -(paddingTop + ResolveVerticalAlignmentOffset(extraHeight));
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

            itemRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, templateSize.x);
            var height = childHeightMode == ChildHeightMode.StretchToViewport
                ? Mathf.Max(0f, ContentHeight - paddingTop - paddingBottom)
                : templateSize.y;
            itemRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        protected override float GetScrollOffset()
        {
            return ContentRect != null ? Mathf.Max(0f, -ContentRect.anchoredPosition.x) : 0f;
        }

        protected override void SetScrollOffset(float value)
        {
            if (ContentRect == null)
                return;

            var anchoredPosition = ContentRect.anchoredPosition;
            anchoredPosition.x = -Mathf.Clamp(value, 0f, GetMaxScrollOffset());
            ContentRect.anchoredPosition = anchoredPosition;
        }

        protected override float GetMaxScrollOffset()
        {
            return Mathf.Max(0f, ContentWidth - ViewportSize.x);
        }

        protected override float CalculateScrollOffsetForItem(int itemIndex, float offset)
        {
            return paddingLeft + itemIndex * GetItemStride() + offset;
        }

        private float GetItemStride()
        {
            var templateSize = GetPrimaryTemplateSize();
            return templateSize.x + itemSpacing;
        }

        private float CalculateLayoutWidth(int itemCount, float itemWidth)
        {
            var width = paddingLeft + paddingRight;
            if (itemCount > 0)
                width += itemCount * itemWidth + Mathf.Max(0, itemCount - 1) * itemSpacing;

            return width;
        }

        private float CalculateOccupiedHeight(float templateHeight)
        {
            var childHeight = childHeightMode == ChildHeightMode.StretchToViewport
                ? Mathf.Max(0f, ViewportSize.y - paddingTop - paddingBottom)
                : templateHeight;
            return paddingTop + paddingBottom + childHeight;
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
