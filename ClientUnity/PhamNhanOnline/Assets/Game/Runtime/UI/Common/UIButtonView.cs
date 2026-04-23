using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Common
{
    public class UIButtonView : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerClickHandler,
        ISubmitHandler
    {
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

        [Header("Animation")]
        [SerializeField] private bool animHover;
        [SerializeField] private bool animClick = true;
        [SerializeField] private float hoverScaleMultiplier = 1.1f;
        [SerializeField] private float pressedScaleMultiplier = 0.9f;
        [SerializeField] private Vector2 pressedOffset = new Vector2(2f, -2f);
        [SerializeField] private float colorTweenDuration = 0.1f;
        [SerializeField] private float transformTweenDuration = 0.12f;

        [Header("Events")]
        [SerializeField] private UnityEvent onClick;
        [SerializeField] private UnityEvent onRightClick;

        private bool isPointerInside;
        private bool isPressed;
        private bool isHighlightedState;
        private VisualState currentState = VisualState.Normal;
        private RectTransform cachedRectTransform;
        private RectTransform cachedAnimationRectTransform;
        private Vector3 baseLocalScale = Vector3.one;
        private Vector2 baseAnchoredPosition = Vector2.zero;
        private bool isAnimationReady;
        private Tween colorTween;
        private Tween scaleTween;
        private Tween positionTween;

        public event Action Clicked;
        public event Action RightClicked;

        public bool Interactable => interactable;

        public bool IsHighlightedState => isHighlightedState;

        private void Awake()
        {
            AutoWireReferences();
            RefreshVisualState(force: true);
        }

        private void Start()
        {
            EnsureAnimationReady(force: true);
            RefreshVisualState(force: true);
        }

        private void OnEnable()
        {
            AutoWireReferences();
            isAnimationReady = false;
            RefreshVisualState(force: true);
        }

        private void OnDisable()
        {
            isPressed = false;
            isPointerInside = false;
            KillTweens();
            ApplyVisualState(ResolveVisualState(), force: true);
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

        public void SetHighlightState(bool value, bool force = false)
        {
            if (!force && isHighlightedState == value)
                return;

            EnsureAnimationReady();
            isHighlightedState = value;
            RefreshVisualState(force: true);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            EnsureAnimationReady();
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
                EnsureAnimationReady();
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

        protected virtual void InvokeLeftClick()
        {
            onClick?.Invoke();
            Clicked?.Invoke();
        }

        protected virtual void InvokeRightClick()
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

            cachedAnimationRectTransform = animationTarget;
        }

        private void EnsureAnimationReady(bool force = false)
        {
            if (isAnimationReady && !force)
                return;

            AutoWireReferences();
            if (cachedAnimationRectTransform == null)
                return;

            baseLocalScale = cachedAnimationRectTransform.localScale;
            baseAnchoredPosition = cachedAnimationRectTransform.anchoredPosition;
            isAnimationReady = true;
        }

        private void RefreshVisualState(bool force)
        {
            var nextState = ResolveVisualState();
            if (!force && nextState == currentState)
                return;

            currentState = nextState;
            ApplyVisualState(nextState, force);
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

        private void ApplyVisualState(VisualState state, bool force)
        {
            if (targetImage == null)
            {
                ApplyTransformState(state, force);
                return;
            }

            switch (transitionMode)
            {
                case TransitionMode.Sprite:
                    if (colorTween != null)
                    {
                        colorTween.Kill();
                        colorTween = null;
                    }

                    targetImage.sprite = ResolveSprite(state, targetImage.sprite);
                    break;
                default:
                    ApplyColorState(state, force);
                    break;
            }

            ApplyTransformState(state, force);
        }

        private void ApplyColorState(VisualState state, bool force)
        {
            var targetColor = ResolveColor(state);
            if (targetImage == null)
                return;

            if (force || !gameObject.activeInHierarchy)
            {
                if (colorTween != null)
                {
                    colorTween.Kill();
                    colorTween = null;
                }

                targetImage.color = targetColor;
                return;
            }

            if (colorTween != null && colorTween.IsActive())
                colorTween.Kill();

            colorTween = DOTween
                .To(() => targetImage.color, value => targetImage.color = value, targetColor, colorTweenDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }

        private void ApplyTransformState(VisualState state, bool force)
        {
            if (!CanAnimateTransform())
            {
                ResetAnimatedTransform();
                return;
            }

            var scaleMultiplier = 1f;
            var positionOffset = Vector2.zero;

            if (isHighlightedState)
                scaleMultiplier = hoverScaleMultiplier;

            switch (state)
            {
                case VisualState.Highlighted:
                    if (animHover)
                        scaleMultiplier = hoverScaleMultiplier;
                    break;
                case VisualState.Pressed:
                    if (animClick)
                    {
                        if (!isHighlightedState)
                            scaleMultiplier = pressedScaleMultiplier;

                        positionOffset = pressedOffset;
                    }
                    break;
            }

            if (cachedAnimationRectTransform == null)
                return;

            var targetScale = baseLocalScale * scaleMultiplier;
            var targetAnchoredPosition = baseAnchoredPosition + positionOffset;
            if (force || !gameObject.activeInHierarchy)
            {
                KillTransformTweens();
                cachedAnimationRectTransform.localScale = targetScale;
                cachedAnimationRectTransform.anchoredPosition = targetAnchoredPosition;
                return;
            }

            KillTransformTweens();
            scaleTween = cachedAnimationRectTransform
                .DOScale(targetScale, transformTweenDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
            positionTween = DOTween
                .To(
                    () => cachedAnimationRectTransform.anchoredPosition,
                    value => cachedAnimationRectTransform.anchoredPosition = value,
                    targetAnchoredPosition,
                    transformTweenDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }

        private bool CanAnimateTransform()
        {
            if (!animHover && !animClick)
                return false;

            if (!isAnimationReady)
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
            if (cachedAnimationRectTransform == null)
                return;

            KillTransformTweens();
            cachedAnimationRectTransform.localScale = baseLocalScale;
            cachedAnimationRectTransform.anchoredPosition = baseAnchoredPosition;
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

        private void KillTweens()
        {
            KillTransformTweens();
            if (colorTween != null)
            {
                colorTween.Kill();
                colorTween = null;
            }
        }

        private void KillTransformTweens()
        {
            if (scaleTween != null)
            {
                scaleTween.Kill();
                scaleTween = null;
            }

            if (positionTween != null)
            {
                positionTween.Kill();
                positionTween = null;
            }
        }
    }
}
