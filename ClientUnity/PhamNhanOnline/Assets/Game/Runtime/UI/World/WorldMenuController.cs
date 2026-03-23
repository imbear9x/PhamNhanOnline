using System;
using System.Collections.Generic;
using System.Globalization;
using PhamNhanOnline.Client.Core.Application;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.World
{
    [DisallowMultipleComponent]
    public sealed class WorldMenuController : MonoBehaviour
    {
        public const string ScreenId = "world-menu";
        public const string QuestTabId = "quest";
        public const string InventoryTabId = "inventory";
        public const string StatsTabId = "stats";
        public const string EquipmentTabId = "equipment";
        public const string GuildTabId = "guild";

        [Serializable]
        public sealed class WorldMenuTabBinding
        {
            [SerializeField] private string tabId;
            [SerializeField] private string title;
            [SerializeField] private Button button;
            [SerializeField] private GameObject contentRoot;
            [SerializeField] private TMP_Text contentText;

            public string TabId => tabId;
            public string Title => title;
            public Button Button => button;
            public GameObject ContentRoot => contentRoot;
            public TMP_Text ContentText => contentText;
        }

        private static WorldMenuController activeInstance;

        [Header("Screen Roots")]
        [SerializeField] private GameObject panelRoot;

        [Header("Buttons")]
        [SerializeField] private Button menuButton;
        [SerializeField] private TMP_Text menuButtonText;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button dimmerButton;

        [Header("Header")]
        [SerializeField] private TMP_Text titleText;

        [Header("Tabs")]
        [SerializeField] private string defaultTabId = QuestTabId;
        [SerializeField] private List<WorldMenuTabBinding> tabs = new List<WorldMenuTabBinding>(5);

        [Header("Input")]
        [SerializeField] private KeyCode closeKey = KeyCode.Escape;

        private bool isRegistered;
        private string activeTabId = string.Empty;

        public static bool IsAnyMenuOpen
        {
            get
            {
                return activeInstance != null && activeInstance.IsMenuVisible;
            }
        }

        public bool IsMenuVisible
        {
            get
            {
                var root = panelRoot != null ? panelRoot : gameObject;
                return root.activeSelf;
            }
        }

        private void Awake()
        {
            activeInstance = this;
            if (panelRoot == null)
                panelRoot = gameObject;

            WireUi();
            RefreshAllTabContent();

            activeTabId = ResolveInitialTabId();
            ApplyTabSelection(activeTabId);
            SetMenuVisible(false);
        }

        private void Start()
        {
            TryRegisterScreen();
        }

        private void Update()
        {
            TryRegisterScreen();

            if (IsMenuVisible)
            {
                RefreshAllTabContent();

                if (Input.GetKeyDown(closeKey))
                    HideMenu();
            }
        }

        private void OnDestroy()
        {
            if (menuButton != null)
                menuButton.onClick.RemoveListener(ToggleMenu);

            if (closeButton != null)
                closeButton.onClick.RemoveListener(HideMenu);

            if (dimmerButton != null)
                dimmerButton.onClick.RemoveListener(HideMenu);

            for (var i = 0; i < tabs.Count; i++)
            {
                var button = tabs[i].Button;
                if (button == null)
                    continue;

                button.onClick.RemoveAllListeners();
            }

            if (isRegistered && ClientRuntime.IsInitialized)
                ClientRuntime.UiScreens.Unregister(ScreenId, panelRoot);

            if (activeInstance == this)
                activeInstance = null;
        }

        public void ToggleMenu()
        {
            SetMenuVisible(!IsMenuVisible);
        }

        public void ShowMenu()
        {
            SetMenuVisible(true);
        }

        public void HideMenu()
        {
            SetMenuVisible(false);
        }

        public void ShowTab(string tabId)
        {
            if (string.IsNullOrWhiteSpace(tabId))
                return;

            activeTabId = tabId;
            if (!IsMenuVisible)
                SetMenuVisible(true);

            ApplyTabSelection(activeTabId);
            RefreshAllTabContent();
        }

        private void ApplyTabSelection(string tabId)
        {
            for (var i = 0; i < tabs.Count; i++)
            {
                var isActive = string.Equals(tabs[i].TabId, tabId, StringComparison.Ordinal);
                if (tabs[i].ContentRoot != null)
                    tabs[i].ContentRoot.SetActive(isActive);

                ApplyTabButtonVisual(tabs[i], isActive);
                if (isActive && titleText != null)
                    titleText.text = tabs[i].Title;
            }
        }

        private void WireUi()
        {
            if (menuButton != null)
            {
                menuButton.onClick.RemoveListener(ToggleMenu);
                menuButton.onClick.AddListener(ToggleMenu);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(HideMenu);
                closeButton.onClick.AddListener(HideMenu);
            }

            if (dimmerButton != null)
            {
                dimmerButton.onClick.RemoveListener(HideMenu);
                dimmerButton.onClick.AddListener(HideMenu);
            }

            for (var i = 0; i < tabs.Count; i++)
            {
                var tab = tabs[i];
                if (tab.Button == null)
                    continue;

                var capturedTabId = tab.TabId;
                tab.Button.onClick.RemoveAllListeners();
                tab.Button.onClick.AddListener(delegate { ShowTab(capturedTabId); });
            }
        }

        private void TryRegisterScreen()
        {
            if (isRegistered || !ClientRuntime.IsInitialized)
                return;

            ClientRuntime.UiScreens.Register(ScreenId, panelRoot != null ? panelRoot : gameObject);
            isRegistered = true;
        }

        private void SetMenuVisible(bool visible)
        {
            var root = panelRoot != null ? panelRoot : gameObject;
            if (root.activeSelf != visible)
                root.SetActive(visible);

            if (visible)
                RefreshAllTabContent();

            UpdateMenuButtonLabel();
        }

        private void UpdateMenuButtonLabel()
        {
            if (menuButtonText == null)
                return;

            menuButtonText.text = IsMenuVisible ? "Dong" : "Menu";
        }

        private void RefreshAllTabContent()
        {
            for (var i = 0; i < tabs.Count; i++)
            {
                var tab = tabs[i];
                if (tab.ContentText == null)
                    continue;

                tab.ContentText.text = BuildTabContent(tab.TabId);
            }
        }

        private string BuildTabContent(string tabId)
        {
            if (string.Equals(tabId, QuestTabId, StringComparison.Ordinal))
            {
                return "Nhiem vu se duoc noi vao packet/flow rieng.\n\nPhase nay menu da co san cho tab Quest de sau nay gan danh sach nhiem vu, tien do va phan thuong.";
            }

            if (string.Equals(tabId, InventoryTabId, StringComparison.Ordinal))
            {
                return "Kho do se duoc noi voi GetInventoryPacket va inventory grid.\n\nMenu nay da tao san dung cho UI mo rong tiep theo: item slots, item tooltip, quick use va filter.";
            }

            if (string.Equals(tabId, EquipmentTabId, StringComparison.Ordinal))
            {
                return "Trang bi se duoc tach thanh panel rieng de doc tu inventory/equipment runtime.\n\nCho nay phu hop de gan avatar, o trang bi, chi so cong them va nut mac/thao.";
            }

            if (string.Equals(tabId, GuildTabId, StringComparison.Ordinal))
            {
                return "Bang hoi hien chua noi server flow.\n\nTab nay da co san trong world menu de sau nay gan danh sach thanh vien, loi moi, quyen han va activity.";
            }

            return BuildStatsContent();
        }

        private static string BuildStatsContent()
        {
            if (!ClientRuntime.IsInitialized)
                return "Client runtime chua khoi tao.";

            var selectedCharacter = ClientRuntime.Character.SelectedCharacter;
            var baseStats = ClientRuntime.Character.BaseStats;
            var currentState = ClientRuntime.Character.CurrentState;

            var characterName = selectedCharacter.HasValue && !string.IsNullOrWhiteSpace(selectedCharacter.Value.Name)
                ? selectedCharacter.Value.Name
                : "Chua co nhan vat";

            var mapName = string.IsNullOrWhiteSpace(ClientRuntime.World.CurrentMapName)
                ? "Chua vao map"
                : ClientRuntime.World.CurrentMapName;

            if (!baseStats.HasValue)
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "Nhan vat: {0}\nMap: {1}\n\nChua co du lieu chi so tu server.",
                    characterName,
                    mapName);
            }

            var stats = baseStats.Value;
            var currentHp = currentState.HasValue ? currentState.Value.CurrentHp : stats.BaseHp;
            var currentMp = currentState.HasValue ? currentState.Value.CurrentMp : stats.BaseMp;
            var currentStamina = currentState.HasValue ? currentState.Value.CurrentStamina : stats.BaseStamina;

            return string.Format(
                CultureInfo.InvariantCulture,
                "Nhan vat: {0}\nMap hien tai: {1}\nKhu: {2}\n\nHP: {3}/{4}\nMP: {5}/{6}\nThe luc: {7}/{8}\nTan cong: {9}\nToc do: {10}\nThan thuc: {11}\nCo duyen: {12:0.##}\nTiem nang con lai: {13}",
                characterName,
                mapName,
                ClientRuntime.World.CurrentZoneIndex.HasValue ? ClientRuntime.World.CurrentZoneIndex.Value.ToString(CultureInfo.InvariantCulture) : "-",
                currentHp,
                stats.BaseHp,
                currentMp,
                stats.BaseMp,
                currentStamina,
                stats.BaseStamina,
                stats.BaseAttack,
                stats.BaseSpeed,
                stats.BaseSpiritualSense,
                stats.BaseFortune,
                stats.UnallocatedPotential);
        }

        private static void ApplyTabButtonVisual(WorldMenuTabBinding tab, bool isActive)
        {
            if (tab == null || tab.Button == null)
                return;

            var image = tab.Button.GetComponent<Image>();
            if (image != null)
                image.color = isActive ? new Color(0.30f, 0.42f, 0.60f, 0.96f) : new Color(0.15f, 0.18f, 0.24f, 0.96f);

            var label = tab.Button.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.color = isActive ? new Color(1f, 0.97f, 0.86f, 1f) : Color.white;
        }

        private string ResolveInitialTabId()
        {
            if (!string.IsNullOrWhiteSpace(defaultTabId))
                return defaultTabId;

            for (var i = 0; i < tabs.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(tabs[i].TabId))
                    return tabs[i].TabId;
            }

            return QuestTabId;
        }
    }
}
