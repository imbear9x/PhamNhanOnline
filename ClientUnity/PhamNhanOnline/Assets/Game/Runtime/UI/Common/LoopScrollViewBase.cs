using System;
using System.Collections.Generic;
using PhamNhanOnline.Client.Core.Logging;
using UnityEngine;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Common
{
    public abstract class LoopScrollViewBase : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform viewportRect;
        [SerializeField] private RectTransform contentRect;
        [SerializeField] private LoopScrollViewItem[] itemTemplates = Array.Empty<LoopScrollViewItem>();
        [SerializeField] private LoopScrollBarView customScrollbarView;

        [Header("Behavior")]
        [SerializeField] private bool hideTemplateObjects = true;

        private readonly Dictionary<int, LoopScrollViewItem> visibleItems = new Dictionary<int, LoopScrollViewItem>(32);
        private readonly Dictionary<string, Stack<LoopScrollViewItem>> recycledItemsByPrefabName = new Dictionary<string, Stack<LoopScrollViewItem>>(StringComparer.Ordinal);
        private readonly Dictionary<string, LoopScrollViewItem> templatesByPrefabName = new Dictionary<string, LoopScrollViewItem>(StringComparer.Ordinal);
        private readonly List<string> templateOrder = new List<string>(4);

        private Func<int, LoopScrollViewItem> itemProvider;
        private bool templatesPrepared;
        private bool isInitialized;
        private bool isRefreshing;
        private Vector2 lastViewportSize = new Vector2(-1f, -1f);

        public int ItemTotalCount { get; private set; }

        public bool IsInitialized => isInitialized;

        protected RectTransform ContentRect => contentRect;
        protected Vector2 ViewportSize => viewportRect != null ? viewportRect.rect.size : Vector2.zero;
        protected float ContentWidth => contentRect != null ? contentRect.rect.width : 0f;
        protected float ContentHeight => contentRect != null ? contentRect.rect.height : 0f;

        protected virtual void Awake()
        {
            ResolveSerializedReferences();
            PrepareTemplates();
            ConfigureContentRect();
        }

        protected virtual void Start()
        {
            ValidateSerializedReferences();
            UpdateContentSize();
            RefreshVisibleItems(forceRebuild: true);
        }

        protected virtual void OnEnable()
        {
            if (scrollRect != null)
            {
                scrollRect.onValueChanged.RemoveListener(HandleScrollValueChanged);
                scrollRect.onValueChanged.AddListener(HandleScrollValueChanged);
            }

            if (customScrollbarView != null)
            {
                customScrollbarView.ValueChanged -= HandleCustomScrollbarValueChanged;
                customScrollbarView.ValueChanged += HandleCustomScrollbarValueChanged;
            }

            RefreshVisibleItems(forceRebuild: true);
            RefreshCustomScrollbar();
        }

        protected virtual void OnDisable()
        {
            if (scrollRect != null)
                scrollRect.onValueChanged.RemoveListener(HandleScrollValueChanged);

            if (customScrollbarView != null)
                customScrollbarView.ValueChanged -= HandleCustomScrollbarValueChanged;
        }

        protected virtual void LateUpdate()
        {
            if (!isInitialized || viewportRect == null)
                return;

            var viewportSize = viewportRect.rect.size;
            if (!Approximately(viewportSize, lastViewportSize))
            {
                UpdateContentSize();
                RefreshVisibleItems(forceRebuild: true);
            }
        }

        protected virtual void OnDestroy()
        {
            if (scrollRect != null)
                scrollRect.onValueChanged.RemoveListener(HandleScrollValueChanged);

            if (customScrollbarView != null)
                customScrollbarView.ValueChanged -= HandleCustomScrollbarValueChanged;
        }

        public LoopScrollViewItem GetShownItemByItemIndex(int itemIndex)
        {
            return visibleItems.TryGetValue(itemIndex, out var item) ? item : null;
        }

        public LoopScrollViewItem NewListViewItem(string itemPrefabName = null)
        {
            PrepareTemplates();
            var prefabName = string.IsNullOrWhiteSpace(itemPrefabName)
                ? GetDefaultTemplatePrefabName()
                : itemPrefabName.Trim();
            if (string.IsNullOrWhiteSpace(prefabName))
            {
                ClientLog.Error($"{GetType().Name} on '{gameObject.name}' has no registered loop item template.");
                return null;
            }

            if (!templatesByPrefabName.TryGetValue(prefabName, out var template) || template == null)
            {
                ClientLog.Error($"{GetType().Name} on '{gameObject.name}' could not find template '{prefabName}'.");
                return null;
            }

            if (recycledItemsByPrefabName.TryGetValue(prefabName, out var pool))
            {
                while (pool.Count > 0)
                {
                    var recycled = pool.Pop();
                    if (recycled != null)
                    {
                        recycled.gameObject.SetActive(true);
                        return recycled;
                    }
                }
            }

            var instance = Instantiate(template, contentRect);
            instance.name = template.gameObject.name;
            instance.gameObject.SetActive(true);
            return instance;
        }

        public void RefreshAllShownItem()
        {
            UpdateContentSize();
            RefreshVisibleItems(forceRebuild: true);
        }

        public void MovePanelToItemIndex(int itemIndex, float offset = 0f)
        {
            if (ItemTotalCount <= 0)
                return;

            var clampedIndex = Mathf.Clamp(itemIndex, 0, ItemTotalCount - 1);
            var scrollOffset = Mathf.Clamp(CalculateScrollOffsetForItem(clampedIndex, offset), 0f, GetMaxScrollOffset());
            SetScrollOffset(scrollOffset);
            RefreshVisibleItems(forceRebuild: true);
            RefreshCustomScrollbar();
        }

        protected void InitInternal(int itemCount, Func<int, LoopScrollViewItem> onGetItemByIndex)
        {
            itemProvider = onGetItemByIndex;
            isInitialized = onGetItemByIndex != null;
            SetListItemCountInternal(itemCount, keepPosition: false);
        }

        protected void SetListItemCountInternal(int itemCount, bool keepPosition = false)
        {
            ItemTotalCount = Mathf.Max(0, itemCount);
            UpdateContentSize();
            if (!keepPosition)
                SetScrollOffset(0f);
            else
                SetScrollOffset(Mathf.Clamp(GetScrollOffset(), 0f, GetMaxScrollOffset()));

            RefreshVisibleItems(forceRebuild: true);
        }

        protected Vector2 GetPrimaryTemplateSize()
        {
            PrepareTemplates();
            var templateName = GetDefaultTemplatePrefabName();
            if (string.IsNullOrWhiteSpace(templateName) || !templatesByPrefabName.TryGetValue(templateName, out var template) || template == null)
                return Vector2.zero;

            return ResolveTemplateSize(template);
        }

        protected abstract int GetVisibleItemStartIndex(float scrollOffset);

        protected abstract int GetVisibleItemEndIndex(float scrollOffset, int itemCount);

        protected abstract Vector2 CalculateContentSize(int itemCount);

        protected abstract Vector2 CalculateItemAnchoredPosition(int itemIndex);

        protected abstract void PrepareItemRect(RectTransform itemRect);

        protected abstract float GetScrollOffset();

        protected abstract void SetScrollOffset(float value);

        protected abstract float GetMaxScrollOffset();

        protected abstract float CalculateScrollOffsetForItem(int itemIndex, float offset);

        protected virtual void ConfigureContentRect()
        {
            if (contentRect == null)
                return;

            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(0f, 1f);
            contentRect.pivot = new Vector2(0f, 1f);
        }

        protected virtual void ValidateSerializedReferences()
        {
            ThrowIfMissing(scrollRect, nameof(scrollRect));
            ThrowIfMissing(viewportRect, nameof(viewportRect));
            ThrowIfMissing(contentRect, nameof(contentRect));
            if (itemTemplates == null || itemTemplates.Length == 0)
                throw new InvalidOperationException($"{GetType().Name} on '{gameObject.name}' is missing '{nameof(itemTemplates)}'.");

            var validTemplateCount = 0;
            for (var i = 0; i < itemTemplates.Length; i++)
            {
                if (itemTemplates[i] != null)
                    validTemplateCount++;
            }

            if (validTemplateCount == 0)
                throw new InvalidOperationException($"{GetType().Name} on '{gameObject.name}' requires at least one non-null item template.");

            if (contentRect.GetComponent<LayoutGroup>() != null || contentRect.GetComponent<ContentSizeFitter>() != null)
            {
                ClientLog.Warn($"{GetType().Name} on '{gameObject.name}' manages content layout manually. Remove LayoutGroup/ContentSizeFitter from '{contentRect.name}'.");
            }
        }

        protected void ThrowIfMissing(UnityEngine.Object value, string fieldName)
        {
            if (value == null)
                throw new InvalidOperationException($"{GetType().Name} on '{gameObject.name}' is missing required reference '{fieldName}'.");
        }

        private void ResolveSerializedReferences()
        {
            if (scrollRect == null)
                scrollRect = GetComponent<ScrollRect>();

            if (scrollRect != null)
            {
                if (viewportRect == null)
                    viewportRect = scrollRect.viewport != null ? scrollRect.viewport : scrollRect.GetComponent<RectTransform>();

                if (contentRect == null)
                    contentRect = scrollRect.content;
            }
        }

        private void PrepareTemplates()
        {
            if (templatesPrepared)
                return;

            templatesPrepared = true;
            templatesByPrefabName.Clear();
            templateOrder.Clear();

            if (itemTemplates == null)
                return;

            for (var i = 0; i < itemTemplates.Length; i++)
            {
                var template = itemTemplates[i];
                if (template == null)
                    continue;

                var prefabName = template.ItemPrefabName;
                if (string.IsNullOrWhiteSpace(prefabName))
                {
                    ClientLog.Warn($"{GetType().Name} on '{gameObject.name}' has an item template with empty prefab name at index {i}.");
                    continue;
                }

                prefabName = prefabName.Trim();
                if (templatesByPrefabName.ContainsKey(prefabName))
                {
                    ClientLog.Warn($"{GetType().Name} on '{gameObject.name}' has duplicate item template name '{prefabName}'.");
                    continue;
                }

                templatesByPrefabName.Add(prefabName, template);
                templateOrder.Add(prefabName);

                if (hideTemplateObjects && template.gameObject.activeSelf)
                    template.gameObject.SetActive(false);
            }
        }

        private void HandleScrollValueChanged(Vector2 _)
        {
            RefreshVisibleItems(forceRebuild: false);
            RefreshCustomScrollbar();
        }

        private void UpdateContentSize()
        {
            if (contentRect == null)
                return;

            var size = CalculateContentSize(ItemTotalCount);
            contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(0f, size.x));
            contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(0f, size.y));
            lastViewportSize = viewportRect != null ? viewportRect.rect.size : Vector2.zero;
            RefreshCustomScrollbar();
        }

        private void RefreshVisibleItems(bool forceRebuild)
        {
            if (isRefreshing || contentRect == null)
                return;

            isRefreshing = true;
            try
            {
                if (!isInitialized || itemProvider == null || ItemTotalCount <= 0)
                {
                    RecycleAllVisibleItems();
                    return;
                }

                var scrollOffset = Mathf.Clamp(GetScrollOffset(), 0f, GetMaxScrollOffset());
                var startIndex = Mathf.Clamp(GetVisibleItemStartIndex(scrollOffset), 0, Mathf.Max(0, ItemTotalCount - 1));
                var endIndex = Mathf.Clamp(GetVisibleItemEndIndex(scrollOffset, ItemTotalCount), 0, Mathf.Max(0, ItemTotalCount - 1));

                if (forceRebuild)
                    RecycleAllVisibleItems();
                else
                    RecycleItemsOutsideRange(startIndex, endIndex);

                for (var itemIndex = startIndex; itemIndex <= endIndex; itemIndex++)
                {
                    if (!forceRebuild && visibleItems.ContainsKey(itemIndex))
                        continue;

                    var item = itemProvider(itemIndex);
                    if (item == null)
                        continue;

                    AttachVisibleItem(itemIndex, item);
                }
            }
            finally
            {
                isRefreshing = false;
            }
        }

        private void AttachVisibleItem(int itemIndex, LoopScrollViewItem item)
        {
            if (item == null)
                return;

            RemoveExistingVisibleEntry(item);
            visibleItems[itemIndex] = item;
            item.ItemIndex = itemIndex;
            item.transform.SetParent(contentRect, false);
            item.gameObject.SetActive(true);
            PositionItem(item, itemIndex);
            item.OnItemVisible(itemIndex);
        }

        private void PositionItem(LoopScrollViewItem item, int itemIndex)
        {
            var itemRect = item.RectTransform;
            if (itemRect == null)
                return;

            PrepareItemRect(itemRect);
            itemRect.anchoredPosition = CalculateItemAnchoredPosition(itemIndex);
        }

        private void RemoveExistingVisibleEntry(LoopScrollViewItem item)
        {
            if (item == null || visibleItems.Count == 0)
                return;

            var staleKeys = new List<int>(4);
            foreach (var pair in visibleItems)
            {
                if (pair.Value == item)
                    staleKeys.Add(pair.Key);
            }

            for (var i = 0; i < staleKeys.Count; i++)
                visibleItems.Remove(staleKeys[i]);
        }

        private void RecycleItemsOutsideRange(int startIndex, int endIndex)
        {
            if (visibleItems.Count == 0)
                return;

            var recycleKeys = new List<int>(visibleItems.Count);
            foreach (var pair in visibleItems)
            {
                if (pair.Key < startIndex || pair.Key > endIndex)
                    recycleKeys.Add(pair.Key);
            }

            for (var i = 0; i < recycleKeys.Count; i++)
                RecycleVisibleItem(recycleKeys[i]);
        }

        private void RecycleAllVisibleItems()
        {
            if (visibleItems.Count == 0)
                return;

            var recycleKeys = new List<int>(visibleItems.Count);
            foreach (var pair in visibleItems)
                recycleKeys.Add(pair.Key);

            for (var i = 0; i < recycleKeys.Count; i++)
                RecycleVisibleItem(recycleKeys[i]);
        }

        private void RecycleVisibleItem(int itemIndex)
        {
            if (!visibleItems.TryGetValue(itemIndex, out var item) || item == null)
            {
                visibleItems.Remove(itemIndex);
                return;
            }

            visibleItems.Remove(itemIndex);
            item.OnItemRecycled();
            item.ItemIndex = -1;
            item.gameObject.SetActive(false);

            var prefabName = item.ItemPrefabName;
            if (!recycledItemsByPrefabName.TryGetValue(prefabName, out var pool))
            {
                pool = new Stack<LoopScrollViewItem>();
                recycledItemsByPrefabName.Add(prefabName, pool);
            }

            pool.Push(item);
        }

        private static Vector2 ResolveTemplateSize(LoopScrollViewItem template)
        {
            if (template == null || template.RectTransform == null)
                return Vector2.zero;

            var size = template.RectTransform.rect.size;
            var layoutElement = template.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                if (layoutElement.preferredWidth > 0f)
                    size.x = layoutElement.preferredWidth;

                if (layoutElement.preferredHeight > 0f)
                    size.y = layoutElement.preferredHeight;
            }

            return size;
        }

        private string GetDefaultTemplatePrefabName()
        {
            return templateOrder.Count > 0 ? templateOrder[0] : string.Empty;
        }

        private static bool Approximately(Vector2 a, Vector2 b)
        {
            return Mathf.Abs(a.x - b.x) <= 0.01f && Mathf.Abs(a.y - b.y) <= 0.01f;
        }

        private void HandleCustomScrollbarValueChanged(float normalizedValue)
        {
            SetScrollOffset(Mathf.Lerp(0f, GetMaxScrollOffset(), Mathf.Clamp01(normalizedValue)));
            RefreshVisibleItems(forceRebuild: false);
            RefreshCustomScrollbar();
        }

        private void RefreshCustomScrollbar()
        {
            if (customScrollbarView == null)
                return;

            var maxScrollOffset = GetMaxScrollOffset();
            var normalizedValue = maxScrollOffset > 0f
                ? Mathf.Clamp01(GetScrollOffset() / maxScrollOffset)
                : 0f;
            customScrollbarView.SetState(normalizedValue, maxScrollOffset > 0.01f);
        }
    }
}
