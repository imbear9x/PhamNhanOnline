using System;
using System.Collections.Generic;
using GameShared.Models;
using PhamNhanOnline.Client.UI.Inventory;
using PhamNhanOnline.Client.UI.World;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.Skills
{
    public sealed class WorldSkillPanelView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SkillLoadoutSlotsView loadoutSlotsView;
        [SerializeField] private SkillListView skillListView;
        [SerializeField] private SkillPresentationCatalog presentationCatalog;

        private SkillLoadoutSlotModel[] loadoutSlots = Array.Empty<SkillLoadoutSlotModel>();
        private int maxLoadoutSlotCount;

        public event Action<int, PlayerSkillModel, int?> SkillDroppedToSlot;
        public event Action<PlayerSkillModel> EquippedSkillDroppedToList;
        public event Action<PlayerSkillModel> SkillListItemClicked;
        public event Action<SkillLoadoutSlotView> SkillSlotClicked;

        private void Awake()
        {
            if (loadoutSlotsView == null)
                loadoutSlotsView = GetComponentInChildren<SkillLoadoutSlotsView>(true);

            if (skillListView == null)
                skillListView = GetComponentInChildren<SkillListView>(true);

            if (loadoutSlotsView != null)
            {
                loadoutSlotsView.SkillDropped += HandleSkillDroppedToSlot;
                loadoutSlotsView.SlotClicked += HandleSkillSlotClicked;
            }

            if (skillListView != null)
            {
                skillListView.EquippedSkillDroppedToList += HandleEquippedSkillDroppedToList;
                skillListView.ItemClicked += HandleSkillListItemClicked;
            }
        }

        private void OnDestroy()
        {
            if (loadoutSlotsView != null)
            {
                loadoutSlotsView.SkillDropped -= HandleSkillDroppedToSlot;
                loadoutSlotsView.SlotClicked -= HandleSkillSlotClicked;
            }

            if (skillListView != null)
            {
                skillListView.EquippedSkillDroppedToList -= HandleEquippedSkillDroppedToList;
                skillListView.ItemClicked -= HandleSkillListItemClicked;
            }
        }

        public void SetSkills(IReadOnlyList<PlayerSkillModel> skills, long? selectedSkillId, bool force = false)
        {
            skillListView?.SetItems(
                skills ?? Array.Empty<PlayerSkillModel>(),
                selectedSkillId,
                presentationCatalog,
                force);
        }

        public void SetLoadoutSlots(
            SkillLoadoutSlotModel[] slots,
            int maxSlotCount,
            int? selectedSlotIndex,
            bool dragEnabled,
            bool force = false)
        {
            loadoutSlots = slots ?? Array.Empty<SkillLoadoutSlotModel>();
            maxLoadoutSlotCount = Math.Max(0, maxSlotCount);

            if (loadoutSlotsView == null)
                return;

            var normalizedSlots = BuildNormalizedLoadoutSlots(loadoutSlots, maxLoadoutSlotCount);
            loadoutSlotsView.SetSlots(normalizedSlots, presentationCatalog, dragEnabled, force);
            loadoutSlotsView.SetSelectedSlot(selectedSlotIndex, force: true);
        }

        public void Clear(bool force = false)
        {
            loadoutSlots = Array.Empty<SkillLoadoutSlotModel>();
            maxLoadoutSlotCount = 0;
            loadoutSlotsView?.Clear(force);
            skillListView?.Clear(force);
        }

        public void ShowItemOptionsPopup(IReadOnlyList<ItemOptionEntry> options, bool force = false)
        {
            WorldModalUIManager.Instance?.ShowItemOptionsPopup(options, force);
        }

        public void HideItemOptionsPopup(bool force = false)
        {
            WorldModalUIManager.Instance?.HideItemOptionsPopup(force);
        }

        public void HideItemTooltip(bool force = false)
        {
            WorldModalUIManager.Instance?.HideItemTooltip(force: force);
        }

        private void HandleSkillDroppedToSlot(int slotIndex, PlayerSkillModel skill, int? sourceSlotIndex)
        {
            var handler = SkillDroppedToSlot;
            if (handler != null)
                handler(slotIndex, skill, sourceSlotIndex);
        }

        private void HandleEquippedSkillDroppedToList(PlayerSkillModel skill)
        {
            var handler = EquippedSkillDroppedToList;
            if (handler != null)
                handler(skill);
        }

        private void HandleSkillListItemClicked(PlayerSkillModel skill)
        {
            var handler = SkillListItemClicked;
            if (handler != null)
                handler(skill);
        }

        private void HandleSkillSlotClicked(SkillLoadoutSlotView slotView)
        {
            var handler = SkillSlotClicked;
            if (handler != null)
                handler(slotView);
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
