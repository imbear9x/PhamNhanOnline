namespace GameServer.Config;

public sealed class CharacterCreateConfig
{
    public int RealmTemplateId { get; set; } = 1;
    public int FallbackRealmLifespan { get; set; } = 120;
    public long Cultivation { get; set; } = 0;
    public int BaseHp { get; set; } = 100;
    public int BaseMp { get; set; } = 100;
    public int BaseAttack { get; set; } = 10;
    public decimal BaseMoveSpeed { get; set; } = 300m;
    public int BaseSpeed { get; set; } = 100;
    public int BaseSpiritualSense { get; set; } = 10;
    public int BaseStamina { get; set; } = 100;
    public int LifespanBonus { get; set; } = 0;
    public double BaseFortune { get; set; } = 0.01d;
    public int BasePotential { get; set; } = 0;
    public int UnallocatedPotential { get; set; } = 0;
    public bool PotentialRewardLocked { get; set; } = false;
}
