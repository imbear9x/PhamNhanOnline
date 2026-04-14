using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PhamNhanOnline.Client.UI.Common
{
    public sealed class LoopScrollBarView : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        public enum Direction
        {
            Vertical = 0,
            Horizontal = 1,
        }

        [Header("References")]
        [SerializeField] private RectTransform trackRect;
        [SerializeField] private RectTransform handleRect;

        [Header("Behavior")]
        [SerializeField] private Direction direction = Direction.Vertical;
        [SerializeField] private bool hideWhenNotScrollable;

        private CanvasGroup canvasGroup;
        private float normalizedValue;
        private bool scrollable = true;
        private bool draggingHandle;
        private float dragPointerOffset;

        public event Action<float> ValueChanged;

        public float NormalizedValue => normalizedValue;

        private void Awake()
        {
            if (trackRect == null)
                trackRect = transform as RectTransform;

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            PrepareHandleRect();
            ApplyHandlePosition();
        }

        private void Start()
        {
            ValidateSerializedReferences();
        }

        public void SetState(float normalized, bool canScroll)
        {
            scrollable = canScroll;
            normalizedValue = Mathf.Clamp01(normalized);
            ApplyHandlePosition();
            ApplyVisibility();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!scrollable || eventData == null || handleRect == null || trackRect == null)
                return;

            draggingHandle = RectTransformUtility.RectangleContainsScreenPoint(handleRect, eventData.position, eventData.pressEventCamera);
            if (TryGetPointerLocalPoint(eventData, out var localPoint))
            {
                if (draggingHandle)
                    dragPointerOffset = GetHandleAxisPosition() - GetAxisCoordinate(localPoint);
                else
                    dragPointerOffset = 0f;

                UpdateValueFromLocalPoint(localPoint);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!scrollable || eventData == null || !TryGetPointerLocalPoint(eventData, out var localPoint))
                return;

            UpdateValueFromLocalPoint(localPoint);
        }

        private void UpdateValueFromLocalPoint(Vector2 localPoint)
        {
            var nextValue = CalculateNormalizedFromLocalPoint(localPoint, draggingHandle ? dragPointerOffset : 0f);
            if (Mathf.Approximately(nextValue, normalizedValue))
                return;

            normalizedValue = nextValue;
            ApplyHandlePosition();
            ValueChanged?.Invoke(normalizedValue);
        }

        private void ApplyHandlePosition()
        {
            if (handleRect == null || trackRect == null)
                return;

            PrepareHandleRect();
            var anchoredPosition = handleRect.anchoredPosition;
            if (direction == Direction.Vertical)
            {
                anchoredPosition.y = Mathf.Lerp(GetVerticalCenterMax(), GetVerticalCenterMin(), normalizedValue);
                anchoredPosition.x = 0f;
            }
            else
            {
                anchoredPosition.x = Mathf.Lerp(GetHorizontalCenterMin(), GetHorizontalCenterMax(), normalizedValue);
                anchoredPosition.y = 0f;
            }

            handleRect.anchoredPosition = anchoredPosition;
        }

        private void ApplyVisibility()
        {
            if (canvasGroup == null)
                return;

            var visible = !hideWhenNotScrollable || scrollable;
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = visible && scrollable;
            canvasGroup.interactable = visible && scrollable;
        }

        private void PrepareHandleRect()
        {
            if (handleRect == null)
                return;

            handleRect.SetParent(trackRect, false);
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
        }

        private float CalculateNormalizedFromLocalPoint(Vector2 localPoint, float pointerOffset)
        {
            if (direction == Direction.Vertical)
            {
                var center = Mathf.Clamp(GetAxisCoordinate(localPoint) + pointerOffset, GetVerticalCenterMin(), GetVerticalCenterMax());
                return Mathf.InverseLerp(GetVerticalCenterMax(), GetVerticalCenterMin(), center);
            }

            var horizontalCenter = Mathf.Clamp(GetAxisCoordinate(localPoint) + pointerOffset, GetHorizontalCenterMin(), GetHorizontalCenterMax());
            return Mathf.InverseLerp(GetHorizontalCenterMin(), GetHorizontalCenterMax(), horizontalCenter);
        }

        private float GetHandleAxisPosition()
        {
            return direction == Direction.Vertical
                ? handleRect.anchoredPosition.y
                : handleRect.anchoredPosition.x;
        }

        private float GetAxisCoordinate(Vector2 localPoint)
        {
            return direction == Direction.Vertical ? localPoint.y : localPoint.x;
        }

        private float GetVerticalCenterMax()
        {
            return Mathf.Max(0f, (trackRect.rect.height - handleRect.rect.height) * 0.5f);
        }

        private float GetVerticalCenterMin()
        {
            return -GetVerticalCenterMax();
        }

        private float GetHorizontalCenterMin()
        {
            return -Mathf.Max(0f, (trackRect.rect.width - handleRect.rect.width) * 0.5f);
        }

        private float GetHorizontalCenterMax()
        {
            return -GetHorizontalCenterMin();
        }

        private bool TryGetPointerLocalPoint(PointerEventData eventData, out Vector2 localPoint)
        {
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                trackRect,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint);
        }

        private void ValidateSerializedReferences()
        {
            if (trackRect == null)
                throw new InvalidOperationException($"{nameof(LoopScrollBarView)} on '{gameObject.name}' is missing required reference '{nameof(trackRect)}'.");
            if (handleRect == null)
                throw new InvalidOperationException($"{nameof(LoopScrollBarView)} on '{gameObject.name}' is missing required reference '{nameof(handleRect)}'.");
        }
    }
}
