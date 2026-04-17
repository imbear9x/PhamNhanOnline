using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Common
{
    public sealed class UIButtonView : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerClickHandler,
        ISubmitHandler
    {
        private const float HoverScaleMultiplier = 1.1f;
        private const float PressedScaleMultiplier = 0.9f;
        private static readonly Vector2 PressedOffset = new Vector2(2f, -2f);

        private enum VisualState
        {
            Normal = 0,
            Highlighted = 1,
            Pressed = 2,
            Disabled = 3
        }

        private enum TransitionMode
        {
            Color = 0,
            Sprite = 1
        }

        [Header("State")]
        [SerializeField] private bool interactable = true;

        [Header("References")]
        [SerializeField] private Image targetImage;
        [SerializeField] private RectTransform animationTarget;

        [Header("Transition")]
        [SerializeField] private TransitionMode transitionMode = TransitionMode.Color;

        [Header("Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
        [SerializeField] private Color pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        [SerializeField] private Color disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.5f);

        [Header("Sprites")]
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite highlightedSprite;
        [SerializeField] private Sprite pressedSprite;
        [SerializeField] private Sprite disabledSprite;

        [Header("Behavior")]
        [SerializeField] private bool leftClickEnabled = true;
        [SerializeField] private bool rightClickEnabled;

        [Header("Events")]
        [SerializeField] private UnityEvent onClick;
        [SerializeField] private UnityEvent onRightClick;

        private bool isPointerInside;
        private bool isPressed;
        private VisualState currentState = VisualState.Normal;
        private RectTransform cachedRectTransform;
        private RectTransform cachedAnimationRectTransform;
        private Vector3 baseLocalScale = Vector3.one;
        private Vector3 baseLocalPosition = Vector3.zero;
        private Vector2 baseAnchoredPosition = Vector2.zero;
        private bool hasCapturedBaseTransform;

        public event Action Clicked;
        public event Action RightClicked;

        public bool Interactable => interactable;

        private void Awake()
        {
            AutoWireReferences();
            CaptureBaseTransform();
            RefreshVisualState(force: true);
        }

        private void OnEnable()
        {
            CaptureBaseTransform();
            RefreshVisualState(force: true);
        }

        private void OnDisable()
        {
            isPressed = false;
            isPointerInside = false;
            RefreshVisualState(force: true);
        }

        public void SetInteractable(bool value, bool force = false)
        {
            if (!force && interactable == value)
                return;

            interactable = value;
            if (!interactable)
                isPressed = false;

            RefreshVisualState(force: true);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerInside = true;
            RefreshVisualState(force: false);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerInside = false;
            isPressed = false;
            RefreshVisualState(force: false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!interactable || eventData == null)
                return;

            if (eventData.button == PointerEventData.InputButton.Left && leftClickEnabled)
            {
                isPressed = true;
                RefreshVisualState(force: false);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isPressed)
                return;

            isPressed = false;
            RefreshVisualState(force: false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!interactable || eventData == null)
                return;

            if (eventData.button == PointerEventData.InputButton.Left && leftClickEnabled)
            {
                eventData.Use();
                InvokeLeftClick();
                return;
            }

            if (eventData.button == PointerEventData.InputButton.Right && rightClickEnabled)
            {
                eventData.Use();
                InvokeRightClick();
            }
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (!interactable || !leftClickEnabled)
                return;

            InvokeLeftClick();
        }

        public void TriggerClick()
        {
            if (!interactable || !leftClickEnabled)
                return;

            InvokeLeftClick();
        }

        private void InvokeLeftClick()
        {
            onClick?.Invoke();
            Clicked?.Invoke();
        }

        private void InvokeRightClick()
        {
            onRightClick?.Invoke();
            RightClicked?.Invoke();
        }

        private void AutoWireReferences()
        {
            if (targetImage == null)
                targetImage = GetComponent<Image>();

            if (cachedRectTransform == null)
                cachedRectTransform = transform as RectTransform;

            if (animationTarget == null)
            {
                var rootRectTransform = transform as RectTransform;
                if (targetImage != null && targetImage.rectTransform != null && targetImage.rectTransform != rootRectTransform)
                    animationTarget = targetImage.rectTransform;
                else
                    animationTarget = rootRectTransform;
            }

            if (cachedAnimationRectTransform == null)
                cachedAnimationRectTransform = animationTarget;
        }

        private void CaptureBaseTransform()
        {
            if (hasCapturedBaseTransform)
                return;

            hasCapturedBaseTransform = true;
            var transformTarget = cachedAnimationRectTransform != null
                ? (Transform)cachedAnimationRectTransform
                : transform;

            baseLocalScale = transformTarget.localScale;
            baseLocalPosition = transformTarget.localPosition;
            if (cachedAnimationRectTransform != null)
                baseAnchoredPosition = cachedAnimationRectTransform.anchoredPosition;
        }

        private void RefreshVisualState(bool force)
        {
            var nextState = ResolveVisualState();
            if (!force && nextState == currentState)
                return;

            currentState = nextState;
            ApplyVisualState(nextState);
        }

        private VisualState ResolveVisualState()
        {
            if (!interactable)
                return VisualState.Disabled;

            if (isPressed)
                return VisualState.Pressed;

            if (isPointerInside)
                return VisualState.Highlighted;

            return VisualState.Normal;
        }

        private void ApplyVisualState(VisualState state)
        {
            if (targetImage == null)
            {
                ApplyTransformState(state);
                return;
            }

            switch (transitionMode)
            {
                case TransitionMode.Sprite:
                    targetImage.sprite = ResolveSprite(state, targetImage.sprite);
                    break;
                default:
                    targetImage.color = ResolveColor(state);
                    break;
            }

            ApplyTransformState(state);
        }

        private void ApplyTransformState(VisualState state)
        {
            if (!CanAnimateTransform())
            {
                ResetAnimatedTransform();
                return;
            }

            var scaleMultiplier = 1f;
            var positionOffset = Vector2.zero;

            switch (state)
            {
                case VisualState.Highlighted:
                    scaleMultiplier = HoverScaleMultiplier;
                    break;
                case VisualState.Pressed:
                    scaleMultiplier = PressedScaleMultiplier;
                    positionOffset = PressedOffset;
                    break;
            }

            var transformTarget = cachedAnimationRectTransform != null
                ? (Transform)cachedAnimationRectTransform
                : transform;
            transformTarget.localScale = baseLocalScale * scaleMultiplier;
            if (cachedAnimationRectTransform != null)
            {
                cachedAnimationRectTransform.anchoredPosition = baseAnchoredPosition + positionOffset;
                return;
            }

            transformTarget.localPosition = baseLocalPosition + new Vector3(positionOffset.x, positionOffset.y, 0f);
        }

        private bool CanAnimateTransform()
        {
            if (!hasCapturedBaseTransform)
                return false;

            if (animationTarget == null)
                return false;

            var rootRectTransform = transform as RectTransform;
            if (animationTarget != rootRectTransform)
                return true;

            return !IsDrivenByLayoutGroup(rootRectTransform);
        }

        private void ResetAnimatedTransform()
        {
            var transformTarget = cachedAnimationRectTransform != null
                ? (Transform)cachedAnimationRectTransform
                : transform;

            transformTarget.localScale = baseLocalScale;
            if (cachedAnimationRectTransform != null)
            {
                cachedAnimationRectTransform.anchoredPosition = baseAnchoredPosition;
                return;
            }

            transformTarget.localPosition = baseLocalPosition;
        }

        private static bool IsDrivenByLayoutGroup(RectTransform rectTransform)
        {
            if (rectTransform == null)
                return false;

            for (var current = rectTransform.parent; current != null; current = current.parent)
            {
                if (current.GetComponent<LayoutGroup>() != null)
                    return true;
            }

            return false;
        }

        private Sprite ResolveSprite(VisualState state, Sprite fallbackSprite)
        {
            switch (state)
            {
                case VisualState.Highlighted:
                    return highlightedSprite != null ? highlightedSprite : normalSprite != null ? normalSprite : fallbackSprite;
                case VisualState.Pressed:
                    return pressedSprite != null ? pressedSprite : highlightedSprite != null ? highlightedSprite : normalSprite != null ? normalSprite : fallbackSprite;
                case VisualState.Disabled:
                    return disabledSprite != null ? disabledSprite : normalSprite != null ? normalSprite : fallbackSprite;
                default:
                    return normalSprite != null ? normalSprite : fallbackSprite;
            }
        }

        private Color ResolveColor(VisualState state)
        {
            switch (state)
            {
                case VisualState.Highlighted:
                    return highlightedColor;
                case VisualState.Pressed:
                    return pressedColor;
                case VisualState.Disabled:
                    return disabledColor;
                default:
                    return normalColor;
            }
        }
    }
}
