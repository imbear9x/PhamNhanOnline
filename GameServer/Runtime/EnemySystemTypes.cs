using System.Numerics;

namespace GameServer.Runtime;

public enum EnemyKind
{
    Normal = 1,
    Elite = 2,
    Boss = 3
}

public enum MapSpawnRuntimeScope
{
    Any = 0,
    Public = 1,
    Private = 2,
    Instance = 3
}

public enum EnemySpawnMode
{
    Timer = 1,
    Objective = 2,
    Manual = 3
}

public enum RewardDeliveryType
{
    GroundDrop = 1,
    DirectGrant = 2
}

public enum RewardTargetRule
{
    EligibleAll = 1,
    LastHit = 2,
    TopDamage = 3
}

public enum InstanceMode
{
    Timed = 1,
    Farm = 2
}

public enum InstanceCompletionRule
{
    None = 0,
    KillBoss = 1
}

public enum MapRuntimeKind
{
    Public = 1,
    PrivateHome = 2,
    SoloTimedInstance = 3,
    SoloFarmInstance = 4
}

public enum EnemyRuntimeState
{
    Idle = 1,
    Patrol = 2,
    Combat = 3,
    Dead = 4
}

public enum MapInstanceCloseReason
{
    Expired = 1,
    Completed = 2
}

public sealed record EnemySkillLoadoutDefinition(
    int Id,
    int EnemyTemplateId,
    int SkillId,
    int OrderIndex);

public sealed record EnemyRewardRuleDefinition(
    int Id,
    int EnemyTemplateId,
    RewardDeliveryType DeliveryType,
    RewardTargetRule TargetRule,
    string RandomTableId,
    int RollCount,
    int? OwnershipDurationSeconds,
    int? FreeForAllDurationSeconds,
    int MinimumDamagePartsPerMillion,
    int OrderIndex);

public sealed record EnemyDefinition(
    int Id,
    string Code,
    string Name,
    EnemyKind Kind,
    int MaxHp,
    int BaseAttack,
    float BaseMoveSpeed,
    float PatrolRadius,
    float DetectionRadius,
    float CombatRadius,
    bool EnableOutOfCombatRestore,
    int OutOfCombatRestoreDelaySeconds,
    int MinimumSkillIntervalMs,
    long CultivationRewardTotal,
    int PotentialRewardTotal,
    string? Description,
    IReadOnlyList<EnemySkillLoadoutDefinition> Skills,
    IReadOnlyList<EnemyRewardRuleDefinition> RewardRules);

public sealed record MapEnemySpawnEntryDefinition(
    int Id,
    int SpawnGroupId,
    int EnemyTemplateId,
    int Weight,
    int OrderIndex);

public sealed record MapEnemySpawnGroupDefinition(
    int Id,
    string Code,
    string Name,
    int MapTemplateId,
    MapSpawnRuntimeScope RuntimeScope,
    int? ZoneIndex,
    EnemySpawnMode SpawnMode,
    bool IsBossSpawn,
    int MaxAlive,
    int RespawnSeconds,
    int InitialSpawnDelaySeconds,
    Vector2 CenterPosition,
    float SpawnRadius,
    string? Description,
    IReadOnlyList<MapEnemySpawnEntryDefinition> Entries);

public sealed record MapInstanceConfigDefinition(
    int Id,
    string Code,
    string Name,
    int MapTemplateId,
    InstanceMode InstanceMode,
    int? DurationSeconds,
    int? IdleDestroySeconds,
    InstanceCompletionRule CompletionRule,
    int? CompleteDestroyDelaySeconds,
    string? Description);

public readonly record struct RewardTargetSnapshot(
    Guid PlayerId,
    int DamageDealt,
    DateTime LastHitAtUtc);

public sealed record GroundRewardItem(
    long PlayerItemId,
    int ItemTemplateId,
    string Code,
    string Name,
    ItemType ItemType,
    ItemRarity Rarity,
    int Quantity,
    bool IsBound,
    string? Icon,
    string? BackgroundIcon);
