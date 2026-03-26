using GameShared.Messages;
using GameShared.Models;

namespace PhamNhanOnline.Client.Features.Skills.Application
{
    public readonly struct SkillListLoadResult
    {
        public SkillListLoadResult(
            bool success,
            MessageCode? code,
            int maxLoadoutSlotCount,
            PlayerSkillModel[] skills,
            SkillLoadoutSlotModel[] loadoutSlots,
            string message,
            bool fromCache)
        {
            Success = success;
            Code = code;
            MaxLoadoutSlotCount = maxLoadoutSlotCount;
            Skills = skills ?? System.Array.Empty<PlayerSkillModel>();
            LoadoutSlots = loadoutSlots ?? System.Array.Empty<SkillLoadoutSlotModel>();
            Message = message ?? string.Empty;
            FromCache = fromCache;
        }

        public bool Success { get; }
        public MessageCode? Code { get; }
        public int MaxLoadoutSlotCount { get; }
        public PlayerSkillModel[] Skills { get; }
        public SkillLoadoutSlotModel[] LoadoutSlots { get; }
        public string Message { get; }
        public bool FromCache { get; }
    }
}
