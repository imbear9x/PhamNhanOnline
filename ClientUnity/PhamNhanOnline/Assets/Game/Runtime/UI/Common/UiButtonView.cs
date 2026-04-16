using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Common
{
    public sealed class UiButtonView : MonoBehaviour,
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

        public event Action Clicked;
        public event Action RightClicked;

        public bool Interactable => interactable;

        private void Awake()
        {
            AutoWireReferences();
            RefreshVisualState(force: true);
        }

        private void OnEnable()
        {
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
                return;

            switch (transitionMode)
            {
                case TransitionMode.Sprite:
                    targetImage.sprite = ResolveSprite(state, targetImage.sprite);
                    break;
                default:
                    targetImage.color = ResolveColor(state);
                    break;
            }
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
