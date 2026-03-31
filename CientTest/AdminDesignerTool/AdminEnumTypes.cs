namespace AdminDesignerTool;

internal enum AdminEditorKind
{
    GenericTable = 0,
    MartialArtWorkspace = 1,
    CraftRecipeWorkspace = 2,
    EquipmentWorkspace = 3,
    MapWorkspace = 4,
    PillRecipeWorkspace = 5,
    HerbWorkspace = 6,
    PillWorkspace = 7,
    GameRandomWorkspace = 8,
    EnemyWorkspace = 9,
    EnemySpawnWorkspace = 10,
    PlayerInventoryWorkspace = 11,
    CharacterMartialArtWorkspace = 12,
    CharacterItemWorkspace = 13,
    CharacterSkillWorkspace = 14,
    SkillWorkspace = 15
}

internal enum CharacterStatType
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

internal enum SkillCategory
{
    Basic = 1,
    Normal = 2,
    Special = 3
}

internal enum SkillTargetType
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

internal enum SkillEffectType
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

internal enum SkillEffectFormulaType
{
    None = 0,
    Flat = 1,
    AttackRatio = 2,
    CasterMaxHpRatio = 3,
    CasterMaxMpRatio = 4
}

internal enum SkillEffectValueType
{
    None = 0,
    Flat = 1,
    Ratio = 2,
    Percent = 3
}

internal enum SkillEffectResourceType
{
    None = 0,
    Hp = 1,
    Mp = 2,
    Stamina = 3
}

internal enum SkillEffectStatType
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

internal enum SkillEffectTargetScope
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

internal enum SkillEffectTriggerTiming
{
    None = 0,
    OnCastRelease = 1,
    OnHit = 2,
    OnExpire = 3,
    OnCastStart = 4,
    OnInterval = 5
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
    Soil = 11,
    HerbPlant = 12
}

internal enum ItemLocationType
{
    Inventory = 1,
    Ground = 2,
    TradeHold = 3,
    Mail = 4,
    Storage = 5
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

internal enum EquipmentBonusSourceType
{
    DropBonus = 1,
    CraftBonus = 2,
    MutationBonus = 3,
    RefineBonus = 4,
    EventBonus = 5
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

internal enum MapSpawnPointCategory
{
    Start = 1,
    End = 2,
    Middle = 3,
    Custom = 4
}

internal enum MapPortalInteractionMode
{
    Touch = 1,
    Interact = 2
}

internal enum GameRandomTableMode
{
    Exclusive = 0
}

internal enum EnemyKind
{
    Normal = 1,
    Elite = 2,
    Boss = 3
}

internal enum MapSpawnRuntimeScope
{
    Any = 0,
    Public = 1,
    Private = 2,
    Instance = 3
}

internal enum EnemySpawnMode
{
    Timer = 1,
    Objective = 2,
    Manual = 3
}

internal enum RewardDeliveryType
{
    GroundDrop = 1,
    DirectGrant = 2
}

internal enum RewardTargetRule
{
    EligibleAll = 1,
    LastHit = 2,
    TopDamage = 3
}

internal enum PlayerSkillSourceType
{
    Manual = 1,
    MartialArtUnlock = 2,
    ItemUse = 3,
    QuestReward = 4,
    SystemGrant = 5
}

internal enum InstanceMode
{
    Timed = 1,
    Farm = 2
}

internal enum InstanceCompletionRule
{
    None = 0,
    KillBoss = 1
}
