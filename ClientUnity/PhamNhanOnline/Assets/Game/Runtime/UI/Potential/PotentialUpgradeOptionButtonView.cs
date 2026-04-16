using System;
using PhamNhanOnline.Client.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Potential
{
    public sealed class PotentialUpgradeOptionButtonView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button button;
        [SerializeField] private UiButtonView customButton;
        [SerializeField] private TMP_Text labelText;

        private string lastLabel = string.Empty;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (customButton == null)
                customButton = GetComponent<UiButtonView>();
        }

        public void SetContent(string label, Action onClick, bool interactable = true, bool force = false)
        {
            label = string.IsNullOrWhiteSpace(label) ? "-" : label.Trim();
            if (force || !string.Equals(lastLabel, label, StringComparison.Ordinal))
            {
                lastLabel = label;
                if (labelText != null)
                    labelText.text = label;
            }

            if (customButton != null)
            {
                customButton.Clicked -= HandleCustomButtonClicked;
                pendingClickAction = onClick;
                customButton.SetInteractable(interactable, force: true);
                if (interactable && onClick != null)
                    customButton.Clicked += HandleCustomButtonClicked;
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.interactable = false;
                }
            }
            else if (button != null)
            {
                button.interactable = interactable;
                button.onClick.RemoveAllListeners();
                if (interactable && onClick != null)
                    button.onClick.AddListener(() => onClick());
            }
        }

        private Action pendingClickAction;

        private void OnDisable()
        {
            if (customButton != null)
                customButton.Clicked -= HandleCustomButtonClicked;
        }

        private void HandleCustomButtonClicked()
        {
            pendingClickAction?.Invoke();
        }
    }
}
