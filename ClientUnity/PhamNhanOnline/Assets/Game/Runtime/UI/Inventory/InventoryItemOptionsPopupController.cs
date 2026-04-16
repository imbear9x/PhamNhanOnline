using System;
using System.Collections.Generic;
using PhamNhanOnline.Client.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PhamNhanOnline.Client.UI.Inventory
{
    public sealed class InventoryItemOptionsPopupController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public readonly struct OptionEntry
        {
            public OptionEntry(string label, Action onClick, bool interactable = true)
            {
                Label = label;
                OnClick = onClick;
                Interactable = interactable;
            }

            public string Label { get; }
            public Action OnClick { get; }
            public bool Interactable { get; }
        }

        private sealed class RuntimeOptionButton
        {
            public GameObject Root;
            public UIButtonView Button;
            public TMP_Text Label;
            public Action ClickAction;

            public void Bind(string label, Action onClick, bool interactable)
            {
                ClickAction = onClick;
                if (Label != null)
                    Label.text = string.IsNullOrWhiteSpace(label) ? string.Empty : label.Trim();

                if (Button != null)
                    Button.SetInteractable(interactable, force: true);

                if (Root != null && !Root.activeSelf)
                    Root.SetActive(true);
            }

            public void Clear()
            {
                ClickAction = null;
                if (Root != null && Root.activeSelf)
                    Root.SetActive(false);
            }
        }

        [Header("References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Transform optionsRoot;
        [SerializeField] private GameObject optionTemplate;
        [SerializeField] private string defaultTitleText = "Lua chon";

        [Header("Layout")]
        [SerializeField] private Vector2 cursorOffsetBelow = new Vector2(20f, -20f);
        [SerializeField] private Vector2 cursorOffsetAbove = new Vector2(20f, 20f);
        [SerializeField] private Vector2 screenPadding = new Vector2(16f, 16f);

        private readonly List<RuntimeOptionButton> runtimeButtons = new List<RuntimeOptionButton>();

        public bool IsPointerInside { get; private set; }
        public bool IsVisible => panelRoot != null ? panelRoot.activeSelf : gameObject.activeSelf;

        private void Awake()
        {
            if (panelRoot == null)
                panelRoot = gameObject;

            AutoWireTemplateReferences();
        }

        public void Show(RectTransform anchor, string title, IReadOnlyList<OptionEntry> options, bool force = false)
        {
            if (anchor == null || options == null || options.Count == 0)
            {
                Hide(force);
                return;
            }

            if (titleText != null)
                titleText.text = defaultTitleText;

            ApplyButtons(options);
            PositionNearCursor();
            SetVisible(true, force);
        }

        public void Hide(bool force = false)
        {
            IsPointerInside = false;

            for (var i = 0; i < runtimeButtons.Count; i++)
                runtimeButtons[i].Clear();

            SetVisible(false, force);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            IsPointerInside = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            IsPointerInside = false;
        }

        private void AutoWireTemplateReferences()
        {
            if (panelRoot == null)
                return;

            if (titleText == null)
            {
                var texts = panelRoot.GetComponentsInChildren<TMP_Text>(true);
                for (var i = 0; i < texts.Length; i++)
                {
                    var text = texts[i];
                    if (text == null || text.GetComponentInParent<UIButtonView>(true) != null)
                        continue;

                    titleText = text;
                    break;
                }
            }

            if (optionTemplate == null)
            {
                var firstButton = panelRoot.GetComponentInChildren<UIButtonView>(true);
                if (firstButton != null)
                    optionTemplate = firstButton.gameObject;
            }

            if (optionsRoot == null && optionTemplate != null)
                optionsRoot = optionTemplate.transform.parent;

            if (optionTemplate == null || optionsRoot == null)
                return;

            for (var i = 0; i < optionsRoot.childCount; i++)
            {
                var child = optionsRoot.GetChild(i);
                if (child != optionTemplate.transform)
                    child.gameObject.SetActive(false);
            }

            optionTemplate.SetActive(false);
        }

        private void ApplyButtons(IReadOnlyList<OptionEntry> options)
        {
            EnsureButtonPool(options.Count);

            for (var i = 0; i < runtimeButtons.Count; i++)
            {
                if (i < options.Count)
                {
                    var option = options[i];
                    runtimeButtons[i].Bind(option.Label, option.OnClick, option.Interactable);
                }
                else
                {
                    runtimeButtons[i].Clear();
                }
            }
        }

        private void EnsureButtonPool(int requiredCount)
        {
            if (optionTemplate == null || optionsRoot == null)
                return;

            while (runtimeButtons.Count < requiredCount)
            {
                var buttonObject = Instantiate(optionTemplate, optionsRoot);
                buttonObject.name = $"{optionTemplate.name}_{runtimeButtons.Count}";
                buttonObject.SetActive(true);

                var buttonView = buttonObject.GetComponent<UIButtonView>();
                var label = buttonObject.GetComponentInChildren<TMP_Text>(true);
                var runtimeButton = new RuntimeOptionButton
                {
                    Root = buttonObject,
                    Button = buttonView,
                    Label = label
                };

                if (buttonView != null)
                {
                    buttonView.Clicked += () =>
                    {
                        var action = runtimeButton.ClickAction;
                        action?.Invoke();
                    };
                }

                runtimeButtons.Add(runtimeButton);
            }
        }

        private void SetVisible(bool visible, bool force)
        {
            var root = panelRoot != null ? panelRoot : gameObject;
            if (force || root.activeSelf != visible)
                root.SetActive(visible);
        }

        private void PositionNearCursor()
        {
            var panelTransform = panelRoot != null ? panelRoot.transform as RectTransform : transform as RectTransform;
            if (panelTransform == null)
                return;

            var parent = panelTransform.parent as RectTransform;
            if (parent == null)
                return;

            Canvas.ForceUpdateCanvases();

            var canvas = parent.GetComponentInParent<Canvas>();
            var eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            var cursorScreenPoint = (Vector2)Input.mousePosition;
            var panelSize = panelTransform.rect.size;
            var offset = cursorOffsetBelow;

            if (cursorScreenPoint.y - panelSize.y - Mathf.Abs(cursorOffsetBelow.y) < screenPadding.y)
                offset = cursorOffsetAbove;

            var screenPoint = cursorScreenPoint + offset;
            var minX = screenPadding.x + (panelSize.x * panelTransform.pivot.x);
            var maxX = Screen.width - screenPadding.x - (panelSize.x * (1f - panelTransform.pivot.x));
            var minY = screenPadding.y + (panelSize.y * panelTransform.pivot.y);
            var maxY = Screen.height - screenPadding.y - (panelSize.y * (1f - panelTransform.pivot.y));
            screenPoint.x = Mathf.Clamp(screenPoint.x, minX, maxX);
            screenPoint.y = Mathf.Clamp(screenPoint.y, minY, maxY);

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, eventCamera, out var localPoint))
                return;

            panelTransform.anchoredPosition = localPoint;
        }
    }
}
