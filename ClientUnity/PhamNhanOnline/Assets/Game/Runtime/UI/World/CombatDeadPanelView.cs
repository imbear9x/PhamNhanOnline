using System;
using PhamNhanOnline.Client.Core.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.World
{
    [DisallowMultipleComponent]
    public sealed class CombatDeadPanelView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button returnHomeButton;

        [Header("Text")]
        [SerializeField] private string title = "Trong thuong";
        [SerializeField] private string message = "Nhan vat da tu thuong. Tam thoi chi co the tro ve dong phu.";

        public event Action ReturnHomeRequested;
        private bool loggedMissingPanelRoot;
        private bool loggedMissingReturnHomeButton;

        public bool IsVisible
        {
            get { return panelRoot != null && panelRoot.activeSelf; }
        }

        private void Awake()
        {
            if (returnHomeButton != null)
            {
                returnHomeButton.onClick.RemoveListener(HandleReturnHomeClicked);
                returnHomeButton.onClick.AddListener(HandleReturnHomeClicked);
            }

            ApplyStaticText();
            SetStatus(string.Empty);
            SetVisible(false);
        }

        private void Start()
        {
            LogMissingCriticalDependenciesIfNeeded();
        }

        private void OnDestroy()
        {
            if (returnHomeButton != null)
                returnHomeButton.onClick.RemoveListener(HandleReturnHomeClicked);
        }

        public void Show()
        {
            ApplyStaticText();
            SetVisible(true);
        }

        public void Hide()
        {
            SetStatus(string.Empty);
            SetVisible(false);
        }

        public void SetBusy(bool busy)
        {
            if (returnHomeButton != null)
                returnHomeButton.interactable = !busy;
        }

        public void SetStatus(string text)
        {
            if (statusText == null)
                return;

            statusText.text = text ?? string.Empty;
            statusText.gameObject.SetActive(!string.IsNullOrWhiteSpace(statusText.text));
        }

        private void ApplyStaticText()
        {
            if (titleText != null)
                titleText.text = title ?? string.Empty;

            if (messageText != null)
                messageText.text = message ?? string.Empty;
        }

        private void HandleReturnHomeClicked()
        {
            var handler = ReturnHomeRequested;
            if (handler != null)
                handler();
        }

        private void SetVisible(bool visible)
        {
            if (panelRoot == null)
                return;

            if (panelRoot.activeSelf != visible)
                panelRoot.SetActive(visible);
        }

        private void LogMissingCriticalDependenciesIfNeeded()
        {
            if (panelRoot == null && !loggedMissingPanelRoot)
            {
                ClientLog.Error("CombatDeadPanelView is missing Panel Root.");
                loggedMissingPanelRoot = true;
            }

            if (returnHomeButton == null && !loggedMissingReturnHomeButton)
            {
                ClientLog.Error("CombatDeadPanelView is missing Return Home Button.");
                loggedMissingReturnHomeButton = true;
            }
        }
    }
}


