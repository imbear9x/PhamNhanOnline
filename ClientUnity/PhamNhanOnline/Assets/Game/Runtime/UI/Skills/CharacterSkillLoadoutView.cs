using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using GameShared.Enums;
using GameShared.Messages;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.Skills
{
    public sealed class CharacterSkillLoadoutView : MonoBehaviour
    {
        private const int BasicSkillSlotIndex = 1;

        [Header("References")]
        [SerializeField] private SkillLoadoutSlotsView loadoutSlotsView;
        [SerializeField] private SkillPresentationCatalog presentationCatalog;

        private bool actionInFlight;
        private SkillLoadoutSlotModel[] loadoutSlots = Array.Empty<SkillLoadoutSlotModel>();
        private int maxLoadoutSlotCount;

        private void Awake()
        {
            if (loadoutSlotsView == null)
                loadoutSlotsView = GetComponentInChildren<SkillLoadoutSlotsView>(true);

            if (loadoutSlotsView != null)
                loadoutSlotsView.SkillDropped += HandleSkillDroppedToSlot;
        }

        private void OnDestroy()
        {
            if (loadoutSlotsView != null)
                loadoutSlotsView.SkillDropped -= HandleSkillDroppedToSlot;
        }

        public void SetLoadoutSlots(
            SkillLoadoutSlotModel[] slots,
            int maxSlotCount,
            bool force = false)
        {
            loadoutSlots = slots ?? Array.Empty<SkillLoadoutSlotModel>();
            maxLoadoutSlotCount = Math.Max(0, maxSlotCount);

            if (loadoutSlotsView == null)
                return;

            var normalizedSlots = BuildNormalizedLoadoutSlots(loadoutSlots, maxLoadoutSlotCount);
            loadoutSlotsView.SetSlots(normalizedSlots, presentationCatalog, !actionInFlight, force);
        }

        public void Clear(bool force = false)
        {
            loadoutSlots = Array.Empty<SkillLoadoutSlotModel>();
            maxLoadoutSlotCount = 0;
            actionInFlight = false;
            if (loadoutSlotsView != null)
                loadoutSlotsView.Clear(force);
        }

        private void HandleSkillDroppedToSlot(int slotIndex, PlayerSkillModel skill)
        {
            if (!CanAssignSkillToSlot(slotIndex, skill, out _))
                return;

            _ = SetSkillLoadoutSlotAsync(slotIndex, skill.PlayerSkillId);
        }

        private async Task SetSkillLoadoutSlotAsync(int slotIndex, long playerSkillId)
        {
            if (!ClientRuntime.IsInitialized || actionInFlight || slotIndex <= 0)
                return;

            actionInFlight = true;
            RefreshSlots(force: true);

            try
            {
                var result = await ClientRuntime.SkillService.SetSkillLoadoutSlotAsync(slotIndex, playerSkillId);
                if (!result.Success)
                {
                    ClientLog.Warn(
                        $"CharacterSkillLoadoutView set loadout failed: {result.Message ?? (result.Code ?? MessageCode.UnknownError).ToString()}");
                }
            }
            catch (Exception ex)
            {
                ClientLog.Warn($"CharacterSkillLoadoutView set loadout exception: {ex.Message}");
            }
            finally
            {
                actionInFlight = false;
                RefreshSlots(force: true);
            }
        }

        private void RefreshSlots(bool force)
        {
            if (loadoutSlotsView == null)
                return;

            var normalizedSlots = BuildNormalizedLoadoutSlots(loadoutSlots, maxLoadoutSlotCount);
            loadoutSlotsView.SetSlots(normalizedSlots, presentationCatalog, !actionInFlight, force);
        }

        private static bool CanAssignSkillToSlot(int slotIndex, PlayerSkillModel skill, out string blockedMessage)
        {
            if (!skill.CanAssignToLoadout)
            {
                blockedMessage = string.IsNullOrWhiteSpace(skill.LoadoutBlockedReason)
                    ? "Skill nay hien khong the gan vao loadout."
                    : skill.LoadoutBlockedReason.Trim();
                return false;
            }

            var category = (SkillCategory)skill.SkillCategory;
            if (slotIndex == BasicSkillSlotIndex)
            {
                if (category == SkillCategory.Basic || skill.SourcePlayerItemId.HasValue)
                {
                    blockedMessage = string.Empty;
                    return true;
                }

                blockedMessage = "O skill dau tien chi nhan skill co ban hoac skill den tu trang bi.";
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
                if (slotByIndex.TryGetValue(slotIndex, out var slot))
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
    }
}
