using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.World
{
    [DisallowMultipleComponent]
    public sealed class PersistentWorldUiController : MonoBehaviour
    {
        [Header("Quick Actions")]
        [SerializeField] private Button quickMenuOpenButton;
        [SerializeField] private TMP_Text quickMenuOpenButtonText;

        [Header("Labels")]
        [SerializeField] private string menuClosedLabel = "Menu";
        [SerializeField] private string menuOpenLabel = "Dong";

        private string lastAppliedButtonLabel = string.Empty;

        private void Awake()
        {
            WireUi();
            Refresh(force: true);
        }

        private void Start()
        {
            ValidateSerializedReferences();
            Refresh(force: true);
        }

        private void Update()
        {
            Refresh(force: false);
        }

        private void OnDestroy()
        {
            if (quickMenuOpenButton != null)
                quickMenuOpenButton.onClick.RemoveListener(HandleQuickMenuOpenClicked);
        }

        private void WireUi()
        {
            if (quickMenuOpenButton == null)
                return;

            quickMenuOpenButton.onClick.RemoveListener(HandleQuickMenuOpenClicked);
            quickMenuOpenButton.onClick.AddListener(HandleQuickMenuOpenClicked);
        }

        private void Refresh(bool force)
        {
            var label = WorldUiController.IsAnyMenuOpen ? menuOpenLabel : menuClosedLabel;
            if (!force && string.Equals(lastAppliedButtonLabel, label, System.StringComparison.Ordinal))
                return;

            lastAppliedButtonLabel = label;
            if (quickMenuOpenButtonText != null)
                quickMenuOpenButtonText.text = label;
        }

        private void HandleQuickMenuOpenClicked()
        {
            if (WorldUiController.Instance == null)
            {
                Debug.LogWarning($"PersistentWorldUiController on '{gameObject.name}' could not find WorldUiController.Instance.");
                return;
            }

            WorldUiController.Instance.ToggleMenu();
            Refresh(force: true);
        }

        private void ValidateSerializedReferences()
        {
            if (quickMenuOpenButton == null)
            {
                throw new System.InvalidOperationException(
                    $"PersistentWorldUiController on '{gameObject.name}' is missing required reference '{nameof(quickMenuOpenButton)}'.");
            }
        }
    }
}
