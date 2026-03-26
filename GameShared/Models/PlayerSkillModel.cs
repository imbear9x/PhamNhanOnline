using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct PlayerSkillModel
{
    public long PlayerSkillId;
    public int SkillId;
    public string? Code;
    public string? Name;
    public string? SkillGroupCode;
    public int SkillLevel;
    public int SkillType;
    public int SkillCategory;
    public int TargetType;
    public float CastRange;
    public int CooldownMs;
    public string? Description;
    public int SourceType;
    public int SourceMartialArtId;
    public string? SourceMartialArtName;
    public int UnlockStage;
    public bool IsEquipped;
    public int EquippedSlotIndex;
}
