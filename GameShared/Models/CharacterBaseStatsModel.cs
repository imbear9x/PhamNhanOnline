using System.Collections.Generic;
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
    public int BaseAttack;
    public int BaseSpeed;
    public int BaseSpiritualSense;
    public int BaseStamina;
    public int LifespanBonus;
    public double BaseFortune;
    public int BasePotential;
    public int UnallocatedPotential;
    public int BonusHp;
    public int BonusMp;
    public int BonusAttack;
    public int BonusSpeed;
    public int BonusSpiritualSense;
    public double BonusFortune;
    public int HpUpgradeCount;
    public int MpUpgradeCount;
    public int AttackUpgradeCount;
    public int SpeedUpgradeCount;
    public int SpiritualSenseUpgradeCount;
    public int FortuneUpgradeCount;
    public int ActiveMartialArtId;
    public List<PotentialUpgradePreviewModel>? PotentialUpgradePreviews;
}
