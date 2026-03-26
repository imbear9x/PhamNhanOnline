using System;
using System.Collections.Generic;
using System.Globalization;
using GameShared.Messages;
using GameShared.Models;
using GameShared.Enums;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Features.Skills.Application;
using PhamNhanOnline.Client.Network.Session;
using PhamNhanOnline.Client.UI.Skills;
using TMPro;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.World
{
    public sealed class WorldSkillPanelController : MonoBehaviour
    {
        private const int BasicSkillSlotIndex = 1;

        [Header("References")]
        [SerializeField] private TMP_Text ownedCountText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private SkillPresentationCatalog presentationCatalog;
        [SerializeField] private SkillListView skillListView;
        [SerializeField] private SkillLoadoutSlotsView loadoutSlotsView;

        [Header("Behavior")]
        [SerializeField] private bool autoLoadMissingSkills = true;
        [SerializeField] private float reloadRetryCooldownSeconds = 2f;

        [Header("Display Text")]
        [SerializeField] private string missingOwnedCountText = "Skill: 0";
        [SerializeField] private string loadingOwnedCountText = "Dang tai skill...";
        [SerializeField] private string missingStatusText = "Chua tai danh sach skill.";
        [SerializeField] private string emptySkillListText = "Chua so huu skill nao.";
        [SerializeField] private string emptyLoadoutText = "Keo skill vao o de trang bi.";
        [SerializeField] private string actionInFlightText = "Dang cap nhat skill...";

        private Guid? lastRequestedCharacterId;
        private float lastSkillReloadAttemptTime = float.NegativeInfinity;
        private bool skillReloadInFlight;
        private bool actionInFlight;
        private string lastStatusMessage = string.Empty;
        private string lastSnapshot = string.Empty;

        private void Awake()
        {
            if (skillListView != null)
                skillListView.EquippedSkillDroppedToList += HandleEquippedSkillDroppedToList;

            if (loadoutSlotsView != null)
                loadoutSlotsView.SkillDropped += HandleSkillDroppedToSlot;
        }

        private void OnEnable()
        {
            RefreshPanel(force: true);
            TryReloadOnOpen();
            TryReloadMissingData();
        }

        private void Update()
        {
            if (!isActiveAndEnabled)
                return;

            RefreshPanel(force: false);
            TryReloadMissingData();
        }

        private void OnDestroy()
        {
            if (skillListView != null)
                skillListView.EquippedSkillDroppedToList -= HandleEquippedSkillDroppedToList;

            if (loadoutSlotsView != null)
                loadoutSlotsView.SkillDropped -= HandleSkillDroppedToSlot;
        }

        private void RefreshPanel(bool force)
        {
            if (!ClientRuntime.IsInitialized)
            {
                ApplyMissingState(force);
                return;
            }

            var skillState = ClientRuntime.Skills;
            if (!skillState.HasLoadedSkills)
            {
                ApplyMissingState(force);
                return;
            }

            var snapshot = BuildSnapshot(skillState);
            if (!force && string.Equals(lastSnapshot, snapshot, StringComparison.Ordinal))
                return;

            lastSnapshot = snapshot;
            ApplyLoadedState(skillState, true);
        }

        private void ApplyLoadedState(ClientSkillState skillState, bool force)
        {
            var visibleSkills = BuildVisibleSkillList(skillState.Skills);
            var equippedCount = CountEquippedSkills(skillState.LoadoutSlots);

            ApplyText(
                ownedCountText,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Skill: {0} | O da dung: {1}/{2}",
                    skillState.Skills != null ? skillState.Skills.Length : 0,
                    equippedCount,
                    Math.Max(0, skillState.MaxLoadoutSlotCount)),
                force);

            if (skillListView != null)
                skillListView.SetItems(visibleSkills, null, presentationCatalog, true);

            ApplyLoadoutSlots(skillState.LoadoutSlots, skillState.MaxLoadoutSlotCount);
            ApplyText(statusText, ResolveStatusText(skillState, visibleSkills, equippedCount), true);
        }

        private void ApplyMissingState(bool force)
        {
            var ownedText = skillReloadInFlight ? loadingOwnedCountText : missingOwnedCountText;
            ApplyText(ownedCountText, ownedText, force);
            ApplyText(statusText, ResolveMissingStatusText(), force);

            if (skillListView != null)
                skillListView.Clear(force: true);

            if (loadoutSlotsView != null)
                loadoutSlotsView.Clear(force: true);
        }

        private void ApplyLoadoutSlots(SkillLoadoutSlotModel[] loadoutSlots, int maxLoadoutSlotCount)
        {
            if (loadoutSlotsView == null)
                return;

            var normalizedSlots = BuildNormalizedLoadoutSlots(loadoutSlots, maxLoadoutSlotCount);
            loadoutSlotsView.SetSlots(normalizedSlots, presentationCatalog, !actionInFlight, force: true);
        }

        private void TryReloadMissingData()
        {
            if (!ClientRuntime.IsInitialized || ClientRuntime.Connection.State != ClientConnectionState.Connected)
                return;

            var selectedCharacterId = ClientRuntime.Character.SelectedCharacterId;
            if (!selectedCharacterId.HasValue || !autoLoadMissingSkills || skillReloadInFlight)
                return;

            if (!ClientRuntime.Skills.HasLoadedSkills &&
                CanRetryReload(lastRequestedCharacterId, lastSkillReloadAttemptTime, selectedCharacterId.Value))
            {
                _ = ReloadSkillsAsync(selectedCharacterId.Value);
            }
        }

        private void TryReloadOnOpen()
        {
            if (!ClientRuntime.IsInitialized || ClientRuntime.Connection.State != ClientConnectionState.Connected)
                return;

            var selectedCharacterId = ClientRuntime.Character.SelectedCharacterId;
            if (!selectedCharacterId.HasValue || skillReloadInFlight)
                return;

            if (!CanRetryReload(lastRequestedCharacterId, lastSkillReloadAttemptTime, selectedCharacterId.Value))
                return;

            _ = ReloadSkillsAsync(selectedCharacterId.Value);
        }

        private async System.Threading.Tasks.Task ReloadSkillsAsync(Guid characterId)
        {
            skillReloadInFlight = true;
            lastRequestedCharacterId = characterId;
            lastSkillReloadAttemptTime = Time.unscaledTime;
            RefreshPanel(force: true);

            try
            {
                var result = await ClientRuntime.SkillService.LoadOwnedSkillsAsync(forceRefresh: true);
                if (!result.Success)
                {
                    lastStatusMessage = string.Format(
                        CultureInfo.InvariantCulture,
                        "Tai skill that bai: {0}",
                        result.Code ?? MessageCode.UnknownError);
                    ClientLog.Warn(string.Format(
                        CultureInfo.InvariantCulture,
                        "WorldSkillPanelController failed to load skills for {0}: {1}",
                        characterId,
                        result.Message));
                }
            }
            catch (Exception ex)
            {
                lastStatusMessage = string.Format(CultureInfo.InvariantCulture, "Loi tai skill: {0}", ex.Message);
                ClientLog.Warn($"WorldSkillPanelController skill reload exception: {ex.Message}");
            }
            finally
            {
                skillReloadInFlight = false;
                RefreshPanel(force: true);
            }
        }

        private void HandleSkillDroppedToSlot(int slotIndex, PlayerSkillModel skill)
        {
            if (!CanAssignSkillToSlot(slotIndex, skill, out var blockedMessage))
            {
                lastStatusMessage = blockedMessage;
                RefreshPanel(force: true);
                return;
            }

            _ = SetSkillLoadoutSlotAsync(slotIndex, skill.PlayerSkillId);
        }

        private void HandleEquippedSkillDroppedToList(PlayerSkillModel skill)
        {
            if (!skill.IsEquipped || skill.EquippedSlotIndex <= 0)
                return;

            if (!CanUnequipSkill(skill, out var blockedMessage))
            {
                lastStatusMessage = blockedMessage;
                RefreshPanel(force: true);
                return;
            }

            _ = SetSkillLoadoutSlotAsync(skill.EquippedSlotIndex, 0);
        }

        private async System.Threading.Tasks.Task SetSkillLoadoutSlotAsync(int slotIndex, long playerSkillId)
        {
            if (!ClientRuntime.IsInitialized || actionInFlight)
                return;

            if (slotIndex <= 0)
                return;

            if (!BeginAction(actionInFlightText))
                return;

            try
            {
                var result = await ClientRuntime.SkillService.SetSkillLoadoutSlotAsync(slotIndex, playerSkillId);
                lastStatusMessage = result.Success
                    ? (playerSkillId > 0 ? "Da cap nhat o skill." : "Da go skill khoi o.")
                    : string.Format(CultureInfo.InvariantCulture, "Cap nhat skill that bai: {0}", result.Code ?? MessageCode.UnknownError);

                if (!result.Success)
                    ClientLog.Warn($"WorldSkillPanelController set loadout failed: {result.Message}");
            }
            catch (Exception ex)
            {
                lastStatusMessage = string.Format(CultureInfo.InvariantCulture, "Loi cap nhat skill: {0}", ex.Message);
                ClientLog.Warn($"WorldSkillPanelController set loadout exception: {ex.Message}");
            }
            finally
            {
                EndAction();
            }
        }

        private bool BeginAction(string status)
        {
            if (actionInFlight)
                return false;

            actionInFlight = true;
            lastStatusMessage = status ?? string.Empty;
            RefreshPanel(force: true);
            return true;
        }

        private void EndAction()
        {
            actionInFlight = false;
            RefreshPanel(force: true);
        }

        private string ResolveStatusText(ClientSkillState skillState, PlayerSkillModel[] visibleSkills, int equippedCount)
        {
            if (actionInFlight && !string.IsNullOrWhiteSpace(lastStatusMessage))
                return lastStatusMessage;

            if (!string.IsNullOrWhiteSpace(lastStatusMessage))
                return lastStatusMessage;

            if ((skillState.Skills == null || skillState.Skills.Length == 0) && equippedCount <= 0)
                return emptySkillListText;

            if (equippedCount <= 0)
                return emptyLoadoutText;

            if (visibleSkills.Length <= 0)
                return "Tat ca skill dang duoc trang bi trong loadout.";

            return "Keo skill tu danh sach vao o trong de trang bi.";
        }

        private string ResolveMissingStatusText()
        {
            if (!string.IsNullOrWhiteSpace(lastStatusMessage))
                return lastStatusMessage;

            if (skillReloadInFlight)
                return "Dang tai du lieu skill...";

            return missingStatusText;
        }

        private string BuildSnapshot(ClientSkillState skillState)
        {
            return string.Join(
                "|",
                skillState.HasLoadedSkills ? "1" : "0",
                skillState.IsLoading ? "1" : "0",
                skillState.MaxLoadoutSlotCount.ToString(CultureInfo.InvariantCulture),
                BuildSkillsSnapshot(skillState.Skills),
                BuildLoadoutSnapshot(skillState.LoadoutSlots),
                actionInFlight ? "1" : "0",
                lastStatusMessage ?? string.Empty);
        }

        private static string BuildSkillsSnapshot(PlayerSkillModel[] skills)
        {
            if (skills == null || skills.Length == 0)
                return string.Empty;

            var parts = new string[skills.Length];
            for (var i = 0; i < skills.Length; i++)
            {
                parts[i] = string.Concat(
                    skills[i].PlayerSkillId.ToString(CultureInfo.InvariantCulture),
                    ":",
                    skills[i].SkillId.ToString(CultureInfo.InvariantCulture),
                    ":",
                    skills[i].SkillLevel.ToString(CultureInfo.InvariantCulture),
                    ":",
                    skills[i].SkillCategory.ToString(CultureInfo.InvariantCulture),
                    ":",
                    skills[i].EquippedSlotIndex.ToString(CultureInfo.InvariantCulture),
                    ":",
                    skills[i].Code ?? string.Empty,
                    ":",
                    skills[i].Name ?? string.Empty,
                    ":",
                    skills[i].SkillGroupCode ?? string.Empty,
                    ":",
                    skills[i].SourceMartialArtName ?? string.Empty);
            }

            return string.Join(";", parts);
        }

        private static string BuildLoadoutSnapshot(SkillLoadoutSlotModel[] slots)
        {
            if (slots == null || slots.Length == 0)
                return string.Empty;

            var parts = new string[slots.Length];
            for (var i = 0; i < slots.Length; i++)
            {
                parts[i] = string.Concat(
                    slots[i].SlotIndex.ToString(CultureInfo.InvariantCulture),
                    ":",
                    slots[i].HasSkill ? "1" : "0",
                    ":",
                    slots[i].HasSkill && slots[i].Skill.HasValue
                        ? slots[i].Skill.Value.PlayerSkillId.ToString(CultureInfo.InvariantCulture)
                        : "0");
            }

            return string.Join(";", parts);
        }

        private static int CountEquippedSkills(SkillLoadoutSlotModel[] loadoutSlots)
        {
            if (loadoutSlots == null || loadoutSlots.Length == 0)
                return 0;

            var count = 0;
            for (var i = 0; i < loadoutSlots.Length; i++)
            {
                if (loadoutSlots[i].HasSkill && loadoutSlots[i].Skill.HasValue)
                    count++;
            }

            return count;
        }

        private static PlayerSkillModel[] BuildVisibleSkillList(PlayerSkillModel[] skills)
        {
            if (skills == null || skills.Length == 0)
                return Array.Empty<PlayerSkillModel>();

            var visible = new List<PlayerSkillModel>(skills.Length);
            for (var i = 0; i < skills.Length; i++)
            {
                if (skills[i].IsEquipped)
                    continue;

                visible.Add(skills[i]);
            }

            return visible.ToArray();
        }

        private static bool CanAssignSkillToSlot(int slotIndex, PlayerSkillModel skill, out string blockedMessage)
        {
            var category = (SkillCategory)skill.SkillCategory;
            if (slotIndex == BasicSkillSlotIndex)
            {
                if (category == SkillCategory.Basic)
                {
                    blockedMessage = string.Empty;
                    return true;
                }

                blockedMessage = "O skill dau tien chi nhan skill co ban.";
                return false;
            }

            blockedMessage = string.Empty;
            return true;
        }

        private static bool CanUnequipSkill(PlayerSkillModel skill, out string blockedMessage)
        {
            var category = (SkillCategory)skill.SkillCategory;
            if (skill.EquippedSlotIndex == BasicSkillSlotIndex &&
                category == SkillCategory.Basic)
            {
                blockedMessage = "Skill co ban o o dau tien khong the go trong. Chi co the thay bang mot skill co ban khac.";
                return false;
            }

            blockedMessage = string.Empty;
            return true;
        }

        private static SkillLoadoutSlotModel[] BuildNormalizedLoadoutSlots(
            SkillLoadoutSlotModel[] loadoutSlots,
            int maxLoadoutSlotCount)
        {
            var normalizedCount = Math.Max(0, maxLoadoutSlotCount);
            if (normalizedCount <= 0)
                return Array.Empty<SkillLoadoutSlotModel>();

            var slotByIndex = new Dictionary<int, SkillLoadoutSlotModel>(normalizedCount);
            if (loadoutSlots != null)
            {
                for (var i = 0; i < loadoutSlots.Length; i++)
                {
                    var slot = loadoutSlots[i];
                    if (slot.SlotIndex <= 0 || slot.SlotIndex > normalizedCount)
                        continue;

                    slotByIndex[slot.SlotIndex] = slot;
                }
            }

            var normalized = new SkillLoadoutSlotModel[normalizedCount];
            for (var i = 0; i < normalizedCount; i++)
            {
                var slotIndex = i + 1;
                SkillLoadoutSlotModel slot;
                if (slotByIndex.TryGetValue(slotIndex, out slot))
                {
                    normalized[i] = slot;
                    continue;
                }

                normalized[i] = new SkillLoadoutSlotModel
                {
                    SlotIndex = slotIndex,
                    HasSkill = false,
                    Skill = null
                };
            }

            return normalized;
        }

        private bool CanRetryReload(Guid? lastRequestedId, float lastAttemptTime, Guid characterId)
        {
            return lastRequestedId != characterId ||
                   Time.unscaledTime - lastAttemptTime >= reloadRetryCooldownSeconds;
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
