using GameShared.Messages;
using GameShared.Models;

namespace PhamNhanOnline.Client.Features.Skills.Application
{
    public readonly struct SkillLoadoutSetResult
    {
        public SkillLoadoutSetResult(
            bool success,
            MessageCode? code,
            int maxLoadoutSlotCount,
            PlayerSkillModel[] skills,
            SkillLoadoutSlotModel[] loadoutSlots,
            string message)
        {
            Success = success;
            Code = code;
            MaxLoadoutSlotCount = maxLoadoutSlotCount;
            Skills = skills ?? System.Array.Empty<PlayerSkillModel>();
            LoadoutSlots = loadoutSlots ?? System.Array.Empty<SkillLoadoutSlotModel>();
            Message = message ?? string.Empty;
        }

        public bool Success { get; }
        public MessageCode? Code { get; }
        public int MaxLoadoutSlotCount { get; }
        public PlayerSkillModel[] Skills { get; }
        public SkillLoadoutSlotModel[] LoadoutSlots { get; }
        public string Message { get; }
    }
}
