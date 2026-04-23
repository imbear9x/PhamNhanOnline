using TMPro;
using UnityEngine;
using PhamNhanOnline.Client.UI.Common;
using PhamNhanOnline.Client.UI.Hud;

namespace PhamNhanOnline.Client.UI.World
{
    [DisallowMultipleComponent]
    public sealed class PersistentWorldUIController : MonoBehaviour
    {
        [Header("Quick Actions")]
        [SerializeField] private UIButtonView quickMenuOpenButton;
        [SerializeField] private UIButtonView nextTargetButton;
        [SerializeField] private TMP_Text quickMenuOpenButtonText;

        [Header("References")]
        [SerializeField] private TargetHudController targetHudController;

        [Header("Labels")]
        [SerializeField] private string menuClosedLabel = "Menu";
        [SerializeField] private string menuOpenLabel = "Dong";

        private string lastAppliedButtonLabel = string.Empty;

        private void Awake()
        {
            WireUI();
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
                quickMenuOpenButton.Clicked -= HandleQuickMenuOpenClicked;

            if (nextTargetButton != null)
                nextTargetButton.Clicked -= HandleNextTargetClicked;
        }

        private void WireUI()
        {
            if (quickMenuOpenButton == null)
                return;

            quickMenuOpenButton.Clicked -= HandleQuickMenuOpenClicked;
            quickMenuOpenButton.Clicked += HandleQuickMenuOpenClicked;

            if (nextTargetButton != null)
            {
                nextTargetButton.Clicked -= HandleNextTargetClicked;
                nextTargetButton.Clicked += HandleNextTargetClicked;
            }
        }

        private void Refresh(bool force)
        {
            var label = WorldUIController.IsAnyMenuOpen ? menuOpenLabel : menuClosedLabel;
            if (!force && string.Equals(lastAppliedButtonLabel, label, System.StringComparison.Ordinal))
                return;

            lastAppliedButtonLabel = label;
            if (quickMenuOpenButtonText != null)
                quickMenuOpenButtonText.text = label;
        }

        private void HandleQuickMenuOpenClicked()
        {
            if (WorldUIController.Instance == null)
            {
                Debug.LogWarning($"PersistentWorldUIController on '{gameObject.name}' could not find WorldUIController.Instance.");
                return;
            }

            WorldUIController.Instance.ToggleMenu();
            Refresh(force: true);
        }

        private void HandleNextTargetClicked()
        {
            if (targetHudController == null)
            {
                Debug.LogWarning(
                    $"PersistentWorldUIController on '{gameObject.name}' is missing required reference '{nameof(targetHudController)}' for next target.");
                return;
            }

            targetHudController.CycleNearbyTarget();
        }

        private void ValidateSerializedReferences()
        {
            if (quickMenuOpenButton == null)
            {
                throw new System.InvalidOperationException(
                    $"PersistentWorldUIController on '{gameObject.name}' is missing required reference '{nameof(quickMenuOpenButton)}'.");
            }

            if (nextTargetButton == null)
            {
                throw new System.InvalidOperationException(
                    $"PersistentWorldUIController on '{gameObject.name}' is missing required reference '{nameof(nextTargetButton)}'.");
            }

            if (targetHudController == null)
                Debug.LogError(
                    $"PersistentWorldUIController on '{gameObject.name}' is missing required reference '{nameof(targetHudController)}'.");
        }
    }
}
