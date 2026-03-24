using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PhamNhanOnline.Client.UI.Potential
{
    public sealed class PotentialUpgradeOptionsPopupView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public readonly struct OptionEntry
        {
            public OptionEntry(string label, Action onClick)
            {
                Label = label;
                OnClick = onClick;
            }

            public string Label { get; }
            public Action OnClick { get; }
        }

        [Header("References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RectTransform panelTransform;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Transform optionsRoot;
        [SerializeField] private PotentialUpgradeOptionButtonView optionTemplate;

        [Header("Layout")]
        [SerializeField] private Vector2 cursorOffsetBelow = new Vector2(20f, -20f);
        [SerializeField] private Vector2 cursorOffsetAbove = new Vector2(20f, 20f);
        [SerializeField] private Vector2 screenPadding = new Vector2(16f, 16f);
        [SerializeField] private bool hideTemplateObject = true;

        private readonly List<PotentialUpgradeOptionButtonView> spawnedOptions = new List<PotentialUpgradeOptionButtonView>(4);

        public bool IsPointerInside { get; private set; }
        public bool IsVisible => panelRoot != null ? panelRoot.activeSelf : gameObject.activeSelf;

        private void Awake()
        {
            if (panelRoot == null)
                panelRoot = gameObject;

            if (panelTransform == null)
                panelTransform = panelRoot.transform as RectTransform;

            if (optionsRoot == null && optionTemplate != null)
                optionsRoot = optionTemplate.transform.parent;

            if (hideTemplateObject && optionTemplate != null)
                optionTemplate.gameObject.SetActive(false);

            Hide(force: true);
        }

        public void Show(RectTransform anchor, string title, IReadOnlyList<OptionEntry> options, bool force = false)
        {
            if (anchor == null || options == null || options.Count == 0)
            {
                Hide(force);
                return;
            }

            EnsureOptionCount(options.Count);
            if (titleText != null)
                titleText.text = string.IsNullOrWhiteSpace(title) ? string.Empty : title.Trim();

            for (var i = 0; i < spawnedOptions.Count; i++)
            {
                var option = spawnedOptions[i];
                if (option == null)
                    continue;

                var shouldBeVisible = i < options.Count;
                if (option.gameObject.activeSelf != shouldBeVisible)
                    option.gameObject.SetActive(shouldBeVisible);

                if (!shouldBeVisible)
                    continue;

                option.SetContent(options[i].Label, options[i].OnClick, force: true);
            }

            PositionNearCursor();
            SetVisible(true, force);
        }

        public void Hide(bool force = false)
        {
            IsPointerInside = false;
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

        private void SetVisible(bool visible, bool force)
        {
            var root = panelRoot != null ? panelRoot : gameObject;
            if (force || root.activeSelf != visible)
                root.SetActive(visible);
        }

        private void PositionNearCursor()
        {
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

            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, eventCamera, out localPoint))
                return;

            panelTransform.anchoredPosition = localPoint;
        }

        private void EnsureOptionCount(int targetCount)
        {
            if (targetCount <= spawnedOptions.Count)
                return;

            if (optionTemplate == null)
            {
                Debug.LogWarning("PotentialUpgradeOptionsPopupView is missing optionTemplate.");
                return;
            }

            var parent = optionsRoot != null ? optionsRoot : optionTemplate.transform.parent;
            for (var i = spawnedOptions.Count; i < targetCount; i++)
            {
                var instance = Instantiate(optionTemplate, parent);
                instance.name = string.Format("{0}_{1}", optionTemplate.name, i);
                instance.gameObject.SetActive(true);
                spawnedOptions.Add(instance);
            }
        }
    }
}
