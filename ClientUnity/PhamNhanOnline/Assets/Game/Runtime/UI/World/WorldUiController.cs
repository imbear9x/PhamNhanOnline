using UnityEngine;

namespace PhamNhanOnline.Client.UI.World
{
    [DisallowMultipleComponent]
    public sealed class WorldUiController : MonoBehaviour
    {
        public static WorldUiController Instance { get; private set; }
        public static bool IsAnyMenuOpen => Instance != null && Instance.IsMenuVisible;

        [Header("World Panels")]
        [SerializeField] private WorldMenuController worldMenuController;
        [SerializeField] private WorldCraftingPanelController worldCraftingPanelController;

        public bool IsMenuVisible => worldMenuController != null && worldMenuController.IsMenuVisible;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning(
                    $"Duplicate WorldUiController detected on '{gameObject.name}'. " +
                    $"Keeping '{Instance.gameObject.name}' and disabling this component.");
                enabled = false;
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            ValidateSerializedReferences();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public bool ToggleMenu()
        {
            if (worldMenuController == null)
                return false;

            worldMenuController.ToggleMenu();
            return true;
        }

        public bool ShowMenu()
        {
            if (worldMenuController == null)
                return false;

            if (worldMenuController.IsMenuVisible)
                return false;

            worldMenuController.ShowMenu();
            return true;
        }

        public bool ShowCraftingPanel()
        {
            if (worldCraftingPanelController == null)
            {
                Debug.LogError($"WorldUiController on '{gameObject.name}' is missing required reference '{nameof(worldCraftingPanelController)}'.");
                return false;
            }

            HideMenuIfVisible();

            worldCraftingPanelController.ShowPanel();
            return true;
        }

        public bool HideMenuIfVisible()
        {
            if (worldMenuController == null)
                return false;

            if (!worldMenuController.IsMenuVisible)
                return false;

            worldMenuController.HideMenu();
            return true;
        }

        public bool HideCraftingPanelIfVisible()
        {
            if (worldCraftingPanelController == null)
            {
                Debug.LogError($"WorldUiController on '{gameObject.name}' is missing required reference '{nameof(worldCraftingPanelController)}'.");
                return false;
            }

            if (!worldCraftingPanelController.IsPanelVisible)
                return false;

            worldCraftingPanelController.HidePanel();
            return true;
        }

        private void ValidateSerializedReferences()
        {
            if (worldCraftingPanelController == null)
            {
                throw new System.InvalidOperationException(
                    $"WorldUiController on '{gameObject.name}' is missing required reference '{nameof(worldCraftingPanelController)}'.");
            }
        }
    }
}
