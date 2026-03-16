using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct CharacterBaseStatsModel
{
    public Guid CharacterId;
    public int RealmTemplateId;
    public long Cultivation;

    public int BaseHp;
    public int BaseMp;
    public int BasePhysique;
    public int BaseAttack;
    public int BaseSpeed;
    public int BaseSpiritualSense;
    public int BaseStamina;
    public int LifespanBonus;
    public double BaseFortune;
    public int BasePotential;
    public int UnallocatedPotential;
}
