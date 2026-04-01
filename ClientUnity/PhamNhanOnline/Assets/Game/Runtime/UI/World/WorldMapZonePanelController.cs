using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GameShared.Messages;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using TMPro;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.World
{
    [DisallowMultipleComponent]
    public sealed class WorldMapZonePanelController : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text currentMapText;
        [SerializeField] private TMP_Text currentZoneText;
        [SerializeField] private TMP_Text statusText;

        [Header("Zone List")]
        [SerializeField] private MapZoneListView zoneListView;

        [Header("Display Text")]
        [SerializeField] private string panelTitle = "Danh sách khu";
        [SerializeField] private string currentMapPrefix = "Map: ";
        [SerializeField] private string currentZonePrefix = "Khu hiện tại: ";
        [SerializeField] private string unknownMapText = "Chưa vào map";
        [SerializeField] private string unsupportedMapText = "Map hiện tại không hỗ trợ chọn khu.";
        [SerializeField] private string emptyZoneListText = "Map hiện tại chưa có khu khả dụng.";

        [Header("Occupancy Colors")]
        [SerializeField] private Color lowOccupancyColor = new Color(0.20f, 0.56f, 0.27f, 0.95f);
        [SerializeField] private Color mediumOccupancyColor = new Color(0.78f, 0.67f, 0.16f, 0.95f);
        [SerializeField] private Color highOccupancyColor = new Color(0.87f, 0.45f, 0.12f, 0.95f);
        [SerializeField] private Color fullOccupancyColor = new Color(0.76f, 0.18f, 0.18f, 0.96f);

        private readonly List<MapZoneSummaryModel> loadedZones = new List<MapZoneSummaryModel>(8);
        private bool loadInFlight;
        private bool switchInFlight;
        private int? loadedMapId;
        private int? loadedCurrentZoneIndex;
        private int? observedMapId;
        private string lastStatusMessage = string.Empty;
        private string lastSnapshot = string.Empty;

        private void Awake()
        {
            if (zoneListView != null)
                zoneListView.ItemClicked += HandleZoneItemClicked;
        }

        private void Update()
        {
            if (!isActiveAndEnabled)
                return;

            RefreshPanel(force: false);

            if (!ClientRuntime.IsInitialized)
                return;

            var currentMapId = ClientRuntime.World.CurrentMapId;
            if (observedMapId != currentMapId)
            {
                observedMapId = currentMapId;
                loadedMapId = null;
                loadedCurrentZoneIndex = null;
                loadedZones.Clear();
                lastStatusMessage = string.Empty;
                RefreshPanel(force: true);
                _ = LoadCurrentMapZonesAsync(force: true);
            }
        }

        private void OnDestroy()
        {
            if (zoneListView != null)
                zoneListView.ItemClicked -= HandleZoneItemClicked;
        }

        private void OnEnable()
        {
            observedMapId = ClientRuntime.IsInitialized ? ClientRuntime.World.CurrentMapId : null;
            lastStatusMessage = string.Empty;
            RefreshPanel(force: true);
            _ = LoadCurrentMapZonesAsync(force: true);
        }

        private void RefreshPanel(bool force)
        {
            var snapshot = BuildSnapshot();
            if (!force && string.Equals(lastSnapshot, snapshot, StringComparison.Ordinal))
                return;

            lastSnapshot = snapshot;

            ApplyText(titleText, panelTitle, force: true);
            ApplyText(currentMapText, BuildCurrentMapText(), force: true);
            ApplyText(currentZoneText, BuildCurrentZoneText(), force: true);
            ApplyText(statusText, ResolveStatusText(), force: true);

            if (zoneListView == null)
                return;

            if (!ClientRuntime.IsInitialized || !ClientRuntime.World.CurrentMapId.HasValue)
            {
                zoneListView.Clear(force: true);
                return;
            }

            var currentMapId = ClientRuntime.World.CurrentMapId.Value;
            if (loadedMapId != currentMapId)
            {
                zoneListView.Clear(force: true);
                return;
            }

            zoneListView.SetEntries(BuildZoneEntries(currentMapId), force: true);
        }

        private async System.Threading.Tasks.Task LoadCurrentMapZonesAsync(bool force)
        {
            if (loadInFlight || !ClientRuntime.IsInitialized)
                return;

            var currentMapId = ClientRuntime.World.CurrentMapId;
            if (!currentMapId.HasValue)
            {
                loadedMapId = null;
                loadedCurrentZoneIndex = null;
                loadedZones.Clear();
                RefreshPanel(force: true);
                return;
            }

            if (!force && loadedMapId == currentMapId.Value && loadedZones.Count > 0)
                return;

            loadInFlight = true;
            lastStatusMessage = string.Empty;
            RefreshPanel(force: true);

            var requestedMapId = currentMapId.Value;
            try
            {
                var result = await ClientRuntime.WorldTravelService.GetMapZonesAsync(requestedMapId);
                if (ClientRuntime.World.CurrentMapId != requestedMapId)
                    return;

                loadedMapId = result.MapId;
                loadedCurrentZoneIndex = result.CurrentZoneIndex;
                loadedZones.Clear();

                if (!result.Success)
                {
                    lastStatusMessage = ResolveZoneLoadFailureMessage(result.Code);
                    return;
                }

                if (result.Zones != null)
                    loadedZones.AddRange(result.Zones);

                lastStatusMessage = loadedZones.Count > 0
                    ? "Chọn khu muốn vào."
                    : emptyZoneListText;
            }
            catch (Exception ex)
            {
                lastStatusMessage = string.Format(CultureInfo.InvariantCulture, "Lỗi tải danh sách khu: {0}", ex.Message);
                ClientLog.Warn($"WorldMapZonePanelController load zones exception: {ex.Message}");
            }
            finally
            {
                loadInFlight = false;
                RefreshPanel(force: true);
            }
        }

        private async void HandleZoneItemClicked(MapZoneListItemView itemView)
        {
            if (itemView == null || !itemView.HasEntry)
                return;

            await SwitchZoneAsync(itemView.Entry.ZoneIndex);
        }

        private async System.Threading.Tasks.Task SwitchZoneAsync(int zoneIndex)
        {
            if (switchInFlight || !ClientRuntime.IsInitialized)
                return;

            var currentMapId = ClientRuntime.World.CurrentMapId;
            if (!currentMapId.HasValue)
            {
                lastStatusMessage = "Chưa vào map nên không thể đổi khu.";
                RefreshPanel(force: true);
                return;
            }

            if (ClientRuntime.World.CurrentZoneIndex.HasValue && ClientRuntime.World.CurrentZoneIndex.Value == zoneIndex)
            {
                lastStatusMessage = string.Format(CultureInfo.InvariantCulture, "Bạn đang ở khu {0}.", zoneIndex);
                RefreshPanel(force: true);
                return;
            }

            switchInFlight = true;
            lastStatusMessage = string.Format(CultureInfo.InvariantCulture, "Đang chuyển sang khu {0}...", zoneIndex);
            RefreshPanel(force: true);

            var requestedMapId = currentMapId.Value;
            var switched = false;
            try
            {
                var result = await ClientRuntime.WorldTravelService.SwitchMapZoneAsync(requestedMapId, zoneIndex);
                if (!result.Success)
                {
                    lastStatusMessage = ResolveZoneSwitchFailureMessage(zoneIndex, result.Code);
                    return;
                }

                switched = true;
                lastStatusMessage = result.Zone.HasValue
                    ? string.Format(
                        CultureInfo.InvariantCulture,
                        "Đã vào khu {0}. Linh khí: {1} / phút.",
                        result.Zone.Value.ZoneIndex,
                        result.Zone.Value.SpiritualEnergyPerMinute.ToString("0.##", CultureInfo.InvariantCulture))
                    : string.Format(CultureInfo.InvariantCulture, "Đã vào khu {0}.", zoneIndex);
            }
            catch (Exception ex)
            {
                lastStatusMessage = string.Format(CultureInfo.InvariantCulture, "Lỗi đổi khu: {0}", ex.Message);
                ClientLog.Warn($"WorldMapZonePanelController switch zone exception: {ex.Message}");
            }
            finally
            {
                switchInFlight = false;
                RefreshPanel(force: true);
            }

            if (switched)
                await LoadCurrentMapZonesAsync(force: true);
        }

        private MapZoneListView.Entry[] BuildZoneEntries(int currentMapId)
        {
            if (loadedMapId != currentMapId || loadedZones.Count == 0)
                return Array.Empty<MapZoneListView.Entry>();

            var currentZoneIndex = ClientRuntime.World.CurrentZoneIndex ?? loadedCurrentZoneIndex ?? 0;
            var entries = new MapZoneListView.Entry[loadedZones.Count];
            for (var i = 0; i < loadedZones.Count; i++)
            {
                var zone = loadedZones[i];
                var zoneName = string.Format(CultureInfo.InvariantCulture, "Khu {0}", zone.ZoneIndex);
                var playerCountText = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}/{1}",
                    Math.Max(0, zone.CurrentPlayerCount),
                    Math.Max(0, zone.MaxPlayerCount));

                var isCurrentZone = currentZoneIndex > 0 && currentZoneIndex == zone.ZoneIndex;
                entries[i] = new MapZoneListView.Entry
                {
                    ZoneIndex = zone.ZoneIndex,
                    ZoneName = zoneName,
                    PlayerCountText = playerCountText,
                    BackgroundColor = ResolveOccupancyColor(zone.CurrentPlayerCount, zone.MaxPlayerCount),
                    IsCurrentZone = isCurrentZone,
                    IsInteractable = !loadInFlight && !switchInFlight && !isCurrentZone
                };
            }

            return entries;
        }

        private Color ResolveOccupancyColor(int currentPlayerCount, int maxPlayerCount)
        {
            if (maxPlayerCount <= 0)
                return lowOccupancyColor;

            if (currentPlayerCount >= maxPlayerCount)
                return fullOccupancyColor;

            var ratio = Mathf.Clamp01((float)currentPlayerCount / maxPlayerCount);
            if (ratio > 0.80f)
                return highOccupancyColor;

            if (ratio >= 0.30f)
                return mediumOccupancyColor;

            return lowOccupancyColor;
        }

        private string BuildCurrentMapText()
        {
            if (!ClientRuntime.IsInitialized || !ClientRuntime.World.CurrentMapId.HasValue)
                return currentMapPrefix + unknownMapText;

            var mapName = string.IsNullOrWhiteSpace(ClientRuntime.World.CurrentMapName)
                ? string.Format(CultureInfo.InvariantCulture, "Map {0}", ClientRuntime.World.CurrentMapId.Value)
                : ClientRuntime.World.CurrentMapName.Trim();
            return currentMapPrefix + mapName;
        }

        private string BuildCurrentZoneText()
        {
            if (!ClientRuntime.IsInitialized || !ClientRuntime.World.CurrentZoneIndex.HasValue)
                return currentZonePrefix + "-";

            return currentZonePrefix + ClientRuntime.World.CurrentZoneIndex.Value.ToString(CultureInfo.InvariantCulture);
        }

        private string ResolveStatusText()
        {
            if (!ClientRuntime.IsInitialized)
                return "Client runtime chưa khởi tạo.";

            if (!ClientRuntime.World.CurrentMapId.HasValue)
                return "Chưa vào map.";

            if (switchInFlight)
                return lastStatusMessage;

            if (loadInFlight)
                return "Đang tải danh sách khu...";

            if (!string.IsNullOrWhiteSpace(lastStatusMessage))
                return lastStatusMessage;

            return loadedMapId == ClientRuntime.World.CurrentMapId && loadedZones.Count > 0
                ? "Chọn khu muốn vào."
                : emptyZoneListText;
        }

        private string ResolveZoneLoadFailureMessage(MessageCode? code)
        {
            return code == MessageCode.MapZoneSelectionNotSupported
                ? unsupportedMapText
                : string.Format(
                    CultureInfo.InvariantCulture,
                    "Tải danh sách khu thất bại: {0}",
                    code ?? MessageCode.UnknownError);
        }

        private static string ResolveZoneSwitchFailureMessage(int zoneIndex, MessageCode? code)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "Đổi sang khu {0} thất bại: {1}",
                zoneIndex,
                code ?? MessageCode.UnknownError);
        }

        private string BuildSnapshot()
        {
            var currentMapId = ClientRuntime.IsInitialized ? ClientRuntime.World.CurrentMapId : null;
            var currentMapName = ClientRuntime.IsInitialized ? ClientRuntime.World.CurrentMapName : string.Empty;
            var currentZoneIndex = ClientRuntime.IsInitialized ? ClientRuntime.World.CurrentZoneIndex : null;
            var entriesSnapshot = loadedZones.Count <= 0
                ? string.Empty
                : string.Join(
                    ";",
                    loadedZones.Select(
                        zone => string.Concat(
                            zone.ZoneIndex.ToString(CultureInfo.InvariantCulture),
                            ":",
                            zone.CurrentPlayerCount.ToString(CultureInfo.InvariantCulture),
                            ":",
                            zone.MaxPlayerCount.ToString(CultureInfo.InvariantCulture),
                            ":",
                            zone.IsActive ? "1" : "0")));

            return string.Join(
                "|",
                currentMapId.HasValue ? currentMapId.Value.ToString(CultureInfo.InvariantCulture) : "0",
                currentMapName ?? string.Empty,
                currentZoneIndex.HasValue ? currentZoneIndex.Value.ToString(CultureInfo.InvariantCulture) : "0",
                loadedMapId.HasValue ? loadedMapId.Value.ToString(CultureInfo.InvariantCulture) : "0",
                loadedCurrentZoneIndex.HasValue ? loadedCurrentZoneIndex.Value.ToString(CultureInfo.InvariantCulture) : "0",
                loadInFlight ? "1" : "0",
                switchInFlight ? "1" : "0",
                lastStatusMessage ?? string.Empty,
                entriesSnapshot);
        }

        private static void ApplyText(TMP_Text textComponent, string value, bool force)
        {
            if (textComponent == null)
                return;

            var normalized = value ?? string.Empty;
            if (!force && string.Equals(textComponent.text, normalized, StringComparison.Ordinal))
                return;

            textComponent.text = normalized;
        }
    }
}
