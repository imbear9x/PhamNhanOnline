using GameServer.DTO;
namespace GameServer.Runtime;

public sealed class CharacterBaseStatsComposer
{
    public CharacterBaseStatsDto Compose(CharacterBaseStatsDto baseStats)
    {
        var rawBaseHp = baseStats.RawBaseHp ?? baseStats.BaseHp ?? 0;
        var rawBaseMp = baseStats.RawBaseMp ?? baseStats.BaseMp ?? 0;
        var rawBaseAttack = baseStats.RawBaseAttack ?? baseStats.BaseAttack ?? 0;
        var rawBaseSpeed = baseStats.RawBaseSpeed ?? baseStats.BaseSpeed ?? 0;
        var rawBaseSpiritualSense = baseStats.RawBaseSpiritualSense ?? baseStats.BaseSpiritualSense ?? 0;
        var rawBaseStamina = baseStats.RawBaseStamina ?? baseStats.BaseStamina ?? 0;
        var rawBaseFortune = baseStats.RawBaseFortune ?? baseStats.BaseFortune ?? 0d;

        return baseStats with
        {
            BaseHp = checked(rawBaseHp + (baseStats.BonusHp ?? 0)),
            BaseMp = checked(rawBaseMp + (baseStats.BonusMp ?? 0)),
            BaseAttack = checked(rawBaseAttack + (baseStats.BonusAttack ?? 0)),
            BaseSpeed = checked(rawBaseSpeed + (baseStats.BonusSpeed ?? 0)),
            BaseSpiritualSense = checked(rawBaseSpiritualSense + (baseStats.BonusSpiritualSense ?? 0)),
            BaseStamina = rawBaseStamina,
            BaseFortune = rawBaseFortune + (baseStats.BonusFortune ?? 0d)
        };
    }
}
