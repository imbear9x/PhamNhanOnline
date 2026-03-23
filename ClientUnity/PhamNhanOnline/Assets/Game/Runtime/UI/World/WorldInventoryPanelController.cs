using System;
using System.Collections.Generic;
using System.Globalization;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Network.Session;
using PhamNhanOnline.Client.UI.Common;
using TMPro;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.World
{
    public sealed class WorldInventoryPanelController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text characterNameText;
        [SerializeField] private StatLineListView statListView;

        [Header("Display")]
        [SerializeField] private bool useCurrentValuesForHpMp;
        [SerializeField] private string missingCharacterName = "Chua co nhan vat";
        [SerializeField] private string loadingCharacterName = "Dang tai...";

        [Header("Reload")]
        [SerializeField] private bool autoLoadMissingCharacterData = true;
        [SerializeField] private float reloadRetryCooldownSeconds = 2f;

        private Guid? lastRequestedCharacterId;
        private float lastReloadAttemptTime = float.NegativeInfinity;
        private bool reloadInFlight;
        private string lastCharacterName = string.Empty;
        private string lastStatsSnapshot = string.Empty;

        private void OnEnable()
        {
            RefreshFromRuntime(force: true);
            TryReloadMissingData();
        }

        private void Update()
        {
            if (!isActiveAndEnabled)
                return;

            RefreshFromRuntime(force: false);
            TryReloadMissingData();
        }

        private void RefreshFromRuntime(bool force)
        {
            if (!ClientRuntime.IsInitialized)
            {
                ApplyCharacterName(missingCharacterName, force);
                ApplyStatEntries(Array.Empty<StatLineListView.Entry>(), force);
                return;
            }

            var selectedCharacter = ClientRuntime.Character.SelectedCharacter;
            var baseStats = ClientRuntime.Character.BaseStats;
            var currentState = ClientRuntime.Character.CurrentState;

            var isMissingData = !selectedCharacter.HasValue || !baseStats.HasValue;
            var displayName = isMissingData
                ? (reloadInFlight ? loadingCharacterName : missingCharacterName)
                : ResolveCharacterName(selectedCharacter.Value.Name);

            ApplyCharacterName(displayName, force);

            if (!baseStats.HasValue)
            {
                ApplyStatEntries(Array.Empty<StatLineListView.Entry>(), force);
                return;
            }

            var stats = baseStats.Value;
            var hpValue = useCurrentValuesForHpMp && currentState.HasValue
                ? currentState.Value.CurrentHp
                : stats.BaseHp;
            var mpValue = useCurrentValuesForHpMp && currentState.HasValue
                ? currentState.Value.CurrentMp
                : stats.BaseMp;

            var entries = new[]
            {
                new StatLineListView.Entry("HP", hpValue.ToString(CultureInfo.InvariantCulture)),
                new StatLineListView.Entry("MP", mpValue.ToString(CultureInfo.InvariantCulture)),
                new StatLineListView.Entry("ATK", stats.BaseAttack.ToString(CultureInfo.InvariantCulture)),
                new StatLineListView.Entry("Speed", stats.BaseSpeed.ToString(CultureInfo.InvariantCulture)),
                new StatLineListView.Entry("Co duyen", stats.BaseFortune.ToString("0.##", CultureInfo.InvariantCulture)),
                new StatLineListView.Entry("Than thuc", stats.BaseSpiritualSense.ToString(CultureInfo.InvariantCulture)),
            };

            ApplyStatEntries(entries, force);
        }

        private void TryReloadMissingData()
        {
            if (!autoLoadMissingCharacterData || reloadInFlight || !ClientRuntime.IsInitialized)
                return;

            if (ClientRuntime.Connection.State != ClientConnectionState.Connected)
                return;

            var selectedCharacterId = ClientRuntime.Character.SelectedCharacterId;
            if (!selectedCharacterId.HasValue)
                return;

            if (ClientRuntime.Character.SelectedCharacter.HasValue && ClientRuntime.Character.BaseStats.HasValue)
                return;

            if (lastRequestedCharacterId == selectedCharacterId &&
                Time.unscaledTime - lastReloadAttemptTime < reloadRetryCooldownSeconds)
            {
                return;
            }

            _ = ReloadCharacterDataAsync(selectedCharacterId.Value);
        }

        private async System.Threading.Tasks.Task ReloadCharacterDataAsync(Guid characterId)
        {
            reloadInFlight = true;
            lastRequestedCharacterId = characterId;
            lastReloadAttemptTime = Time.unscaledTime;
            RefreshFromRuntime(force: true);

            try
            {
                var result = await ClientRuntime.CharacterService.LoadCharacterDataAsync(characterId);
                if (!result.Success)
                    ClientLog.Warn($"WorldInventoryPanelController failed to load character data: {result.Message}");
            }
            catch (Exception ex)
            {
                ClientLog.Warn($"WorldInventoryPanelController reload exception: {ex.Message}");
            }
            finally
            {
                reloadInFlight = false;
                RefreshFromRuntime(force: true);
            }
        }

        private void ApplyCharacterName(string characterName, bool force)
        {
            characterName = ResolveCharacterName(characterName);
            if (!force && string.Equals(lastCharacterName, characterName, StringComparison.Ordinal))
                return;

            lastCharacterName = characterName;
            if (characterNameText != null)
                characterNameText.text = characterName;
        }

        private void ApplyStatEntries(IReadOnlyList<StatLineListView.Entry> entries, bool force)
        {
            var snapshot = BuildStatSnapshot(entries);
            if (!force && string.Equals(lastStatsSnapshot, snapshot, StringComparison.Ordinal))
                return;

            lastStatsSnapshot = snapshot;
            if (statListView != null)
                statListView.SetEntries(entries, force: true);
        }

        private static string ResolveCharacterName(string rawName)
        {
            return string.IsNullOrWhiteSpace(rawName) ? "-" : rawName.Trim();
        }

        private static string BuildStatSnapshot(IReadOnlyList<StatLineListView.Entry> entries)
        {
            if (entries == null || entries.Count == 0)
                return string.Empty;

            var parts = new List<string>(entries.Count);
            for (var i = 0; i < entries.Count; i++)
                parts.Add(string.Concat(entries[i].Name ?? string.Empty, "=", entries[i].Value ?? string.Empty));

            return string.Join("|", parts);
        }
    }
}
