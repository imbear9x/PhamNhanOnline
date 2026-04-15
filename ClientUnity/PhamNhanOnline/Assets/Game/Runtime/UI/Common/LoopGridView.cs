using UnityEngine;

namespace PhamNhanOnline.Client.UI.Common
{
    public sealed class LoopGridView : LoopScrollViewBase
    {
        public enum GridConstraintMode
        {
            ColumnCount = 0,
            RowCount = 1,
        }

        public enum CrossAxisCellSizeMode
        {
            KeepTemplateSize = 0,
            StretchToViewport = 1,
        }

        [Header("Layout")]
        [SerializeField] private GridConstraintMode constraintMode = GridConstraintMode.ColumnCount;
        [SerializeField, Min(1)] private int constraintCount = 4;
        [SerializeField, Min(0f)] private float paddingTop;
        [SerializeField, Min(0f)] private float paddingBottom;
        [SerializeField, Min(0f)] private float paddingLeft;
        [SerializeField, Min(0f)] private float paddingRight;
        [SerializeField, Min(0f)] private float spacingX;
        [SerializeField, Min(0f)] private float spacingY;
        [SerializeField, Min(0)] private int extraVisibleLineCount = 1;
        [SerializeField] private CrossAxisCellSizeMode crossAxisCellSizeMode = CrossAxisCellSizeMode.KeepTemplateSize;
        [SerializeField] private TextAnchor childAlignment = TextAnchor.UpperLeft;

        public delegate LoopScrollViewItem OnGetItemByIndexDelegate(LoopGridView gridView, int itemIndex);

        private OnGetItemByIndexDelegate itemProvider;

        public void InitGridView(int itemCount, OnGetItemByIndexDelegate onGetItemByIndex)
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
            var lineStride = GetLineStride();
            if (lineStride <= 0f)
                return 0;

            var paddingStart = IsVertical ? paddingTop : paddingLeft;
            var startLine = Mathf.FloorToInt(Mathf.Max(0f, scrollOffset - paddingStart) / lineStride) - extraVisibleLineCount;
            return Mathf.Max(0, startLine) * GetCrossAxisCount();
        }

        protected override int GetVisibleItemEndIndex(float scrollOffset, int itemCount)
        {
            if (itemCount <= 0)
                return -1;

            var lineStride = GetLineStride();
            if (lineStride <= 0f)
                return itemCount - 1;

            var paddingStart = IsVertical ? paddingTop : paddingLeft;
            var viewportSpan = IsVertical ? ViewportSize.y : ViewportSize.x;
            var endLine = Mathf.CeilToInt(Mathf.Max(0f, scrollOffset + viewportSpan - paddingStart) / lineStride) + extraVisibleLineCount;
            var maxItemIndex = Mathf.Max(0, itemCount - 1);
            return Mathf.Min(maxItemIndex, ((endLine + 1) * GetCrossAxisCount()) - 1);
        }

        protected override Vector2 CalculateContentSize(int itemCount)
        {
            var cellSize = GetPrimaryTemplateSize();
            var crossAxisCount = GetCrossAxisCount();
            var lineCount = GetLineCount(itemCount, crossAxisCount);
            var resolvedCellWidth = ResolveCellWidth(cellSize.x, crossAxisCount);
            var resolvedCellHeight = ResolveCellHeight(cellSize.y, crossAxisCount);

            if (IsVertical)
            {
                var width = paddingLeft + paddingRight + crossAxisCount * resolvedCellWidth + Mathf.Max(0, crossAxisCount - 1) * spacingX;
                var height = paddingTop + paddingBottom;
                if (lineCount > 0)
                    height += lineCount * resolvedCellHeight + Mathf.Max(0, lineCount - 1) * spacingY;

                return new Vector2(Mathf.Max(ViewportSize.x, width), Mathf.Max(ViewportSize.y, height));
            }

            var contentWidth = paddingLeft + paddingRight;
            if (lineCount > 0)
                contentWidth += lineCount * resolvedCellWidth + Mathf.Max(0, lineCount - 1) * spacingX;

            var contentHeight = paddingTop + paddingBottom + crossAxisCount * resolvedCellHeight + Mathf.Max(0, crossAxisCount - 1) * spacingY;
            return new Vector2(Mathf.Max(ViewportSize.x, contentWidth), Mathf.Max(ViewportSize.y, contentHeight));
        }

        protected override Vector2 CalculateItemAnchoredPosition(int itemIndex)
        {
            var cellSize = GetPrimaryTemplateSize();
            var crossAxisCount = GetCrossAxisCount();
            var lineCount = GetLineCount(ItemTotalCount, crossAxisCount);
            var resolvedCellWidth = ResolveCellWidth(cellSize.x, crossAxisCount);
            var resolvedCellHeight = ResolveCellHeight(cellSize.y, crossAxisCount);
            var layoutWidth = IsVertical
                ? paddingLeft + paddingRight + crossAxisCount * resolvedCellWidth + Mathf.Max(0, crossAxisCount - 1) * spacingX
                : paddingLeft + paddingRight + lineCount * resolvedCellWidth + Mathf.Max(0, lineCount - 1) * spacingX;
            var layoutHeight = IsVertical
                ? paddingTop + paddingBottom + lineCount * resolvedCellHeight + Mathf.Max(0, lineCount - 1) * spacingY
                : paddingTop + paddingBottom + crossAxisCount * resolvedCellHeight + Mathf.Max(0, crossAxisCount - 1) * spacingY;
            var extraWidth = Mathf.Max(0f, ContentWidth - layoutWidth);
            var extraHeight = Mathf.Max(0f, ContentHeight - layoutHeight);
            var horizontalOffset = ResolveHorizontalAlignmentOffset(extraWidth);
            var verticalOffset = ResolveVerticalAlignmentOffset(extraHeight);

            if (IsVertical)
            {
                var columnIndex = itemIndex % crossAxisCount;
                var rowIndex = itemIndex / crossAxisCount;
                var x = paddingLeft + horizontalOffset + columnIndex * (resolvedCellWidth + spacingX);
                var y = -(paddingTop + verticalOffset + rowIndex * (resolvedCellHeight + spacingY));
                return new Vector2(x, y);
            }

            var row = itemIndex % crossAxisCount;
            var column = itemIndex / crossAxisCount;
            var horizontalX = paddingLeft + horizontalOffset + column * (resolvedCellWidth + spacingX);
            var horizontalY = -(paddingTop + verticalOffset + row * (resolvedCellHeight + spacingY));
            return new Vector2(horizontalX, horizontalY);
        }

        protected override void PrepareItemRect(RectTransform itemRect)
        {
            if (itemRect == null)
                return;

            var cellSize = GetPrimaryTemplateSize();
            var crossAxisCount = GetCrossAxisCount();
            itemRect.anchorMin = new Vector2(0f, 1f);
            itemRect.anchorMax = new Vector2(0f, 1f);
            itemRect.pivot = new Vector2(0f, 1f);
            itemRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, ResolveCellWidth(cellSize.x, crossAxisCount));
            itemRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ResolveCellHeight(cellSize.y, crossAxisCount));
        }

        protected override float GetScrollOffset()
        {
            if (ContentRect == null)
                return 0f;

            if (IsVertical)
                return Mathf.Max(0f, ContentRect.anchoredPosition.y);

            return Mathf.Max(0f, -ContentRect.anchoredPosition.x);
        }

        protected override void SetScrollOffset(float value)
        {
            if (ContentRect == null)
                return;

            value = Mathf.Clamp(value, 0f, GetMaxScrollOffset());
            var anchoredPosition = ContentRect.anchoredPosition;
            if (IsVertical)
                anchoredPosition.y = value;
            else
                anchoredPosition.x = -value;

            ContentRect.anchoredPosition = anchoredPosition;
        }

        protected override float GetMaxScrollOffset()
        {
            if (IsVertical)
                return Mathf.Max(0f, ContentHeight - ViewportSize.y);

            return Mathf.Max(0f, ContentWidth - ViewportSize.x);
        }

        protected override float CalculateScrollOffsetForItem(int itemIndex, float offset)
        {
            var crossAxisCount = GetCrossAxisCount();
            var lineIndex = itemIndex / crossAxisCount;
            return (IsVertical ? paddingTop : paddingLeft) + lineIndex * GetLineStride() + offset;
        }

        private int GetCrossAxisCount()
        {
            return Mathf.Max(1, constraintCount);
        }

        private float GetLineStride()
        {
            var cellSize = GetPrimaryTemplateSize();
            return IsVertical
                ? ResolveCellHeight(cellSize.y, GetCrossAxisCount()) + spacingY
                : ResolveCellWidth(cellSize.x, GetCrossAxisCount()) + spacingX;
        }

        private static int GetLineCount(int itemCount, int crossAxisCount)
        {
            if (itemCount <= 0)
                return 0;

            return Mathf.CeilToInt(itemCount / (float)Mathf.Max(1, crossAxisCount));
        }

        private LoopScrollViewItem ResolveItemByIndex(int itemIndex)
        {
            return itemProvider != null ? itemProvider(this, itemIndex) : null;
        }

        private float ResolveCellWidth(float templateWidth, int crossAxisCount)
        {
            if (!IsVertical || crossAxisCellSizeMode != CrossAxisCellSizeMode.StretchToViewport)
                return templateWidth;

            var availableWidth = Mathf.Max(0f, ViewportSize.x - paddingLeft - paddingRight - Mathf.Max(0, crossAxisCount - 1) * spacingX);
            return crossAxisCount > 0 ? availableWidth / crossAxisCount : templateWidth;
        }

        private float ResolveCellHeight(float templateHeight, int crossAxisCount)
        {
            if (IsVertical || crossAxisCellSizeMode != CrossAxisCellSizeMode.StretchToViewport)
                return templateHeight;

            var availableHeight = Mathf.Max(0f, ViewportSize.y - paddingTop - paddingBottom - Mathf.Max(0, crossAxisCount - 1) * spacingY);
            return crossAxisCount > 0 ? availableHeight / crossAxisCount : templateHeight;
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

        private bool IsVertical => constraintMode == GridConstraintMode.ColumnCount;
    }
}
