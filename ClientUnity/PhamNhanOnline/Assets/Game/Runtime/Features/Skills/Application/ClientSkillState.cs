using System;
using System.Linq;
using GameShared.Messages;
using GameShared.Models;

namespace PhamNhanOnline.Client.Features.Skills.Application
{
    public sealed class ClientSkillState
    {
        public event Action Changed;

        public bool HasLoadedSkills { get; private set; }
        public bool IsLoading { get; private set; }
        public MessageCode? LastResultCode { get; private set; }
        public string LastStatusMessage { get; private set; } = string.Empty;
        public DateTime? LastLoadedAtUtc { get; private set; }
        public int MaxLoadoutSlotCount { get; private set; }
        public PlayerSkillModel[] Skills { get; private set; } = Array.Empty<PlayerSkillModel>();
        public SkillLoadoutSlotModel[] LoadoutSlots { get; private set; } = Array.Empty<SkillLoadoutSlotModel>();

        public void BeginLoading()
        {
            if (IsLoading)
                return;

            IsLoading = true;
            NotifyChanged();
        }

        public void ApplySnapshot(
            int maxLoadoutSlotCount,
            PlayerSkillModel[] skills,
            SkillLoadoutSlotModel[] loadoutSlots,
            MessageCode? code,
            string statusMessage)
        {
            HasLoadedSkills = true;
            IsLoading = false;
            LastResultCode = code;
            LastStatusMessage = statusMessage ?? string.Empty;
            LastLoadedAtUtc = DateTime.UtcNow;
            MaxLoadoutSlotCount = Math.Max(0, maxLoadoutSlotCount);
            Skills = NormalizeSkills(skills, loadoutSlots);
            LoadoutSlots = NormalizeLoadoutSlots(maxLoadoutSlotCount, loadoutSlots, Skills);
            NotifyChanged();
        }

        public void ApplyFailure(MessageCode? code, string statusMessage)
        {
            IsLoading = false;
            LastResultCode = code;
            LastStatusMessage = statusMessage ?? string.Empty;
            NotifyChanged();
        }

        public bool TryGetLoadoutSkill(int slotIndex, out PlayerSkillModel skill)
        {
            for (var i = 0; i < LoadoutSlots.Length; i++)
            {
                var slot = LoadoutSlots[i];
                if (slot.SlotIndex != slotIndex || !slot.HasSkill || !slot.Skill.HasValue)
                    continue;

                skill = slot.Skill.Value;
                return true;
            }

            skill = default(PlayerSkillModel);
            return false;
        }

        public void Clear()
        {
            HasLoadedSkills = false;
            IsLoading = false;
            LastResultCode = null;
            LastStatusMessage = string.Empty;
            LastLoadedAtUtc = null;
            MaxLoadoutSlotCount = 0;
            Skills = Array.Empty<PlayerSkillModel>();
            LoadoutSlots = Array.Empty<SkillLoadoutSlotModel>();
            NotifyChanged();
        }

        private static PlayerSkillModel[] NormalizeSkills(PlayerSkillModel[] skills, SkillLoadoutSlotModel[] loadoutSlots)
        {
            var equippedSlotByPlayerSkillId = (loadoutSlots ?? Array.Empty<SkillLoadoutSlotModel>())
                .Where(slot => slot.HasSkill && slot.Skill.HasValue)
                .GroupBy(slot => slot.Skill.Value.PlayerSkillId)
                .ToDictionary(group => group.Key, group => group.OrderBy(slot => slot.SlotIndex).First().SlotIndex);

            if (skills == null || skills.Length == 0)
                return Array.Empty<PlayerSkillModel>();

            var normalized = new PlayerSkillModel[skills.Length];
            for (var i = 0; i < skills.Length; i++)
            {
                var skill = skills[i];
                int slotIndex;
                if (!equippedSlotByPlayerSkillId.TryGetValue(skill.PlayerSkillId, out slotIndex))
                    slotIndex = 0;

                skill.IsEquipped = slotIndex > 0;
                skill.EquippedSlotIndex = slotIndex;
                normalized[i] = skill;
            }

            return normalized;
        }

        private static SkillLoadoutSlotModel[] NormalizeLoadoutSlots(
            int maxLoadoutSlotCount,
            SkillLoadoutSlotModel[] loadoutSlots,
            PlayerSkillModel[] skills)
        {
            var normalizedCount = Math.Max(0, maxLoadoutSlotCount);
            if (normalizedCount == 0)
                return Array.Empty<SkillLoadoutSlotModel>();

            var skillByPlayerSkillId = (skills ?? Array.Empty<PlayerSkillModel>())
                .ToDictionary(skill => skill.PlayerSkillId);
            var inputBySlotIndex = (loadoutSlots ?? Array.Empty<SkillLoadoutSlotModel>())
                .Where(slot => slot.SlotIndex > 0)
                .GroupBy(slot => slot.SlotIndex)
                .ToDictionary(group => group.Key, group => group.First());

            var normalized = new SkillLoadoutSlotModel[normalizedCount];
            for (var i = 0; i < normalizedCount; i++)
            {
                var slotIndex = i + 1;
                if (inputBySlotIndex.TryGetValue(slotIndex, out var slot) &&
                    slot.HasSkill &&
                    slot.Skill.HasValue &&
                    skillByPlayerSkillId.TryGetValue(slot.Skill.Value.PlayerSkillId, out var skill))
                {
                    normalized[i] = new SkillLoadoutSlotModel
                    {
                        SlotIndex = slotIndex,
                        HasSkill = true,
                        Skill = skill
                    };
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

        private void NotifyChanged()
        {
            var handler = Changed;
            if (handler != null)
                handler();
        }
    }
}
