namespace GameServer.DTO;

public static class CharacterBaseStatsDtoExtensions
{
    public static int GetRawHp(this CharacterBaseStatsDto stats) => stats.BaseHp ?? 0;

    public static int GetRawMp(this CharacterBaseStatsDto stats) => stats.BaseMp ?? 0;

    public static int GetRawAttack(this CharacterBaseStatsDto stats) => stats.BaseAttack ?? 0;

    public static float GetRawMoveSpeed(this CharacterBaseStatsDto stats) => stats.BaseMoveSpeed ?? 0f;

    public static int GetRawSpeed(this CharacterBaseStatsDto stats) => stats.BaseSpeed ?? 0;

    public static int GetRawSpiritualSense(this CharacterBaseStatsDto stats) => stats.BaseSpiritualSense ?? 0;

    public static int GetRawStamina(this CharacterBaseStatsDto stats) => stats.BaseStamina ?? 0;

    public static double GetRawFortune(this CharacterBaseStatsDto stats) => stats.BaseFortune ?? 0d;

    public static int GetEffectiveHp(this CharacterBaseStatsDto stats) => stats.FinalHp ?? stats.GetRawHp();

    public static int GetEffectiveMp(this CharacterBaseStatsDto stats) => stats.FinalMp ?? stats.GetRawMp();

    public static int GetEffectiveAttack(this CharacterBaseStatsDto stats) => stats.FinalAttack ?? stats.GetRawAttack();

    public static float GetEffectiveMoveSpeed(this CharacterBaseStatsDto stats) => stats.BaseMoveSpeed ?? stats.GetRawMoveSpeed();

    public static int GetEffectiveSpeed(this CharacterBaseStatsDto stats) => stats.FinalSpeed ?? stats.GetRawSpeed();

    public static int GetEffectiveSpiritualSense(this CharacterBaseStatsDto stats) => stats.FinalSpiritualSense ?? stats.GetRawSpiritualSense();

    public static int GetEffectiveStamina(this CharacterBaseStatsDto stats) => stats.FinalStamina ?? stats.GetRawStamina();

    public static double GetEffectiveFortune(this CharacterBaseStatsDto stats) => stats.FinalFortune ?? stats.GetRawFortune();
}
