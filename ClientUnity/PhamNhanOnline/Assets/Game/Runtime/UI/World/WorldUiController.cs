using UnityEngine;
using System.Threading.Tasks;
using PhamNhanOnline.Client.Core.Application;

namespace PhamNhanOnline.Client.UI.World
{
    public enum CraftingStationType
    {
        Alchemy = 0,
        Smithing = 1,
        Talisman = 2
    }

    public readonly struct CraftingPanelContext
    {
        public CraftingPanelContext(CraftingStationType stationType, string titleOverride = null)
        {
            StationType = stationType;
            TitleOverride = string.IsNullOrWhiteSpace(titleOverride) ? null : titleOverride.Trim();
        }

        public CraftingStationType StationType { get; }
        public string TitleOverride { get; }
    }

    [DisallowMultipleComponent]
    public sealed class WorldUiController : MonoBehaviour
    {
        public static WorldUiController Instance { get; private set; }
        public static bool IsAnyMenuOpen => Instance != null && Instance.IsMenuVisible;

        [Header("World Panels")]
        [SerializeField] private WorldMenuController worldMenuController;
        [SerializeField] private WorldCraftingPanelController worldCraftingPanelController;

        [Header("Behavior")]
        [SerializeField] private bool autoOpenCraftingPanelForActivePractice = true;
        [SerializeField] private float autoOpenRetryCooldownSeconds = 2f;

        private bool craftingPracticeRestoreHandled;
        private bool craftingPracticeRestoreInFlight;
        private float lastCraftingPracticeRestoreAttemptTime = float.NegativeInfinity;

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
            TryRestoreCraftingPanelOnLogin();
        }

        private void Update()
        {
            TryRestoreCraftingPanelOnLogin();
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
            return ShowCraftingPanel(CraftingStationType.Alchemy);
        }

        public bool ShowCraftingPanel(CraftingStationType stationType, string titleOverride = null)
        {
            if (worldCraftingPanelController == null)
            {
                Debug.LogError($"WorldUiController on '{gameObject.name}' is missing required reference '{nameof(worldCraftingPanelController)}'.");
                return false;
            }

            HideMenuIfVisible();

            worldCraftingPanelController.ConfigureContext(new CraftingPanelContext(stationType, titleOverride));
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

        private void TryRestoreCraftingPanelOnLogin()
        {
            if (!autoOpenCraftingPanelForActivePractice ||
                craftingPracticeRestoreHandled ||
                craftingPracticeRestoreInFlight ||
                !ClientRuntime.IsInitialized)
            {
                return;
            }

            if (Time.unscaledTime - lastCraftingPracticeRestoreAttemptTime < autoOpenRetryCooldownSeconds)
                return;

            _ = RestoreCraftingPanelOnLoginAsync();
        }

        private async Task RestoreCraftingPanelOnLoginAsync()
        {
            craftingPracticeRestoreInFlight = true;
            lastCraftingPracticeRestoreAttemptTime = Time.unscaledTime;

            try
            {
                var result = await ClientRuntime.AlchemyService.LoadPracticeStatusAsync();
                if (!result.Success)
                    return;

                craftingPracticeRestoreHandled = true;

                var session = ClientRuntime.Alchemy.CurrentPracticeSession;
                if (!session.HasValue || session.Value.PracticeType != 2)
                    return;

                if (session.Value.PracticeState != 1 &&
                    session.Value.PracticeState != 2 &&
                    session.Value.PracticeState != 3)
                {
                    return;
                }

                ShowCraftingPanel(CraftingStationType.Alchemy);
            }
            finally
            {
                craftingPracticeRestoreInFlight = false;
            }
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
