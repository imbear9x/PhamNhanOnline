namespace GameServer.DTO;

public sealed record PlayerSkillDto(
    long PlayerSkillId,
    int SkillId,
    string Code,
    string Name,
    string GroupCode,
    int SkillLevel,
    int SkillType,
    int SkillCategory,
    int TargetType,
    float CastRange,
    int CooldownMs,
    string? Description,
    int SourceType,
    int SourceMartialArtId,
    string? SourceMartialArtName,
    int UnlockStage,
    bool IsEquipped,
    int EquippedSlotIndex);
