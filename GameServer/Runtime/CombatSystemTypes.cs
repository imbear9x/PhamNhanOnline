using GameShared.Enums;

namespace GameServer.Runtime;

public enum CharacterStatType
{
    None = 0,
    MaxHp = 1,
    MaxMp = 2,
    MaxStamina = 3,
    Attack = 4,
    Speed = 5,
    SpiritualSense = 6,
    Fortune = 7
}

public enum CombatValueType
{
    None = 0,
    Flat = 1,
    Ratio = 2,
    Percent = 3
}

public enum CombatSkillType
{
    None = 0,
    Active = 1,
    Passive = 2,
    Toggle = 3
}

public enum SkillTargetType
{
    None = 0,
    Self = 1,
    SingleEnemy = 2,
    EnemyArea = 3,
    SingleAlly = 4,
    AllyArea = 5,
    GroundArea = 6,
    AllEnemiesMap = 7,
    AllAlliesMap = 8,
    AllUnitsMap = 9
}

public enum SkillEffectType
{
    None = 0,
    Damage = 1,
    Heal = 2,
    ResourceReduce = 3,
    BuffStat = 4,
    DebuffStat = 5,
    Stun = 6,
    Shield = 7,
    ResourceRestore = 8
}

public enum SkillFormulaType
{
    None = 0,
    Flat = 1,
    AttackRatio = 2,
    CasterMaxHpRatio = 3,
    CasterMaxMpRatio = 4
}

public enum CombatResourceType
{
    None = 0,
    Hp = 1,
    Mp = 2,
    Stamina = 3
}

public enum SkillTargetScope
{
    None = 0,
    Primary = 1,
    AreaAroundPrimary = 2,
    Self = 3,
    AllResolvedTargets = 4,
    AllEnemiesMap = 5,
    AllAlliesMap = 6,
    AllUnitsMap = 7
}

public enum SkillTriggerTiming
{
    None = 0,
    OnCastRelease = 1,
    OnHit = 2,
    OnExpire = 3,
    OnCastStart = 4,
    OnInterval = 5
}

public sealed record CharacterStatBonusValue(
    int Id,
    CharacterStatType StatType,
    decimal Value,
    CombatValueType ValueType);

public sealed record MartialArtStageDefinition(
    int Id,
    int MartialArtId,
    int StageLevel,
    long ExpRequired,
    bool IsBottleneck,
    double? BreakthroughBaseRate,
    long BreakthroughExpPenalty,
    IReadOnlyList<CharacterStatBonusValue> StatBonuses);

public sealed record SkillEffectDefinition(
    int Id,
    int SkillId,
    SkillEffectType EffectType,
    int OrderIndex,
    SkillFormulaType? FormulaType,
    CombatValueType? ValueType,
    decimal? BaseValue,
    decimal? RatioValue,
    decimal? ExtraValue,
    decimal? ChanceValue,
    int? DurationMs,
    CharacterStatType? StatType,
    CombatResourceType? ResourceType,
    SkillTargetScope? TargetScope,
    SkillTriggerTiming? TriggerTiming);

public sealed record SkillDefinition(
    int Id,
    string Code,
    string Name,
    string GroupCode,
    int SkillLevel,
    CombatSkillType SkillType,
    SkillCategory SkillCategory,
    SkillTargetType TargetType,
    float CastRange,
    int CastTimeMs,
    int TravelTimeMs,
    int CooldownMs,
    string? Description,
    IReadOnlyList<SkillEffectDefinition> Effects);

public sealed record MartialArtSkillUnlockDefinition(
    int Id,
    int MartialArtId,
    int SkillId,
    int UnlockStage,
    SkillDefinition Skill);

public sealed record MartialArtDefinition(
    int Id,
    string Code,
    string Name,
    string? Icon,
    int Quality,
    string? Category,
    string? Description,
    decimal QiAbsorptionRate,
    int MaxStage,
    IReadOnlyList<MartialArtStageDefinition> Stages,
    IReadOnlyList<MartialArtSkillUnlockDefinition> SkillUnlocks);

public readonly record struct PlayerMartialArtProgressState(
    int MartialArtId,
    int CurrentStage,
    long CurrentExp);

public readonly record struct FlatStatBonusBundle(
    int MaxHp,
    int MaxMp,
    int MaxStamina,
    int Attack,
    int Speed,
    int SpiritualSense,
    double Fortune)
{
    public static FlatStatBonusBundle Empty => default;

    public FlatStatBonusBundle Add(CharacterStatType statType, decimal value)
    {
        return statType switch
        {
            CharacterStatType.MaxHp => this with { MaxHp = checked(MaxHp + decimal.ToInt32(decimal.Truncate(value))) },
            CharacterStatType.MaxMp => this with { MaxMp = checked(MaxMp + decimal.ToInt32(decimal.Truncate(value))) },
            CharacterStatType.MaxStamina => this with { MaxStamina = checked(MaxStamina + decimal.ToInt32(decimal.Truncate(value))) },
            CharacterStatType.Attack => this with { Attack = checked(Attack + decimal.ToInt32(decimal.Truncate(value))) },
            CharacterStatType.Speed => this with { Speed = checked(Speed + decimal.ToInt32(decimal.Truncate(value))) },
            CharacterStatType.SpiritualSense => this with { SpiritualSense = checked(SpiritualSense + decimal.ToInt32(decimal.Truncate(value))) },
            CharacterStatType.Fortune => this with { Fortune = Fortune + (double)value },
            _ => this
        };
    }
}

public sealed record SkillRuntimeEffect(
    int Id,
    SkillEffectType EffectType,
    int OrderIndex,
    SkillFormulaType? FormulaType,
    CombatValueType? ValueType,
    decimal? BaseValue,
    decimal? RatioValue,
    decimal? ExtraValue,
    decimal? ChanceValue,
    int? DurationMs,
    CharacterStatType? StatType,
    CombatResourceType? ResourceType,
    SkillTargetScope? TargetScope,
    SkillTriggerTiming? TriggerTiming);

public sealed record SkillRuntimeDefinition(
    int SkillId,
    int MartialArtId,
    int MartialArtSkillId,
    int UnlockStage,
    int CurrentMartialArtStage,
    string SkillGroupCode,
    int SkillLevel,
    string Code,
    string Name,
    CombatSkillType SkillType,
    SkillCategory SkillCategory,
    SkillTargetType TargetType,
    float CastRange,
    int CastTimeMs,
    int TravelTimeMs,
    int CooldownMs,
    IReadOnlyList<SkillRuntimeEffect> Effects);
