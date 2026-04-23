using System;
using System.Collections.Generic;
using System.Globalization;
using GameShared.Messages;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Features.Skills.Application;
using PhamNhanOnline.Client.UI.Inventory;
using PhamNhanOnline.Client.UI.Skills;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.World
{
    public sealed class WorldSkillPanelController : MonoBehaviour
    {
        private const string SelectOptionText = "L\u1EF1a ch\u1ECDn";
        private const string UnequipOptionText = "Go ra";
        private const string RemoveOptionText = "G\u1EE1 b\u1ECF";

        [Header("References")]
        [SerializeField] private WorldSkillPanelView panelView;

        private bool actionInFlight;
        private string lastSnapshot = string.Empty;
        private long? popupSkillId;
        private int? popupSlotIndex;
        private bool popupTargetsSlot;

        private void Awake()
        {
            if (panelView == null)
                panelView = GetComponent<WorldSkillPanelView>();

            if (panelView != null)
            {
                panelView.SkillDroppedToSlot += HandleSkillDroppedToSlot;
                panelView.EquippedSkillDroppedToList += HandleEquippedSkillDroppedToList;
                panelView.SkillListItemClicked += HandleSkillListItemClicked;
                panelView.SkillSlotClicked += HandleSkillSlotClicked;
            }
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
            if (panelView != null)
            {
                panelView.SkillDroppedToSlot -= HandleSkillDroppedToSlot;
                panelView.EquippedSkillDroppedToList -= HandleEquippedSkillDroppedToList;
                panelView.SkillListItemClicked -= HandleSkillListItemClicked;
                panelView.SkillSlotClicked -= HandleSkillSlotClicked;
            }
        }

        private void RefreshPanel(bool force)
        {
            if (panelView == null)
                return;

            if (!ClientRuntime.IsInitialized)
            {
                panelView.Clear(force: true);
                return;
            }

            var skillState = ClientRuntime.Skills;
            if (!skillState.HasLoadedSkills)
            {
                panelView.Clear(force: true);
                return;
            }

            var snapshot = BuildSnapshot(skillState);
            if (!force && string.Equals(lastSnapshot, snapshot, StringComparison.Ordinal))
                return;

            lastSnapshot = snapshot;
            panelView.SetSkills(skillState.Skills ?? Array.Empty<PlayerSkillModel>(), popupTargetsSlot ? null : popupSkillId, force: true);
            panelView.SetLoadoutSlots(
                skillState.LoadoutSlots,
                skillState.MaxLoadoutSlotCount,
                popupTargetsSlot ? popupSlotIndex : null,
                !actionInFlight,
                force: true);
        }

        private void HandleSkillDroppedToSlot(int slotIndex, PlayerSkillModel skill, int? sourceSlotIndex)
        {
            if (!CanAssignSkillToSlot(slotIndex, skill, out _))
                return;

            if (sourceSlotIndex.HasValue &&
                sourceSlotIndex.Value > 0 &&
                sourceSlotIndex.Value != slotIndex &&
                ClientRuntime.IsInitialized &&
                ClientRuntime.Skills.TryGetLoadoutSkill(slotIndex, out var targetSkill) &&
                targetSkill.PlayerSkillId != skill.PlayerSkillId)
            {
                _ = SwapSkillLoadoutSlotsAsync(sourceSlotIndex.Value, slotIndex);
                return;
            }

            _ = SetSkillLoadoutSlotAsync(slotIndex, skill.PlayerSkillId);
        }

        private void HandleEquippedSkillDroppedToList(PlayerSkillModel skill)
        {
            if (!skill.IsEquipped || skill.EquippedSlotIndex <= 0)
                return;

            _ = SetSkillLoadoutSlotAsync(skill.EquippedSlotIndex, 0);
        }

        private void HandleSkillListItemClicked(PlayerSkillModel skill)
        {
            if (actionInFlight)
                return;

            if (!skill.CanAssignToLoadout)
            {
                HideSkillOptionsPopup(force: true);
                return;
            }

            var modalUIManager = WorldModalUIManager.Instance;
            if (modalUIManager != null &&
                modalUIManager.IsItemOptionsPopupVisible &&
                !popupTargetsSlot &&
                popupSkillId.HasValue &&
                popupSkillId.Value == skill.PlayerSkillId)
            {
                HideSkillOptionsPopup();
                return;
            }

            ShowSkillOptions(skill, activeSlot: false, slotIndex: null);
        }

        private void HandleSkillSlotClicked(SkillLoadoutSlotView slotView)
        {
            if (slotView == null || !slotView.HasItem || actionInFlight)
                return;

            var skill = slotView.Item;
            var modalUIManager = WorldModalUIManager.Instance;
            if (modalUIManager != null &&
                modalUIManager.IsItemOptionsPopupVisible &&
                popupTargetsSlot &&
                popupSlotIndex.HasValue &&
                popupSlotIndex.Value == slotView.SlotIndex)
            {
                HideSkillOptionsPopup();
                return;
            }

            ShowSkillOptions(skill, activeSlot: true, slotIndex: slotView.SlotIndex);
        }

        private async System.Threading.Tasks.Task SetSkillLoadoutSlotAsync(int slotIndex, long playerSkillId)
        {
            if (!ClientRuntime.IsInitialized || actionInFlight || slotIndex <= 0)
                return;

            actionInFlight = true;
            HideSkillOptionsPopup(force: true);
            RefreshPanel(force: true);

            try
            {
                var result = await ClientRuntime.SkillService.SetSkillLoadoutSlotAsync(slotIndex, playerSkillId);
                if (!result.Success)
                {
                    ClientLog.Warn(
                        $"WorldSkillPanelController set loadout failed: {result.Message ?? (result.Code ?? MessageCode.UnknownError).ToString()}");
                }
            }
            catch (Exception ex)
            {
                ClientLog.Warn($"WorldSkillPanelController set loadout exception: {ex.Message}");
            }
            finally
            {
                actionInFlight = false;
                RefreshPanel(force: true);
            }
        }

        private async System.Threading.Tasks.Task SwapSkillLoadoutSlotsAsync(
            int sourceSlotIndex,
            int targetSlotIndex)
        {
            if (!ClientRuntime.IsInitialized || actionInFlight || sourceSlotIndex <= 0 || targetSlotIndex <= 0)
                return;

            actionInFlight = true;
            HideSkillOptionsPopup(force: true);
            RefreshPanel(force: true);

            try
            {
                var result = await ClientRuntime.SkillService.SwapSkillLoadoutSlotsAsync(sourceSlotIndex, targetSlotIndex);
                if (!result.Success)
                {
                    ClientLog.Warn(
                        $"WorldSkillPanelController swap failed: {result.Message ?? (result.Code ?? MessageCode.UnknownError).ToString()}");
                }
            }
            catch (Exception ex)
            {
                ClientLog.Warn($"WorldSkillPanelController swap exception: {ex.Message}");
            }
            finally
            {
                actionInFlight = false;
                RefreshPanel(force: true);
            }
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
                popupTargetsSlot ? "1" : "0",
                popupSkillId.HasValue ? popupSkillId.Value.ToString(CultureInfo.InvariantCulture) : "0",
                popupSlotIndex.HasValue ? popupSlotIndex.Value.ToString(CultureInfo.InvariantCulture) : "0",
                actionInFlight ? "1" : "0");
        }

        private static bool CanAssignSkillToSlot(int slotIndex, PlayerSkillModel skill, out string blockedMessage)
        {
            _ = slotIndex;

            if (!skill.CanAssignToLoadout)
            {
                blockedMessage = string.IsNullOrWhiteSpace(skill.LoadoutBlockedReason)
                    ? "Skill nay hien khong the gan vao loadout."
                    : skill.LoadoutBlockedReason.Trim();
                return false;
            }

            blockedMessage = string.Empty;
            return true;
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

        private void ShowSkillOptions(PlayerSkillModel skill, bool activeSlot, int? slotIndex)
        {
            var options = BuildSkillOptions(skill, activeSlot, slotIndex);
            if (options.Count == 0)
            {
                HideSkillOptionsPopup(force: true);
                return;
            }

            popupSkillId = skill.PlayerSkillId;
            popupSlotIndex = slotIndex;
            popupTargetsSlot = activeSlot;
            panelView?.HideItemTooltip(force: true);
            panelView?.ShowItemOptionsPopup(options, force: true);
            RefreshPanel(force: true);
        }

        private List<ItemOptionEntry> BuildSkillOptions(PlayerSkillModel skill, bool activeSlot, int? slotIndex)
        {
            if (activeSlot && slotIndex.HasValue && slotIndex.Value > 0)
            {
                return new List<ItemOptionEntry>(1)
                {
                    new ItemOptionEntry(UnequipOptionText, () => _ = SetSkillLoadoutSlotAsync(slotIndex.Value, 0))
                };
            }

            if (skill.IsEquipped && skill.EquippedSlotIndex > 0)
            {
                return new List<ItemOptionEntry>(1)
                {
                    new ItemOptionEntry(RemoveOptionText, () => _ = SetSkillLoadoutSlotAsync(skill.EquippedSlotIndex, 0))
                };
            }

            return new List<ItemOptionEntry>(1)
            {
                new ItemOptionEntry(SelectOptionText, () => _ = EquipSkillToFirstAvailableSlotAsync(skill))
            };
        }

        private void HideSkillOptionsPopup(bool force = false)
        {
            popupSkillId = null;
            popupSlotIndex = null;
            popupTargetsSlot = false;

            panelView?.HideItemOptionsPopup(force);
            panelView?.HideItemTooltip(force: true);
            RefreshPanel(force: true);
        }

        private async System.Threading.Tasks.Task EquipSkillToFirstAvailableSlotAsync(PlayerSkillModel skill)
        {
            if (!ClientRuntime.IsInitialized || actionInFlight)
                return;

            var skillState = ClientRuntime.Skills;
            var targetSlotIndex = skill.IsEquipped && skill.EquippedSlotIndex > 0
                ? skill.EquippedSlotIndex
                : 0;

            if (targetSlotIndex <= 0)
            {
                for (var i = 0; i < skillState.LoadoutSlots.Length; i++)
                {
                    if (skillState.LoadoutSlots[i].SlotIndex > 0 && !skillState.LoadoutSlots[i].HasSkill)
                    {
                        targetSlotIndex = skillState.LoadoutSlots[i].SlotIndex;
                        break;
                    }
                }
            }

            if (targetSlotIndex <= 0)
                targetSlotIndex = 1;

            await SetSkillLoadoutSlotAsync(targetSlotIndex, skill.PlayerSkillId);
        }
    }
}
