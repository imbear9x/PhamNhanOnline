using System;
using System.Collections.Generic;
using System.Globalization;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Features.Character.Application;
using PhamNhanOnline.Client.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.World
{
    [DisallowMultipleComponent]
    public sealed class WorldMenuController : ViewModelBase
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
            [SerializeField] private UIButtonView button;
            [SerializeField] private GameObject contentRoot;
            [SerializeField] private TMP_Text contentText;
            [NonSerialized] private Action clickHandler;

            public string TabId => tabId;
            public string Title => title;
            public UIButtonView Button => button;
            public GameObject ContentRoot => contentRoot;
            public TMP_Text ContentText => contentText;
            public Action ClickHandler
            {
                get { return clickHandler; }
                set { clickHandler = value; }
            }
        }

        private static WorldMenuController activeInstance;

        [Header("Screen Roots")]
        [SerializeField] private GameObject panelRoot;

        [SerializeField] private UIButtonView closeButton;
        [SerializeField] private Button dimmerButton;

        [Header("Header")]
        [SerializeField] private TMP_Text titleText;

        [Header("Tabs")]
        [SerializeField] private string defaultTabId = QuestTabId;
        [SerializeField] private List<WorldMenuTabBinding> tabs = new List<WorldMenuTabBinding>(5);

        [Header("Input")]
        [SerializeField] private KeyCode closeKey = KeyCode.Escape;

        private bool isRegistered;
        private bool isInitialized;
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
            get { return IsVisible; }
        }

        protected override bool HideOnFirstAwake => true;

        protected override GameObject ResolveViewRoot()
        {
            return panelRoot != null ? panelRoot : gameObject;
        }

        protected override void Awake()
        {
            EnsureInitialized(hideAfterInitialize: true);
            base.Awake();
        }

        private void Start()
        {
            TryRegisterScreen();
            TryBindRuntimeEvents();
        }

        private void Update()
        {
            TryRegisterScreen();
            TryBindRuntimeEvents();

            if (IsMenuVisible)
            {
                RefreshAllTabContent();

                if (Input.GetKeyDown(closeKey))
                    HideMenu();
            }
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.Clicked -= HideMenu;

            if (dimmerButton != null)
                dimmerButton.onClick.RemoveListener(HideMenu);

            for (var i = 0; i < tabs.Count; i++)
            {
                var button = tabs[i].Button;
                if (button == null)
                    continue;

                if (tabs[i].ClickHandler != null)
                    button.Clicked -= tabs[i].ClickHandler;

                tabs[i].ClickHandler = null;
            }

            if (isRegistered && ClientRuntime.IsInitialized)
                ClientRuntime.UIScreens.Unregister(ScreenId, panelRoot);

            UnbindRuntimeEvents();
            if (activeInstance == this)
                activeInstance = null;
        }

        public void ToggleMenu()
        {
            EnsureInitialized(hideAfterInitialize: false);
            SetMenuVisible(!IsMenuVisible);
        }

        public void ShowMenu()
        {
            EnsureInitialized(hideAfterInitialize: false);
            SetMenuVisible(true);
        }

        public void HideMenu()
        {
            EnsureInitialized(hideAfterInitialize: false);
            SetMenuVisible(false);
        }

        public void ShowTab(string tabId)
        {
            EnsureInitialized(hideAfterInitialize: false);
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

        private void WireUI()
        {
            if (closeButton != null)
            {
                closeButton.Clicked -= HideMenu;
                closeButton.Clicked += HideMenu;
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

                if (tab.ClickHandler != null)
                    tab.Button.Clicked -= tab.ClickHandler;

                var capturedTabId = tab.TabId;
                tab.ClickHandler = delegate { ShowTab(capturedTabId); };
                tab.Button.Clicked += tab.ClickHandler;
            }
        }

        private void EnsureInitialized(bool hideAfterInitialize)
        {
            if (isInitialized)
            {
                activeInstance = this;
                return;
            }

            activeInstance = this;
            if (panelRoot == null)
                panelRoot = gameObject;

            WireUI();
            RefreshAllTabContent();

            activeTabId = ResolveInitialTabId();
            ApplyTabSelection(activeTabId);
            isInitialized = true;

            if (hideAfterInitialize)
                SetMenuVisible(false);
        }

        private void TryRegisterScreen()
        {
            if (isRegistered || !ClientRuntime.IsInitialized)
                return;

            ClientRuntime.UIScreens.Register(ScreenId, panelRoot != null ? panelRoot : gameObject);
            isRegistered = true;
        }

        private void TryBindRuntimeEvents()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            ClientRuntime.Connection.StateChanged -= HandleConnectionStateChanged;
            ClientRuntime.Connection.StateChanged += HandleConnectionStateChanged;
            ClientRuntime.Character.CurrentStateChanged -= HandleCharacterCurrentStateChanged;
            ClientRuntime.Character.CurrentStateChanged += HandleCharacterCurrentStateChanged;
        }

        private void UnbindRuntimeEvents()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            ClientRuntime.Connection.StateChanged -= HandleConnectionStateChanged;
            ClientRuntime.Character.CurrentStateChanged -= HandleCharacterCurrentStateChanged;
        }

        private void HandleConnectionStateChanged(PhamNhanOnline.Client.Network.Session.ClientConnectionState state)
        {
            if (state == PhamNhanOnline.Client.Network.Session.ClientConnectionState.Disconnected)
                HideMenu();
        }

        private void HandleCharacterCurrentStateChanged(CharacterCurrentStateChangeNotice notice)
        {
            var currentState = notice.CurrentState;
            if (!currentState.HasValue)
                return;

            if (ClientCharacterRuntimeStateCodes.IsDefeated(currentState.Value))
            {
                HideMenu();
            }
        }

        private void SetMenuVisible(bool visible)
        {
            if (visible)
                ShowView();
            else
                SetViewVisible(false);

            if (visible)
                RefreshAllTabContent();
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
            var currentHp = currentState.HasValue ? currentState.Value.CurrentHp : GetTotalHp(stats);
            var currentMp = currentState.HasValue ? currentState.Value.CurrentMp : GetTotalMp(stats);
            var currentStamina = currentState.HasValue ? currentState.Value.CurrentStamina : stats.BaseStamina;

            return string.Format(
                CultureInfo.InvariantCulture,
                "Nhan vat: {0}\nMap hien tai: {1}\nKhu: {2}\n\nHP: {3}/{4}\nMP: {5}/{6}\nThe luc: {7}/{8}\nTan cong: {9}\nToc do: {10}\nSense: {11}\nLuck: {12:0.##}\nTiem nang con lai: {13}",
                characterName,
                mapName,
                ClientRuntime.World.CurrentZoneIndex.HasValue ? ClientRuntime.World.CurrentZoneIndex.Value.ToString(CultureInfo.InvariantCulture) : "-",
                currentHp,
                GetTotalHp(stats),
                currentMp,
                GetTotalMp(stats),
                currentStamina,
                stats.BaseStamina,
                GetTotalAttack(stats),
                GetTotalSpeed(stats),
                GetTotalSense(stats),
                GetTotalLuck(stats),
                stats.UnallocatedPotential);
        }

        private static int GetTotalHp(GameShared.Models.CharacterBaseStatsModel stats)
        {
            return stats.FinalHp;
        }

        private static int GetTotalMp(GameShared.Models.CharacterBaseStatsModel stats)
        {
            return stats.FinalMp;
        }

        private static int GetTotalAttack(GameShared.Models.CharacterBaseStatsModel stats)
        {
            return stats.FinalAttack;
        }

        private static int GetTotalSpeed(GameShared.Models.CharacterBaseStatsModel stats)
        {
            return stats.FinalSpeed;
        }

        private static int GetTotalSense(GameShared.Models.CharacterBaseStatsModel stats)
        {
            return stats.FinalSense;
        }

        private static double GetTotalLuck(GameShared.Models.CharacterBaseStatsModel stats)
        {
            return stats.FinalLuck;
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
