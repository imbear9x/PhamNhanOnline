namespace AdminDesignerTool;

internal enum CharacterStatType
{
    None = 0,
    Hp = 1,
    Mp = 2,
    Attack = 4,
    Speed = 5,
    SpiritualSense = 6,
    Fortune = 7
}

internal enum CombatValueType
{
    None = 0,
    Flat = 1,
    Ratio = 2,
    Percent = 3
}

internal enum CombatSkillType
{
    None = 0,
    Active = 1,
    Passive = 2,
    Toggle = 3
}

internal enum SkillTargetType
{
    None = 0,
    Self = 1,
    EnemySingle = 2,
    EnemyArea = 3,
    AllySingle = 4,
    AllyArea = 5,
    GroundArea = 6
}

internal enum SkillEffectType
{
    None = 0,
    Damage = 1,
    Heal = 2,
    ResourceReduce = 3,
    BuffStat = 4,
    DebuffStat = 5,
    Stun = 6,
    Shield = 7
}

internal enum SkillFormulaType
{
    None = 0,
    Flat = 1,
    AttackRatio = 2,
    MaxHpRatio = 3,
    MaxMpRatio = 4
}

internal enum CombatResourceType
{
    None = 0,
    Hp = 1,
    Mp = 2,
    Stamina = 3
}

internal enum SkillTargetScope
{
    None = 0,
    Primary = 1,
    Splash = 2,
    Self = 3,
    All = 4
}

internal enum SkillTriggerTiming
{
    None = 0,
    OnCast = 1,
    OnHit = 2,
    OnExpire = 3
}

internal enum SkillScalingTarget
{
    None = 0,
    EffectBaseValue = 1,
    EffectRatioValue = 2,
    EffectExtraValue = 3,
    EffectChanceValue = 4,
    EffectDurationMs = 5,
    SkillCooldownMs = 6
}

internal enum ItemType
{
    Equipment = 1,
    Consumable = 2,
    Material = 3,
    Talisman = 4,
    MartialArtBook = 5,
    Currency = 6,
    QuestItem = 7,
    PillRecipeBook = 8,
    HerbSeed = 9,
    HerbMaterial = 10,
    Soil = 11
}

internal enum ItemRarity
{
    Common = 1,
    Uncommon = 2,
    Rare = 3,
    Epic = 4,
    Legendary = 5
}

internal enum EquipmentSlot
{
    Weapon = 1,
    Armor = 2,
    Pants = 3,
    Shoes = 4
}

internal enum EquipmentType
{
    Sword = 1,
    Bow = 2,
    Armor = 3,
    Pants = 4,
    Shoes = 5
}

internal enum CraftConsumeMode
{
    Consume = 1
}

internal enum PillCategory
{
    Recovery = 1,
    Buff = 2,
    Breakthrough = 3,
    Special = 4
}

internal enum PillUsageType
{
    ConsumeDirectly = 1,
    PassiveMaterial = 2
}

internal enum PillEffectType
{
    RecoverHp = 1,
    RecoverMp = 2,
    AddBuffStat = 3,
    AddBreakthroughRate = 4,
    ClearDebuff = 5,
    Special = 6
}

internal enum HerbMaturityRequirement
{
    None = 0,
    Mature = 1,
    Perfect = 2
}

internal enum HerbGrowthStage
{
    Seedling = 1,
    Mature = 2,
    Perfect = 3
}

internal enum HerbHarvestOutputType
{
    Material = 1,
    Seed = 2
}

internal enum PotentialAllocationTarget
{
    None = 0,
    BaseHp = 1,
    BaseMp = 2,
    BaseAttack = 4,
    BaseSpeed = 5,
    BaseSpiritualSense = 6,
    BaseFortune = 7
}

internal enum MapType
{
    Home = 0,
    Farm = 1,
    Quest = 2,
    Dungeon = 3,
    Event = 4
}

internal enum GameRandomTableMode
{
    Exclusive = 0
}
