using System;
using System.Collections.Generic;
using PhamNhanOnline.Client.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PhamNhanOnline.Client.UI.Potential
{
    public sealed class PotentialUpgradeOptionsPopupView : CursorPopupViewModelBase, IPointerEnterHandler, IPointerExitHandler
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

        protected override bool HideOnFirstAwake => true;

        protected override void Awake()
        {
            if (panelRoot == null)
                panelRoot = gameObject;

            if (panelTransform == null)
                panelTransform = panelRoot.transform as RectTransform;

            if (optionsRoot == null && optionTemplate != null)
                optionsRoot = optionTemplate.transform.parent;

            if (hideTemplateObject && optionTemplate != null)
                optionTemplate.gameObject.SetActive(false);

            base.Awake();
        }

        protected override GameObject ResolveViewRoot()
        {
            return panelRoot != null ? panelRoot : gameObject;
        }

        protected override RectTransform ResolveViewRectTransform()
        {
            return panelTransform != null ? panelTransform : base.ResolveViewRectTransform();
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

                option.SetContent(options[i].Label, options[i].OnClick, options[i].Interactable, force: true);
            }

            PositionViewNearCursor(cursorOffsetBelow, cursorOffsetAbove, screenPadding);
            ShowView(force);
        }

        public void Hide(bool force = false)
        {
            IsPointerInside = false;
            SetViewVisible(false, force);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            IsPointerInside = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            IsPointerInside = false;
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
