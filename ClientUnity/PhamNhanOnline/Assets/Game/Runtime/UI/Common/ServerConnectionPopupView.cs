using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Common
{
    public sealed class ServerConnectionPopupView : MonoBehaviour
    {
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button confirmButton;

        private Action confirmAction;

        public bool IsVisible
        {
            get
            {
                var root = popupRoot != null ? popupRoot : gameObject;
                return root.activeSelf;
            }
        }

        private void Awake()
        {
            if (popupRoot == null)
                popupRoot = gameObject;

            if (confirmButton != null)
                confirmButton.onClick.AddListener(HandleConfirmClicked);

            if (statusText != null)
            {
                statusText.text = string.Empty;
                statusText.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (confirmButton != null)
                confirmButton.onClick.RemoveListener(HandleConfirmClicked);
        }

        public void Show(string message, string statusMessage = null, bool allowClose = true, Action onConfirm = null)
        {
            confirmAction = onConfirm;

            if (messageText != null)
                messageText.text = message ?? string.Empty;

            if (statusText != null)
            {
                statusText.text = statusMessage ?? string.Empty;
                statusText.gameObject.SetActive(!string.IsNullOrWhiteSpace(statusMessage));
            }

            if (confirmButton != null)
                confirmButton.gameObject.SetActive(allowClose);

            SetVisible(true, force: true);
        }

        public void Hide(bool force = false)
        {
            confirmAction = null;

            if (statusText != null)
            {
                statusText.text = string.Empty;
                statusText.gameObject.SetActive(false);
            }

            SetVisible(false, force);
        }

        private void HandleConfirmClicked()
        {
            var callback = confirmAction;
            confirmAction = null;
            Hide(force: true);
            if (callback != null)
                callback();
        }

        private void SetVisible(bool visible, bool force)
        {
            var root = popupRoot != null ? popupRoot : gameObject;
            if (force || root.activeSelf != visible)
                root.SetActive(visible);
        }
    }
}
