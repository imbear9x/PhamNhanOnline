using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct SkillLoadoutSlotModel
{
    public int SlotIndex;
    public bool HasSkill;
    public PlayerSkillModel? Skill;
}
