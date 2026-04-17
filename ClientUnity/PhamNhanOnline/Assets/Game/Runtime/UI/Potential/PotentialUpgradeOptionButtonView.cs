using System;
using PhamNhanOnline.Client.UI.Common;
using TMPro;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.Potential
{
    public sealed class PotentialUpgradeOptionButtonView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIButtonView buttonView;
        [SerializeField] private TMP_Text labelText;

        private string lastLabel = string.Empty;
        private Action pendingClickAction;

        private void Awake()
        {
            if (buttonView == null)
                buttonView = GetComponent<UIButtonView>();
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

            if (buttonView == null)
                return;

            buttonView.Clicked -= HandleButtonClicked;
            pendingClickAction = onClick;
            buttonView.SetInteractable(interactable, force: true);
            if (interactable && onClick != null)
                buttonView.Clicked += HandleButtonClicked;
        }

        private void OnDisable()
        {
            if (buttonView != null)
                buttonView.Clicked -= HandleButtonClicked;
        }

        private void HandleButtonClicked()
        {
            pendingClickAction?.Invoke();
        }
    }
}
