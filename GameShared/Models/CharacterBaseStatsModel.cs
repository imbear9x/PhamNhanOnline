using System.Collections.Generic;
using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct CharacterBaseStatsModel
{
    public Guid CharacterId;
    public int RealmTemplateId;
    public string RealmDisplayName;
    public long RealmMaxCultivation;
    public double BreakthroughChancePercent;
    public bool HasNextRealm;
    public long Cultivation;

    public int BaseHp;
    public int BaseMp;
    public int BaseAttack;
    public float BaseMoveSpeed;
    public int BaseSpeed;
    public int BaseSense;
    public int BaseStamina;
    public int LifespanBonus;
    public double BaseLuck;
    public int BasePotential;
    public int UnallocatedPotential;
    public int PotentialHpBonus;
    public int PotentialMpBonus;
    public int PotentialAttackBonus;
    public int PotentialSpeedBonus;
    public int PotentialSenseBonus;
    public double PotentialLuckBonus;
    public int FinalHp;
    public int FinalMp;
    public int FinalAttack;
    public int FinalSpeed;
    public int FinalSense;
    public int FinalStamina;
    public double FinalLuck;
    public int HpUpgradeCount;
    public int MpUpgradeCount;
    public int AttackUpgradeCount;
    public int SpeedUpgradeCount;
    public int SenseUpgradeCount;
    public int LuckUpgradeCount;
    public int ActiveMartialArtId;
    public List<PotentialUpgradePreviewModel>? PotentialUpgradePreviews;
}
