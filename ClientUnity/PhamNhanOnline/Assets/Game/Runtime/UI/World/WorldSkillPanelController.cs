using System;
using System.Collections.Generic;
using System.Globalization;
using GameShared.Messages;
using GameShared.Models;
using GameShared.Enums;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Features.Skills.Application;
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

        [Header("Display Text")]
        [SerializeField] private string missingOwnedCountText = "Skill: 0";
        [SerializeField] private string missingStatusText = "Chua tai danh sach skill.";
        [SerializeField] private string emptySkillListText = "Chua so huu skill nao.";
        [SerializeField] private string actionInFlightText = "Dang cap nhat skill...";

        private bool actionInFlight;
        private string lastStatusMessage = string.Empty;
        private string lastSnapshot = string.Empty;

        private void Awake()
        {
            if (skillListView != null)
                skillListView.EquippedSkillDroppedToList += HandleEquippedSkillDroppedToList;
        }

        private void OnEnable()
        {
            RefreshPanel(force: true);
        }

        private void Update()
        {
            if (!isActiveAndEnabled)
                return;

            RefreshPanel(force: false);
        }

        private void OnDestroy()
        {
            if (skillListView != null)
                skillListView.EquippedSkillDroppedToList -= HandleEquippedSkillDroppedToList;
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

            ApplyText(
                ownedCountText,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Skill: {0}",
                    skillState.Skills != null ? skillState.Skills.Length : 0),
                force);

            if (skillListView != null)
                skillListView.SetItems(visibleSkills, null, presentationCatalog, true);

            ApplyText(statusText, ResolveStatusText(skillState, visibleSkills), true);
        }

        private void ApplyMissingState(bool force)
        {
            ApplyText(ownedCountText, missingOwnedCountText, force);
            ApplyText(statusText, ResolveMissingStatusText(), force);

            if (skillListView != null)
                skillListView.Clear(force: true);
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

        private string ResolveStatusText(ClientSkillState skillState, PlayerSkillModel[] visibleSkills)
        {
            if (actionInFlight && !string.IsNullOrWhiteSpace(lastStatusMessage))
                return lastStatusMessage;

            if (!string.IsNullOrWhiteSpace(lastStatusMessage))
                return lastStatusMessage;

            if (skillState.Skills == null || skillState.Skills.Length == 0)
                return emptySkillListText;

            if (visibleSkills.Length <= 0)
                return "Tat ca skill hien dang nam trong loadout.";

            return "Danh sach skill so huu.";
        }

        private string ResolveMissingStatusText()
        {
            if (!string.IsNullOrWhiteSpace(lastStatusMessage))
                return lastStatusMessage;

            return missingStatusText;
        }

        private string BuildSnapshot(ClientSkillState skillState)
        {
            return string.Join(
                "|",
                skillState.HasLoadedSkills ? "1" : "0",
                skillState.IsLoading ? "1" : "0",
                BuildSkillsSnapshot(skillState.Skills),
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
                    skills[i].SourceMartialArtName ?? string.Empty,
                    ":",
                    skills[i].Description ?? string.Empty,
                    ":",
                    skills[i].CanAssignToLoadout ? "1" : "0",
                    ":",
                    skills[i].LoadoutBlockedReason ?? string.Empty,
                    ":",
                    skills[i].SourcePlayerItemId.HasValue ? skills[i].SourcePlayerItemId.Value.ToString(CultureInfo.InvariantCulture) : "0");
            }

            return string.Join(";", parts);
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
