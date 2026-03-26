namespace GameServer.DTO;

public sealed record SkillLoadoutSlotDto(
    int SlotIndex,
    PlayerSkillDto? Skill);
