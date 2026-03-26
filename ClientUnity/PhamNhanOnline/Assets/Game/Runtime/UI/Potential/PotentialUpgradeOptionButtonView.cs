using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Potential
{
    public sealed class PotentialUpgradeOptionButtonView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text labelText;

        private string lastLabel = string.Empty;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
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

            if (button != null)
            {
                button.interactable = interactable;
                button.onClick.RemoveAllListeners();
                if (interactable && onClick != null)
                    button.onClick.AddListener(() => onClick());
            }
        }
    }
}
